using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ArcheCore.WorldServer.Lua.Scripting;
using ArcheCore.WorldServer.Lua.Scripting.Bindings;
using ArcheCore.WorldServer.Networking.W2C;
using ArcheCore.WorldServer.PersistenceServer;
using ArcheCore.WorldServer.ServerConfig;
using LiteNetLib;
using Shared;
using Shared.Components;
using UnityEngine;
using Worldserver.ArcheCore.PersistenceServer.Scripts;

namespace ArcheCore.WorldServer.Managers
{
    public class PlayerManager
    {
        private int nextNetworkId = 1;

        private readonly Dictionary<NetPeer, int>     peerToId    = new();
        private readonly Dictionary<int, int>         idToAccount = new();
        private readonly Dictionary<int, Vector3>     positions   = new();

        // Persistence data per networkId
        private readonly Dictionary<int, long>        idToCharacterId = new();
        private readonly Dictionary<int, string>      idToName        = new();
        private readonly Dictionary<int, int>         idToLevel       = new();

        private readonly ConcurrentQueue<Action> pendingActions = new();
        private readonly SpawnManager spawnManager;

        public IReadOnlyDictionary<NetPeer, int> PeerToId => peerToId;
        public Dictionary<int, Vector3>          Positions => positions;

        private readonly LuaEngine luaEngine = new();

        public PlayerManager(SpawnManager spawnManager)
        {
            this.spawnManager = spawnManager;
        }

        public void DrainActions()
        {
            while (pendingActions.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public void EnqueueAction(Action action)
        {
            pendingActions.Enqueue(action);
        }

        // Called after CharacterLoad response is resolved
        public void HandlePlayerConnected(
            NetPeer peer,
            int accountId,
            CharacterLoadResponse character)
        {
            W2CMOTDPacketSender.Send(peer, ConfigService.Config.MOTD);

            int newId = SpawnPlayer(peer, accountId, character);

            LuaPlayer luaPlayer = new LuaPlayer(peer, newId, accountId);

            string scriptPath = Path.Combine(
                Application.streamingAssetsPath,
                "Lua",
                "Server",
                "on_player_connect.lua");

            luaEngine.RunFile(scriptPath);
            luaEngine.CallFunction("on_player_connect", luaPlayer);

            Debug.Log($"spawnManager null? {spawnManager == null}");
            spawnManager.SendCubesToPeer(peer);

            // Send all existing players to the new joiner
            foreach (var kvp in peerToId)
            {
                int existingId = kvp.Value;

                if (existingId == newId)
                    continue;

                PacketSender.SendPacket(
                    peer,
                    Opcode.SpawnPlayer,
                    new SpawnPlayerPacket
                    {
                        NetworkId     = existingId,
                        x             = positions[existingId].x,
                        y             = positions[existingId].y,
                        z             = positions[existingId].z,
                        IsLocalPlayer = false
                    });
            }
        }

        public void HandlePlayerDisconnected(NetPeer peer)
        {
            if (!peerToId.TryGetValue(peer, out int networkId))
                return;

            // Fire-and-forget save before removing state
            if (idToCharacterId.TryGetValue(networkId, out long characterId))
            {
                string name    = idToName.GetValueOrDefault(networkId, "Unknown");
                int    level   = idToLevel.GetValueOrDefault(networkId, 1);
                Vector3 pos    = positions.GetValueOrDefault(networkId, Vector3.zero);

                _ = SaveCharacterAsync(characterId, name, level, pos);
            }

            peerToId.Remove(peer);
            idToAccount.Remove(networkId);
            positions.Remove(networkId);
            idToCharacterId.Remove(networkId);
            idToName.Remove(networkId);
            idToLevel.Remove(networkId);

            WorldLogger.Info($"Player {networkId} disconnected");

            foreach (var kvp in peerToId)
            {
                PacketSender.SendPacket(
                    kvp.Key,
                    Opcode.PlayerLeave,
                    new PlayerLeavePacket
                    {
                        NetworkId = networkId
                    });
            }
        }

        public void BroadcastPosition(
            NetPeer sender,
            int     networkId,
            Vector3 position)
        {
            foreach (var kvp in peerToId)
            {
                NetPeer peer = kvp.Key;

                if (peer == sender)
                    continue;

                PacketSender.SendPacket(
                    peer,
                    Opcode.PlayerPosition,
                    new PlayerPositionPacket
                    {
                        NetworkId = networkId,
                        x         = position.x,
                        y         = position.y,
                        z         = position.z
                    },
                    DeliveryMethod.Unreliable);
            }
        }

        private int SpawnPlayer(
            NetPeer peer,
            int accountId,
            CharacterLoadResponse character)
        {
            int     networkId     = nextNetworkId++;
            Vector3 spawnPosition = new Vector3(character.X, character.Y, character.Z);

            peerToId[peer]               = networkId;
            idToAccount[networkId]       = accountId;
            positions[networkId]         = spawnPosition;
            idToCharacterId[networkId]   = character.CharacterId;
            idToName[networkId]          = character.Name;
            idToLevel[networkId]         = character.Level;

            foreach (var kvp in peerToId)
            {
                bool isLocal = kvp.Key == peer;

                PacketSender.SendPacket(
                    kvp.Key,
                    Opcode.SpawnPlayer,
                    new SpawnPlayerPacket
                    {
                        NetworkId     = networkId,
                        x             = spawnPosition.x,
                        y             = spawnPosition.y,
                        z             = spawnPosition.z,
                        IsLocalPlayer = isLocal
                    });
            }

            WorldLogger.Info(
                $"Spawned player {networkId} (AccountId={accountId}, CharacterId={character.CharacterId}, Name={character.Name})");

            return networkId;
        }

        private async Task SaveCharacterAsync(
            long characterId,
            string name,
            int level,
            Vector3 pos)
        {
            var persistence = PersistenceClient.Instance;

            if (persistence == null)
            {
                Debug.LogWarning("[PlayerManager] PersistenceClient.Instance is null — skipping save");
                return;
            }

            await persistence.W2PCharacter.Save(
                characterId,
                name,
                level,
                pos.x, pos.y, pos.z);

            WorldLogger.Info($"Saved character {characterId} ({name})");
        }
    }
}