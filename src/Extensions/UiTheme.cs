using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;

/// <summary>
/// Shared dark theme (navy surfaces, blue accents, red for stop/alerts) for the Slap2 ImGui
/// visualizers. Call <see cref="Push"/> after pushing the font and <see cref="Pop"/> before popping it.
/// </summary>
static class UiTheme
{
    public static readonly Vector4 Background = new Vector4(0.055f, 0.065f, 0.095f, 1f);
    public static readonly Vector4 Surface = new Vector4(0.085f, 0.100f, 0.145f, 1f);
    public static readonly Vector4 Field = new Vector4(0.115f, 0.135f, 0.195f, 1f);
    public static readonly Vector4 Border = new Vector4(0.20f, 0.24f, 0.34f, 1f);
    public static readonly Vector4 Text = new Vector4(0.90f, 0.92f, 0.97f, 1f);
    public static readonly Vector4 TextDim = new Vector4(0.55f, 0.60f, 0.70f, 1f);
    public static readonly Vector4 Blue = new Vector4(0.30f, 0.60f, 1.00f, 1f);
    public static readonly Vector4 BlueButton = new Vector4(0.15f, 0.34f, 0.68f, 1f);
    public static readonly Vector4 BlueButtonHovered = new Vector4(0.20f, 0.44f, 0.84f, 1f);
    public static readonly Vector4 BlueButtonActive = new Vector4(0.12f, 0.27f, 0.55f, 1f);
    public static readonly Vector4 Red = new Vector4(0.90f, 0.30f, 0.32f, 1f);
    public static readonly Vector4 RedButton = new Vector4(0.62f, 0.17f, 0.20f, 1f);
    public static readonly Vector4 RedButtonHovered = new Vector4(0.80f, 0.24f, 0.27f, 1f);
    public static readonly Vector4 RedButtonActive = new Vector4(0.50f, 0.13f, 0.16f, 1f);
    public static readonly Vector4 Slate = new Vector4(0.27f, 0.31f, 0.42f, 1f);

    const int ColorCount = 16;
    const int VarCount = 4;

    public static void Push()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, Text);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, TextDim);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Surface);
        ImGui.PushStyleColor(ImGuiCol.Border, Border);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Field);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.15f, 0.18f, 0.26f, 1f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.18f, 0.22f, 0.32f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Button, BlueButton);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BlueButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, BlueButtonActive);
        ImGui.PushStyleColor(ImGuiCol.CheckMark, Blue);
        ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, new Vector4(0.30f, 0.60f, 1.00f, 0.35f));
        ImGui.PushStyleColor(ImGuiCol.Separator, Border);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Surface);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, Border);
        ImGui.PushStyleColor(ImGuiCol.PopupBg, Surface);

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 8f));
    }

    public static void Pop()
    {
        ImGui.PopStyleVar(VarCount);
        ImGui.PopStyleColor(ColorCount);
    }

    // Paints the whole canvas window navy, underneath everything drawn after it.
    public static void PaintBackground()
    {
        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        ImGui.GetWindowDrawList().AddRectFilled(position, position + size, ImGui.ColorConvertFloat4ToU32(Background));
    }

    // ImPlot colors for the plot area; wrap BeginPlot ... EndPlot.
    public static void PushPlot()
    {
        ImPlot.PushStyleColor(ImPlotCol.FrameBg, Surface);
        ImPlot.PushStyleColor(ImPlotCol.Bg, Background);
        ImPlot.PushStyleColor(ImPlotCol.Border, Border);
        ImPlot.PushStyleColor(ImPlotCol.AxisGrid, new Vector4(1f, 1f, 1f, 0.08f));
        ImPlot.PushStyleColor(ImPlotCol.AxisText, TextDim);
    }

    public static void PopPlot()
    {
        ImPlot.PopStyleColor(5);
    }
}
