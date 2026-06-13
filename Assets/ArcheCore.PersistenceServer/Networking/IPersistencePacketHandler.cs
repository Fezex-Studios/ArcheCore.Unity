using MessagePack;

namespace ArcheCore.WorldServer.PersistenceServer.Networking
{
    public interface IPersistencePacketHandler
    {
        void Handle(Packet packet);
    }
}