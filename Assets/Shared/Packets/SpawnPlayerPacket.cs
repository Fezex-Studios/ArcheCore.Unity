using MessagePack;

namespace Shared.Components
{
    [MessagePackObject(true)]
    public class SpawnPlayerPacket
    {
        public int NetworkId;

        public float x;
        public float y;
        public float z;

        public bool IsLocalPlayer;
    }
}