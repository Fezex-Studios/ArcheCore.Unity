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

        private readonly Dictionary<NetPeer, int>  peerToId        = new();
        private readonly Dictionary<int, int>      idToAccount     = new();
        private readonly Dictionary<int, Vector3>  positions       = new();
        private readonly Dictionary<int, long>     idToCharacterId = new();
        private readonly Dictionary<int, string>   idToName        = new();
        private readonly Dictionary<int, int>      idToLevel       = new();

        private readonly ConcurrentQueue<Action> pendingActions = new();

        private readonly SpawnManager       _spawnManager;
        private readonly ReplicationManager _replication;
        private readonly LuaEngine          luaEngine = new();

        public IReadOnlyDictionary<NetPeer, int> PeerToId  => peerToId;
        public Dictionary<int, Vector3>          Positions => positions;

        public PlayerManager(
            SpawnManager spawnManager,
            ReplicationManager replication)
        {
            _spawnManager = spawnManager;
            _replication  = replication;
        }

        public void DrainActions()
        {
            while (pendingActions.TryDequeue(out Action action))
            {
                try   { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public void EnqueueAction(Action action)
        {
            pendingActions.Enqueue(action);
        }

        public void HandlePlayerConnected(
            NetPeer peer,
            int accountId,
            P2WCharacterLoadResponse character)
        {
            W2CMOTDPacketSender.Send(_replication, peer, ConfigService.Config.MOTD);

            int newId = SpawnPlayer(peer, accountId, character);

            LuaPlayer luaPlayer = new LuaPlayer(peer, newId, accountId,_replication);
            string scriptPath = Path.Combine(
                Directory.GetCurrentDirectory(), "Lua", "Server", "on_player_connect.lua");
            luaEngine.RunFile(scriptPath);
            luaEngine.CallFunction("on_player_connect", luaPlayer);

            _spawnManager.SendCubesToPeer(peer);

            foreach (var kvp in peerToId)
            {
                int existingId = kvp.Value;
                if (existingId == newId) continue;

                W2CSpawnPlayerPacketSender.Send(
                    _replication,
                    peer,
                    existingId,
                    positions[existingId],
                    false);
            }
        }

        public void HandlePlayerDisconnected(NetPeer peer)
        {
            if (!peerToId.TryGetValue(peer, out int networkId))
                return;

            if (idToCharacterId.TryGetValue(networkId, out long characterId))
            {
                string  name  = idToName.GetValueOrDefault(networkId, "Unknown");
                int     level = idToLevel.GetValueOrDefault(networkId, 1);
                Vector3 pos   = positions.GetValueOrDefault(networkId, Vector3.zero);

                _ = SaveCharacterAsync(characterId, name, level, pos);
            }

            peerToId.Remove(peer);
            idToAccount.Remove(networkId);
            positions.Remove(networkId);
            idToCharacterId.Remove(networkId);
            idToName.Remove(networkId);
            idToLevel.Remove(networkId);

            WorldLogger.Info($"Player {networkId} disconnected");

            W2CPlayerLeavePacketSender.Send(_replication, peerToId.Keys, networkId);
        }

        public void BroadcastPosition(
            NetPeer sender,
            int     networkId,
            Vector3 position)
        {
            positions[networkId] = position;

            W2CPlayerPositionPacketSender.SendUnreliable(
                _replication,
                peerToId.Keys,
                sender,
                networkId,
                position);
        }

        private int SpawnPlayer(
            NetPeer peer,
            int accountId,
            P2WCharacterLoadResponse character)
        {
            int     networkId     = nextNetworkId++;
            Vector3 spawnPosition = new Vector3(character.X, character.Y, character.Z);

            peerToId[peer]             = networkId;
            idToAccount[networkId]     = accountId;
            positions[networkId]       = spawnPosition;
            idToCharacterId[networkId] = character.CharacterId;
            idToName[networkId]        = character.Name;
            idToLevel[networkId]       = character.Level;

            foreach (var kvp in peerToId)
            {
                W2CSpawnPlayerPacketSender.Send(
                    _replication,
                    kvp.Key,
                    networkId,
                    spawnPosition,
                    kvp.Key == peer);
            }

            WorldLogger.Info(
                $"Spawned player {networkId} (AccountId={accountId}, CharacterId={character.CharacterId}, Name={character.Name})");

            return networkId;
        }

        private async Task SaveCharacterAsync(
            long characterId, string name, int level, Vector3 pos)
        {
            var persistence = PersistenceClient.Instance;

            if (persistence == null)
            {
                Debug.LogWarning("[PlayerManager] PersistenceClient.Instance is null — skipping save");
                return;
            }

            await persistence.W2PCharacter.Save(characterId, name, level, pos.x, pos.y, pos.z);
            WorldLogger.Info($"Saved character {characterId} ({name})");
        }
    }
}