using System.Runtime.InteropServices;

namespace shared;

public static class Net
{
    public static unsafe void SendMessage(int socketfd, shared.Messages.Message message, OS.SockAddr* sockAddr,
        uint sockLen)
    {
        var messageBytes = message.ToBytes();
        unsafe
        {
            byte* buffer = stackalloc byte[messageBytes.Length];
            new Span<byte>(messageBytes).CopyTo(new Span<byte>(buffer, messageBytes.Length));
            if (OS.sendto(socketfd, buffer, messageBytes.Length, 0, sockAddr, sockLen) ==
                -1)
            {
                throw new Exception($"sendto failed: {Marshal.GetLastPInvokeErrorMessage()}");
            }
        }
    }
}