using LiteNetLib;
using Shared;
using Shared.Packets;

namespace ArcheCore.WorldServer.Networking.W2C
{
    public class W2CAnnouncementPacketSender
    {
        public static void Send(NetPeer peer, string message)
        {
            PacketSender.SendPacket(peer,Opcode.Announcement,new AnnouncementPacket
            {
                Message = message
            });
        }
    }
}