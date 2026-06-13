using MessagePack;
using UnityEngine;

namespace ArcheCore.WorldServer.PersistenceServer.Networking.P2W
{
    public class P2WConnectResponseHandler
        : IPersistencePacketHandler
    {
        public void Handle(Packet packet)
        {
            var response =
                MessagePackSerializer
                    .Deserialize<P2WConnectResponse>(
                        packet.Payload);

            Debug.Log(
                response.Message);
        }
    }
}