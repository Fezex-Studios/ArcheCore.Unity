using MessagePack;

namespace Shared.Components
{
    [MessagePackObject(true)]
    public class SpawnCubePacket
    {
        public int   CubeId;

        public float x;
        public float y;
        public float z;
    }
}