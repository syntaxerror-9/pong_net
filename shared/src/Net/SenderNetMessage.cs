using shared.Messages;

namespace shared;

// If you're sending a net message and need a ack back
public unsafe class SenderNetMessage(
    Message message,
    OS.SockAddr* targetAddr,
    uint targetAddrLen,
    int socketfd,
    Action _cb) : NetMessage(message, targetAddr, targetAddrLen, socketfd)
{
    public Action cb => _cb;

    private const long TIMEOUT_MILLISECONDS = 100;

    public void Tick()
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (now - timer >= TIMEOUT_MILLISECONDS)
        {
            timer = now;
            SendMessage();
        }
    }

    public SenderNetMessage SendMessage()
    {
        Net.SendMessage(socketfd, message, targetAddr, targetAddrLen);
        return this;
    }
}