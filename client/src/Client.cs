using System.Runtime.InteropServices;
using shared;

namespace Client;

internal class Client
{
    public static unsafe void CreateConnection()
    {
        OS.AddrInfo hints = new();
        OS.AddrInfo* result = null;

        hints.ai_family = OS.AF_INET;
        hints.ai_socktype = OS.SOCK_DGRAM;

        int gai_error = OS.getaddrinfo("127.0.0.1", "8080", &hints, &result);

        if (gai_error != 0)
        {
            Console.WriteLine($"Get addr info failed: {Marshal.GetLastPInvokeErrorMessage()}");
            return;
        }

        int socketfd = OS.socket(result->ai_family, result->ai_socktype, result->ai_protocol);
        if (socketfd == -1)
        {
            Console.WriteLine($"Socket creation failed. {Marshal.GetLastPInvokeErrorMessage()}");
            OS.freeaddrinfo(result);
            return;
        }

        // Cast ai_addr to the correct type for connect
        if (OS.connect(socketfd, result->ai_addr, result->ai_addrlen) != 0)
        {
            Console.WriteLine(
                $"Connect failed: {Marshal.GetLastPInvokeErrorMessage()} - {OS.SockAddrInToString((OS.SockAddrIn*)result->ai_addr)}");
            OS.close(socketfd);
            OS.freeaddrinfo(result);
            return;
        }

        OS.freeaddrinfo(result);

        uint size = 400;
        byte* buffer = stackalloc byte[(int)size];
        for (int i = 0; i < size; i++)
        {
            buffer[i] = 10;
        }

        if (OS.write(socketfd, buffer, size) == -1)
        {
            Console.WriteLine($"Write failed: {Marshal.GetLastPInvokeErrorMessage()}");
            OS.close(socketfd);
            return;
        }


        Console.WriteLine("Write successful!");

        OS.close(socketfd);
    }

    public static void Main()
    {
        NativeLibrary.SetDllImportResolver(typeof(Client).Assembly, Raylib.LoadRaylib);
        Console.WriteLine("Hello");
        CreateConnection();

        /*

        OS.printf("Hello From Client\n");

        Raylib.InitWindow(900, 600, "Hello Raylib!");

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
        */
    }
}