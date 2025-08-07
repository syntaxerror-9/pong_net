using System.Runtime.InteropServices;
using shared;

namespace server;

public unsafe struct User : IDisposable
{
    public OS.SockAddr* peer_addr { get; private set; }
    public uint peer_length { get; private set; }
    public byte clientId { get; private set; }

    public User(byte clientId, uint peer_length, OS.SockAddr peer_addr)
    {
        this.clientId = clientId;
        this.peer_length = peer_length;
        this.peer_addr = (OS.SockAddr*)NativeMemory.Alloc((uint)sizeof(OS.SockAddr));
        *this.peer_addr = peer_addr;
    }

    public void Dispose()
    {
        if (peer_addr != null)
        {
            Console.WriteLine($"Disposing User {clientId}");
            NativeMemory.Free(peer_addr);
            peer_addr = null;
        }
    }
}