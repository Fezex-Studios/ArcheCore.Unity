using MessagePack;

[MessagePackObject(true)]
public class Packet
{
    public ushort Opcode;

    public byte[] Payload;
}