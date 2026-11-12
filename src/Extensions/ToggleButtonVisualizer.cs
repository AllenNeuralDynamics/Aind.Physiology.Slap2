using System;
using System.ComponentModel;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using Bonsai;
using Hexa.NET.ImGui;

/// <summary>
/// ImGui toggle button. Its state comes from the <see cref="Value"/> property (which can be set in
/// the property grid or through a PropertyMapping) and every click flips it and emits the new value.
/// Source1 = Frame subject.
/// </summary>
[Combinator]
[WorkflowElementCategory(ElementCategory.Combinator)]
[Description("Toggle button (blue 'Start' when off, red 'Stop' when on). The state is set through the Value property, and every click flips it and emits the new value. Source1 = Frame subject; Value and Enabled can be set via PropertyMapping.")]
public class ToggleButtonVisualizer
{
    private bool visible = true;
    [Description("Specifies whether the control is displayed.")]
    public bool Visible { get { return visible; } set { visible = value; } }

    private bool enabled = true;
    [Description("Specifies whether the button can be clicked. When false it is displayed greyed out.")]
    public bool Enabled { get { return enabled; } set { enabled = value; } }

    private bool value;
    [Description("The state of the toggle. Setting it does not emit; only clicks do.")]
    public bool Value { get { return value; } set { this.value = value; } }

    private string textOff = "Start";
    [Description("Label shown while Value is false (the button starts the process).")]
    public string TextOff { get { return textOff; } set { textOff = value; } }

    private string textOn = "Stop";
    [Description("Label shown while Value is true (the button stops the process).")]
    public string TextOn { get { return textOn; } set { textOn = value; } }

    private float fontSize = 24f;
    [Description("Font size used to render the control.")]
    public float FontSize { get { return fontSize; } set { fontSize = value; } }

    public IObservable<bool> Process<TTickSource>(IObservable<TTickSource> frames)
    {
        return Observable.Create<bool>(observer =>
        {
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
                        clicked = Draw();
                        UiTheme.Pop();
                        ImGui.PopFont();
                    }
                    catch (Exception ex)
                    {
                        ImGuiP.ErrorRecoveryTryToRecoverState(ref recoveryState);
                        observer.OnError(ex);
                        return;
                    }

                    // Emit after drawing, outside of the ImGui frame.
                    if (clicked) observer.OnNext(Value);
                },
                observer.OnError,
                observer.OnCompleted));
        });
    }

    // Returns true when the button was clicked (Value has already been flipped).
    bool Draw()
    {
        var on = Value;
        var size = new Vector2(-1, Math.Max(ImGui.GetFontSize() * 2.2f, ImGui.GetContentRegionAvail().Y));

        ImGui.BeginDisabled(!Enabled);
        ImGui.PushStyleColor(ImGuiCol.Button, on ? UiTheme.RedButton : UiTheme.BlueButton);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, on ? UiTheme.RedButtonHovered : UiTheme.BlueButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, on ? UiTheme.RedButtonActive : UiTheme.BlueButtonActive);
        var clicked = ImGui.Button((on ? TextOn : TextOff) + "##ToggleButton", size);
        ImGui.PopStyleColor(3);
        ImGui.EndDisabled();

        if (clicked) Value = !on;
        return clicked;
    }
}
