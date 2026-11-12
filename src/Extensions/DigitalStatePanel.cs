using System;
using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;

/// <summary>
/// ImGui/ImPlot panel that renders a Timestamped&lt;bool&gt; stream as a rolling high/low step plot
/// (x axis = Harp timestamp) with a toolbar on top and a current-state indicator on the right.
/// Used by <see cref="DigitalStateVisualizer"/>.
/// </summary>
class DigitalStatePanel
{
    public static readonly Vector4 HighColor = UiTheme.Blue;
    public static readonly Vector4 LowColor = UiTheme.Slate;
    static readonly int[] WindowPresets = new int[] { 1, 5, 10, 30, 60 };

    private struct Transition
    {
        public double Time;
        public bool Value;

        public Transition(double time, bool value)
        {
            Time = time;
            Value = value;
        }
    }

    readonly List<Transition> transitions = new List<Transition>();
    readonly object gate = new object();
    double latest;
    bool? synchronized;
    bool paused;
    double pausedAt;

    /// <summary>Width of the rolling window, in seconds. Can be changed from the toolbar.</summary>
    public double WindowSeconds = 10;

    /// <summary>Latest synchronization state of the board (unknown until the first value arrives).</summary>
    public void SetSynchronized(bool value)
    {
        synchronized = value;
    }

    public void AddSample(Bonsai.Harp.Timestamped<bool> sample)
    {
        lock (gate)
        {
            // The step plot is fully defined by its transitions.
            if (transitions.Count == 0 || transitions[transitions.Count - 1].Value != sample.Value)
            {
                transitions.Add(new Transition(sample.Seconds, sample.Value));
            }
            latest = sample.Seconds;
            // Keep one transition before the window as the starting level.
            while (transitions.Count > 2 && transitions[1].Time < latest - WindowSeconds)
            {
                transitions.RemoveAt(0);
            }
        }
    }

    // Toolbar above the plot: x axis window length (typed or preset) and a pause toggle that
    // freezes the x axis at the timestamp it was paused on (data keeps being collected).
    void DrawToolbar(double now)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Window");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 5f);
        var window = (float)WindowSeconds;
        if (ImGui.InputFloat("s##DigitalStateWindow", ref window, 0f, 0f, "%.1f"))
        {
            WindowSeconds = Math.Max(0.1, window);
        }
        for (int i = 0; i < WindowPresets.Length; i++)
        {
            ImGui.SameLine();
            if (ImGui.Button(WindowPresets[i].ToString() + "s##DigitalStatePreset" + WindowPresets[i].ToString()))
            {
                WindowSeconds = WindowPresets[i];
            }
        }
        ImGui.SameLine();
        var wasPaused = paused;
        ImGui.Checkbox("Pause##DigitalState", ref paused);
        if (paused && !wasPaused) pausedAt = now;
    }

    public unsafe void Draw()
    {
        Transition[] snapshot;
        double now;
        lock (gate)
        {
            snapshot = transitions.ToArray();
            now = latest;
        }

        DrawToolbar(now);
        if (paused) now = pausedAt;
        var window = WindowSeconds;
        var avail = ImGui.GetContentRegionAvail();
        var indicatorWidth = Math.Min(avail.X * 0.25f, ImGui.GetFontSize() * 6f);

        UiTheme.PushPlot();
        if (ImPlot.BeginPlot("##DigitalState", new Vector2(Math.Max(1f, avail.X - indicatorWidth - 8f), -1), ImPlotFlags.NoTitle | ImPlotFlags.NoLegend))
        {
            var axesFlags = ImPlotAxisFlags.NoHighlight | ImPlotAxisFlags.NoInitialFit;
            ImPlot.SetupAxis(ImAxis.X1, "Harp time (s)", axesFlags);
            ImPlot.SetupAxis(ImAxis.Y1, "", axesFlags | ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.Lock);
            // Rolling window ending at the most recent timestamp; the axis shows the Harp timestamp.
            ImPlot.SetupAxisLimits(ImAxis.X1, now - window, now, ImPlotCond.Always);
            ImPlot.SetupAxisLimits(ImAxis.Y1, -0.2, 1.2, ImPlotCond.Always);
            ImPlot.SetupAxisTicks(ImAxis.Y1, 0, 1, 2, new string[] { "LOW", "HIGH" });

            if (snapshot.Length > 0)
            {
                // Explicit step points (a vertical edge at every transition, then the last level
                // extended to the most recent timestamp) so the line and the fill match exactly.
                var count = snapshot.Length * 2 + 1;
                var xs = new double[count];
                var ys = new double[count];
                var n = 0;
                for (int i = 0; i < snapshot.Length; i++)
                {
                    var level = snapshot[i].Value ? 1.0 : 0.0;
                    if (i > 0)
                    {
                        xs[n] = snapshot[i].Time;
                        ys[n++] = snapshot[i - 1].Value ? 1.0 : 0.0;
                    }
                    xs[n] = snapshot[i].Time;
                    ys[n++] = level;
                }
                xs[n] = Math.Max(now, snapshot[snapshot.Length - 1].Time);
                ys[n++] = snapshot[snapshot.Length - 1].Value ? 1.0 : 0.0;

                fixed (double* x = xs)
                fixed (double* y = ys)
                {
                    ImPlot.SetNextFillStyle(HighColor, 0.22f);
                    ImPlot.PlotShaded("##stateFill", x, y, n, 0.0);
                    ImPlot.SetNextLineStyle(HighColor, 2.5f);
                    ImPlot.PlotLine("state", x, y, n);
                }
            }
            ImPlot.EndPlot();
        }
        UiTheme.PopPlot();

        ImGui.SameLine();
        DrawIndicators(snapshot.Length > 0 && snapshot[snapshot.Length - 1].Value, synchronized, indicatorWidth);
    }

    // Side column with two stacked state indicators: the digital input (blue = HIGH, slate = LOW)
    // on top and the board synchronization (blue = synchronized, red = not synchronized, slate =
    // unknown) below it. Each is a filled circle with a caption above and the state underneath.
    static void DrawIndicators(bool high, bool? synchronized, float width)
    {
        ImGui.BeginChild("##DigitalStateIndicator", new Vector2(width, -1));
        var avail = ImGui.GetContentRegionAvail();
        var origin = ImGui.GetCursorScreenPos();
        var half = avail.Y / 2f;

        DrawIndicator(origin, new Vector2(avail.X, half), "DIGITAL IN", high ? "HIGH" : "LOW", high ? HighColor : LowColor, high);

        string syncLabel = synchronized.HasValue ? (synchronized.Value ? "SYNCED" : "NOT SYNCED") : "UNKNOWN";
        var syncColor = synchronized.HasValue ? (synchronized.Value ? UiTheme.Blue : UiTheme.Red) : UiTheme.Slate;
        DrawIndicator(new Vector2(origin.X, origin.Y + half), new Vector2(avail.X, half), "SYNC", syncLabel, syncColor, synchronized.HasValue);
        ImGui.EndChild();
    }

    // Draws one indicator centered in the given screen-space cell: caption, circle (with a soft
    // halo when active), then the state label.
    static void DrawIndicator(Vector2 cellOrigin, Vector2 cellSize, string caption, string label, Vector4 color, bool halo)
    {
        var captionSize = ImGui.CalcTextSize(caption);
        var labelSize = ImGui.CalcTextSize(label);
        var gap = 8f;
        var radius = Math.Max(6f, Math.Min(cellSize.X * 0.5f - 10f, (cellSize.Y - captionSize.Y - labelSize.Y - gap * 2f) * 0.5f - 6f));
        var blockHeight = captionSize.Y + gap + radius * 2f + gap + labelSize.Y;

        var top = cellOrigin.Y + Math.Max(0f, (cellSize.Y - blockHeight) / 2f);
        var centerX = cellOrigin.X + cellSize.X / 2f;
        var center = new Vector2(centerX, top + captionSize.Y + gap + radius);
        var drawList = ImGui.GetWindowDrawList();

        ImGui.SetCursorScreenPos(new Vector2(centerX - captionSize.X / 2f, top));
        ImGui.TextColored(UiTheme.TextDim, caption);

        if (halo)
        {
            drawList.AddCircleFilled(center, radius + 5f, ImGui.ColorConvertFloat4ToU32(new Vector4(color.X, color.Y, color.Z, 0.25f)), 64);
        }
        drawList.AddCircleFilled(center, radius, ImGui.ColorConvertFloat4ToU32(color), 64);
        drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.25f)), 64, 2f);

        ImGui.SetCursorScreenPos(new Vector2(centerX - labelSize.X / 2f, center.Y + radius + gap));
        ImGui.TextColored(color, label);
    }
}
