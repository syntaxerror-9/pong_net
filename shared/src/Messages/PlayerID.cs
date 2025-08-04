namespace shared.Messages;

public class PlayerID(byte packetNumber, byte playerId) : Message(packetNumber)
{
    private byte playerId = playerId;
    public byte PlayerId => playerId;

    public override byte[] ToBytes()
    {
        var r = new byte[3];
        r[0] = Opcode;
        r[1] = packetNumber;
        r[2] = playerId;
        return r;
    }

    public override bool RequiresAck(CallMode ackFrom)
    {
        return ackFrom == CallMode.Client;
    }

    public override byte GetOpcode => Opcode;

    public const byte Opcode = 0x07;
}