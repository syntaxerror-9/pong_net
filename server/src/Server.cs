using System.Runtime.InteropServices;
using shared;


int socket = OS.socket(OS.AF_INET, OS.SOCK_DGRAM, 0);
if (socket == -1)
{
    Console.WriteLine("Socket creation failed.");
    return;
}


unsafe
{
    OS.SockAddrIn socket_addr = new();
    socket_addr.sin_family = (ushort)OS.AF_INET;
    socket_addr.in_port_t = OS.htons(8080);
    socket_addr.in_addr.s_addr = OS.inet_addr("127.0.0.1");
    if (OS.bind(socket, &socket_addr, (uint)Marshal.SizeOf<OS.SockAddrIn>()) == -1)
    {
        Console.WriteLine("Binding socket failed");
        return;
    }

    uint size = 500;
    byte* buffer = stackalloc byte[(int)size];
    int packet_size = OS.recv(socket, buffer, size, 0);

    if (packet_size == -1)
    {
        Console.WriteLine("Failed receiving");
        return;
    }

    Console.WriteLine($"Finished receiving - {packet_size}");
    for (int i = 0; i < packet_size; i++)
    {
        Console.Write(buffer[i] + ",");
    }
}


if (OS.close(socket) == -1)
{
    Console.WriteLine("Closing socket failed.");
}