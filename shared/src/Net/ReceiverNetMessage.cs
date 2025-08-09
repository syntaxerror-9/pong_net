namespace shared;

// If you received a net message and you have to ack it back
public unsafe class ReceiverNetMessage(
    Messages.Message message,
    OS.SockAddr* targetAddr,
    uint targetAddrLen,
    int socketfd
) : NetMessage(message, targetAddr, targetAddrLen, socketfd)
{
    private const long EXPIRE_MILLISECONDS = 1000;

    public bool Expired()
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (now - timer >= EXPIRE_MILLISECONDS)
        {
            return true;
        }

        return false;
    }

    public ReceiverNetMessage SendAck()
    {
        Console.WriteLine($"Sending ack for {message.GetOpcode} {message.PacketNumber}");
        var ackMessage = new Messages.Acknowledgment(message.GetOpcode) { PacketNumber = message.PacketNumber };
        Net.SendMessage(socketfd, ackMessage, targetAddr, targetAddrLen);
        return this;
    }
}