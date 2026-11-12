using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bonsai;
using Hexa.NET.ImGui;

/// <summary>
/// Renders a Timestamped&lt;bool&gt; stream as a rolling step plot (high/low) whose x axis is the
/// Harp timestamp, with a small indicator on the right showing the current state.
/// Source1 = Frame subject (ImPlot visualizer ticks); Source2 = the Timestamped&lt;bool&gt; stream.
/// </summary>
[Combinator]
[WorkflowElementCategory(ElementCategory.Combinator)]
[Description("Renders a Timestamped<bool> stream as a rolling high/low step plot (x axis = Harp timestamp, in seconds) with a current-state indicator. Source1 = Frame subject; Source2 = IObservable<Timestamped<bool>>.")]
public class DigitalStateVisualizer
{
    private bool visible = true;
    [Description("Specifies whether the control is displayed.")]
    public bool Visible { get { return visible; } set { visible = value; } }

    private float fontSize = 24f;
    [Description("Font size used to render the control.")]
    public float FontSize { get { return fontSize; } set { fontSize = value; } }

    private double windowSeconds = 10;
    [Description("The rolling time window, in seconds, shown on the plot. The window always ends at the most recent timestamp.")]
    public double WindowSeconds { get { return windowSeconds; } set { windowSeconds = value; } }

    private float heightFraction = 0.4f;
    [Description("Fraction of the canvas height used by the plot, so that visualizers composed below it (e.g. the session form) have room. Use 0 to fill the whole canvas.")]
    public float HeightFraction { get { return heightFraction; } set { heightFraction = value; } }

    public IObservable<Unit> Process<TTickSource>(IObservable<TTickSource> frames, IObservable<Bonsai.Harp.Timestamped<bool>> samples)
    {
        return Observable.Create<Unit>(observer =>
        {
            var panel = new DigitalStatePanel();
            panel.WindowSeconds = WindowSeconds;

            var samplesSub = samples.SubscribeSafe(Observer.Create<Bonsai.Harp.Timestamped<bool>>(
                panel.AddSample,
                observer.OnError));

            var frameSub = frames.SubscribeSafe(Observer.Create<TTickSource>(
                _ =>
                {
                    // Mirrors bonsai-rx/imgui PR #29, not yet in 0.1.0.
                    unsafe { ImGui.GetIO().Handle->ConfigErrorRecoveryEnableAssert = 0; }

                    if (!Visible) { observer.OnNext(Unit.Default); return; }

                    var recoveryState = new ImGuiErrorRecoveryState();
                    ImGuiP.ErrorRecoveryStoreState(ref recoveryState);
                    try
                    {
                        ImGui.PushFont(UiFont.Get(), FontSize);
                        UiTheme.PaintBackground();
                        UiTheme.Push();
                        var height = HeightFraction > 0 ? ImGui.GetContentRegionAvail().Y * Math.Min(1f, HeightFraction) : -1f;
                        ImGui.BeginChild("##DigitalStateChild", new System.Numerics.Vector2(-1, height));
                        panel.Draw();
                        ImGui.EndChild();
                        UiTheme.Pop();
                        ImGui.PopFont();
                    }
                    catch (Exception ex)
                    {
                        ImGuiP.ErrorRecoveryTryToRecoverState(ref recoveryState);
                        observer.OnError(ex);
                        return;
                    }

                    observer.OnNext(Unit.Default);
                },
                observer.OnError,
                observer.OnCompleted));

            return new CompositeDisposable(samplesSub, frameSub);
        });
    }
}
