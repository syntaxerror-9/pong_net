using System.Reflection;
using System.Runtime.InteropServices;

namespace Client;

static partial class Raylib
{
    public const int KEY_J = 74; // Key: J | j
    public const int KEY_K = 75; // Key: K | k
    public const int KEY_DOWN = 264; // Key: Cursor down
    public const int KEY_UP = 265; // Key: Cursor up

    public static nint LoadRaylib(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName.StartsWith("libraylib"))
        {
            string raylibPath = Path.Join(Directory.GetCurrentDirectory(),
                Environment.GetEnvironmentVariable("RAYLIB_BUILD_PATH"), libraryName);
            return NativeLibrary.Load(raylibPath);
        }

        return System.IntPtr.Zero;
    }

    [LibraryImport("libraylib.dylib")]
    public static partial void InitWindow(int width, int height, [MarshalAs(UnmanagedType.LPStr)] string name);

    [LibraryImport("libraylib.dylib", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WindowShouldClose();

    [LibraryImport("libraylib.dylib")]
    public static partial void CloseWindow();

    [LibraryImport("libraylib.dylib")]
    public static partial void BeginDrawing();

    [LibraryImport("libraylib.dylib")]
    public static partial void EndDrawing();

    [LibraryImport("libraylib.dylib")]
    public static partial void ClearBackground(Color color);

    [LibraryImport("libraylib.dylib")]
    public static partial void DrawRectangle(int posX, int posY, int width, int height, Color color);

    [LibraryImport("libraylib.dylib")]
    public static partial void DrawCircle(int centerX, int centerY, float radius, Color color);

    [LibraryImport("libraylib.dylib")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsKeyDown(int key);

    [LibraryImport("libraylib.dylib")]
    public static partial void DrawText([MarshalAs(UnmanagedType.LPStr)] string text, int posX, int posY, int fontSize,
        Color color);

    [LibraryImport("libraylib.dylib")]
    public static partial int MeasureText([MarshalAs(UnmanagedType.LPStr)] string text, int fontSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct Color
    {
        public byte r, g, b, a;
    }
}