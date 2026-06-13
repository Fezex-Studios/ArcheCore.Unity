using MessagePack;

namespace Shared.Packets.Requests
{
    [MessagePackObject(true)]
    public class HelloWorldPacket
    {
        public string Message;
    }
}