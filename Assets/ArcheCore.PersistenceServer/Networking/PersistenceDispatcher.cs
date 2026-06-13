using System.Collections.Generic;

namespace ArcheCore.WorldServer.PersistenceServer.Networking
{
    public class PersistenceDispatcher
    {
        private readonly Dictionary<
                PersistenceOpcode,
                IPersistencePacketHandler>
            handlers = new();

        public void Register(
            PersistenceOpcode opcode,
            IPersistencePacketHandler handler)
        {
            handlers[opcode] =
                handler;
        }

        public void Handle(Packet packet)
        {
            PersistenceOpcode opcode =
                (PersistenceOpcode)
                packet.Opcode;

            if(handlers.TryGetValue(
                   opcode,
                   out var handler))
            {
                handler.Handle(packet);
            }
        }
    }
}