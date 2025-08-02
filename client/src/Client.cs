using System.Runtime.InteropServices;
using shared;

namespace Client;

class Client
{

    public static void Main()
    {
        NativeLibrary.SetDllImportResolver(typeof(Client).Assembly, Raylib.LoadRaylib);
        OS.printf("Hello From Client\n");

        Raylib.InitWindow(900, 600, "Hello Raylib!");

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.EndDrawing();

        }
        Raylib.CloseWindow();

    }
}

