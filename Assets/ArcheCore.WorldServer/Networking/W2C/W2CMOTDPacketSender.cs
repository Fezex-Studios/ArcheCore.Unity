// Server/Networking/W2C/W2CMOTDPacket.cs
using LiteNetLib;
using Shared;
using Shared.Packets;

namespace ArcheCore.WorldServer.Networking.W2C
{
    public static class W2CMOTDPacketSender
    {
        public static void Send(
            NetPeer peer,
            string message)
        {
            PacketSender.SendPacket(
                peer,
                Opcode.MOTD,
                new MOTDPacket
                {
                    Message = message
                });
        }
    }
}