using LiteNetLib;
using MMO.Shared.Packets;
using Shared;
using Shared.Components;

namespace ArcheCore.WorldServer.Networking.W2C
{
    public static class W2CSpawnCubePacketSender
    {
        public static void Send(
            NetPeer peer,
            int     cubeId,
            float   x,
            float   y,
            float   z)
        {
            PacketSender.SendPacket(
                peer,
                Opcode.SpawnCube,
                new SpawnCubePacket
                {
                    CubeId = cubeId,
                    x      = x,
                    y      = y,
                    z      = z
                });
        }
    }
}