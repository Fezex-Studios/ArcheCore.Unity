namespace Shared
{
    public enum Opcode : ushort
    {
        Connect      = 1,
        SpawnPlayer  = 2,
        MOTD         = 3,
        PlayerMove   = 4,
        PlayerPosition = 5,
        Authenticate = 6,
        PlayerLeave  = 7,
        SpawnCube = 8,
        Announcement = 9
    }
}