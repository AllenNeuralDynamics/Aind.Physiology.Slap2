using System;
using System.ComponentModel;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using Bonsai;
using Hexa.NET.ImGui;

/// <summary>
/// Full-width red "Stop" button styled like the other Slap2 panels (same font, theme and
/// rounding), with a stop icon next to the label. The first click emits a single notification;
/// the button then turns grey and shows <see cref="TextStopping"/>, so a stop is only requested once.
/// Source1 = Frame subject (or a table cell notification).
/// </summary>
[Combinator]
[WorkflowElementCategory(ElementCategory.Combinator)]
[Description("Full-width red Stop button in the Slap2 theme. The first click emits one notification, then the button turns grey. Source1 = Frame subject.")]
public class StopButtonVisualizer
{
    private bool visible = true;
    [Description("Specifies whether the control is displayed.")]
    public bool Visible { get { return visible; } set { visible = value; } }

    private string text = "STOP";
    [Description("Label shown before the button is clicked.")]
    public string Text { get { return text; } set { text = value; } }

    private string textStopping = "STOPPING...";
    [Description("Label shown after the button was clicked.")]
    public string TextStopping { get { return textStopping; } set { textStopping = value; } }

    private string tooltip = "End the session and close the log files";
    [Description("Tooltip shown while hovering the button. Leave empty for none.")]
    public string Tooltip { get { return tooltip; } set { tooltip = value; } }

    private float fontSize = 24f;
    [Description("Font size used to render the control.")]
    public float FontSize { get { return fontSize; } set { fontSize = value; } }

    public IObservable<Unit> Process<TTickSource>(IObservable<TTickSource> frames)
    {
        return Observable.Create<Unit>(observer =>
        {
            var stopped = false;
            return frames.SubscribeSafe(Observer.Create<TTickSource>(
                _ =>
                {
                    // Mirrors bonsai-rx/imgui PR #29, not yet in 0.1.0.
                    unsafe { ImGui.GetIO().Handle->ConfigErrorRecoveryEnableAssert = 0; }

                    if (!Visible) return;

                    var clicked = false;
                    var recoveryState = new ImGuiErrorRecoveryState();
                    ImGuiP.ErrorRecoveryStoreState(ref recoveryState);
                    try
                    {
                        ImGui.PushFont(UiFont.Get(), FontSize);
                        UiTheme.Push();
                        clicked = Draw(stopped);
                        UiTheme.Pop();
                        ImGui.PopFont();
                    }
                    catch (Exception ex)
                    {
                        ImGuiP.ErrorRecoveryTryToRecoverState(ref recoveryState);
                        observer.OnError(ex);
                        return;
                    }

                    // Emit after drawing, outside of the ImGui frame, and only once.
                    if (clicked && !stopped)
                    {
                        stopped = true;
                        observer.OnNext(Unit.Default);
                    }
                },
                observer.OnError,
                observer.OnCompleted));
        });
    }

    // Custom-drawn button: rounded red fill (lighter on hover, darker while pressed, slate once
    // stopped), a faint outline, and a centered "[square icon] LABEL".
    bool Draw(bool stopped)
    {
        var fontSize = ImGui.GetFontSize();
        var size = new Vector2(Math.Max(1f, ImGui.GetContentRegionAvail().X), fontSize * 2.4f);
        const float rounding = 8f;

        ImGui.Spacing();
        ImGui.BeginDisabled(stopped);
        var clicked = ImGui.InvisibleButton("##StopButton", size);
        var hovered = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();
        ImGui.EndDisabled();

        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var fill = stopped ? UiTheme.Slate
                 : held ? UiTheme.RedButtonActive
                 : hovered ? UiTheme.RedButtonHovered
                 : UiTheme.RedButton;

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(fill), rounding);
        drawList.AddRect(min, max, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, hovered && !stopped ? 0.22f : 0.10f)), rounding, ImDrawFlags.None, 1f);

        // Centered icon + label.
        var label = stopped ? TextStopping : Text;
        var labelSize = ImGui.CalcTextSize(label);
        var icon = fontSize * 0.6f;
        var gap = fontSize * 0.5f;
        var contentWidth = icon + gap + labelSize.X;
        var left = min.X + (size.X - contentWidth) / 2f;
        var centerY = (min.Y + max.Y) / 2f;
        var textColor = ImGui.ColorConvertFloat4ToU32(stopped ? UiTheme.TextDim : new Vector4(1f, 1f, 1f, 1f));

        var iconMin = new Vector2(left, centerY - icon / 2f);
        drawList.AddRectFilled(iconMin, iconMin + new Vector2(icon, icon), textColor, 2f);
        drawList.AddText(new Vector2(left + icon + gap, centerY - labelSize.Y / 2f), textColor, label);

        if (hovered && !stopped)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (!string.IsNullOrEmpty(Tooltip)) ImGui.SetTooltip(Tooltip);
        }
        return clicked;
    }
}
