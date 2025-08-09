namespace shared.Messages;

public class UpdateScore(int player1Score, int player2Score) : Message
{
    public int Player1Score { get; } = player1Score;
    public int Player2Score { get; } = player2Score;

    public override byte[] ToBytes()
    {
        var r = new byte[2 + sizeof(int) * 2];
        r[0] = Opcode;
        r[1] = PacketNumber;
        var player1Bytes = Utils.Pack(Player1Score);
        var player2Bytes = Utils.Pack(Player2Score);
        int offset = 2;
        Array.Copy(player1Bytes, 0, r, offset, player1Bytes.Length);
        offset += sizeof(int);
        Array.Copy(player2Bytes, 0, r, offset, player2Bytes.Length);

        return r;
    }

    public override bool RequiresAck(CallMode ackFrom)
    {
        return ackFrom == CallMode.Client;
    }

    public override byte GetOpcode => Opcode;

    public const byte Opcode = 0x04;
}