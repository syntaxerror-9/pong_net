using System.Runtime.InteropServices;

namespace Client;

class Client
{

    public static void Main()
    {
        NativeLibrary.SetDllImportResolver(typeof(Client).Assembly, Raylib.LoadRaylib);
        Console.WriteLine("Hello, World!");
        Raylib.InitWindow(900, 600, "Hello Raylib!");

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.EndDrawing();

        }
        Raylib.CloseWindow();

    }
}

