using System;
using System.IO;
using Hexa.NET.ImGui;

/// <summary>
/// Loads a nicer UI font than ImGui's built-in pixel font. Call <see cref="Get"/> from inside an
/// ImGui frame; the font is loaded once and falls back to the default font if none is found.
/// </summary>
static class UiFont
{
    static readonly string[] Candidates = new string[]
    {
        @"C:\Windows\Fonts\segoeui.ttf",
        @"C:\Windows\Fonts\calibri.ttf",
        @"C:\Windows\Fonts\arial.ttf",
    };

    static ImFontPtr font;
    static bool loaded;
    static IntPtr lastIo;
    static int lastFrame;

    public static unsafe ImFontPtr Get()
    {
        // The cached font belongs to the ImGui context it was loaded into. When the workflow is
        // restarted a new context (and font atlas) is created and the old font pointer dangles, so
        // drop the cache whenever the context changed: a different ImGuiIO address, or a frame
        // counter that went backwards (a fresh context starts counting again).
        var io = (IntPtr)ImGui.GetIO().Handle;
        var frame = ImGui.GetFrameCount();
        if (loaded && (io != lastIo || frame < lastFrame))
        {
            loaded = false;
            font = default(ImFontPtr);
        }
        lastIo = io;
        lastFrame = frame;

        if (!loaded)
        {
            loaded = true;
            foreach (var path in Candidates)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    // ImGuiIOPtr.Fonts isn't supported by Bonsai's script-extensions compiler (CS0570),
                    // so go through the native handle.
                    var atlas = new ImFontAtlasPtr(ImGui.GetIO().Handle->Fonts);
                    font = atlas.AddFontFromFileTTF(path);
                    break;
                }
                catch (Exception)
                {
                    // Try the next candidate.
                }
            }
        }
        return font.IsNull ? ImGui.GetFont() : font;
    }
}
