using System.Reflection;
using System.Runtime.InteropServices;

namespace Client;

static partial class Raylib
{
    public static nint LoadRaylib(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {

        if (libraryName.StartsWith("libraylib"))
        {
            string raylibPath = Path.Join(Directory.GetCurrentDirectory(), Environment.GetEnvironmentVariable("RAYLIB_BUILD_PATH"), libraryName);
            return NativeLibrary.Load(raylibPath);
        }
        return System.IntPtr.Zero;
    }
    [LibraryImport("libraylib.dylib")]
    public static partial void InitWindow(int width, int height, [MarshalAs(UnmanagedType.LPStr)] string name);

    [LibraryImport("libraylib.dylib")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WindowShouldClose();

    [LibraryImport("libraylib.dylib")]
    public static partial void CloseWindow();

    [LibraryImport("libraylib.dylib")]
    public static partial void BeginDrawing();

    [LibraryImport("libraylib.dylib")]
    public static partial void EndDrawing();

}

