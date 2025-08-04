namespace shared.Messages;

public class Echo(byte packetNumber, byte previousOpCode) : Message(packetNumber)
{
    private byte previousOpcode = previousOpCode;

    public override byte[] ToBytes()
    {
        var r = new byte[3];
        r[0] = Opcode;
        r[1] = packetNumber;
        r[2] = previousOpcode;
        return r;
    }

    public override bool RequiresAck(CallMode ackFrom)
    {
        return ackFrom == CallMode.Client;
    }

    public override byte GetOpcode => Opcode;

    public const byte Opcode = 0x06;
}