using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bonsai;
using Hexa.NET.ImGui;

/// <summary>
/// ImGui form for the Session model. Emits the <see cref="AindPhysiologySlap2.Session"/> it
/// describes every time a field changes (and once with the initial values). Fields that cannot be
/// changed are greyed out, and the whole form can be locked through <see cref="Enabled"/>.
/// Source1 = Frame subject.
/// </summary>
[Combinator]
[WorkflowElementCategory(ElementCategory.Combinator)]
[Description("Session form. Emits the Session every time a field changes (and once with the initial values). Fields that cannot be changed are greyed out; Enabled locks the whole form. Source1 = Frame subject; Enabled can be set via PropertyMapping.")]
public class SessionFormVisualizer
{
    static readonly Vector4 AccentColor = UiTheme.Blue;
    static readonly Vector4 DimColor = UiTheme.TextDim;
    static readonly Vector4 ErrorColor = UiTheme.Red;
    static readonly UIntPtr TextCapacity = (UIntPtr)256;
    const string DateFormat = "yyyy-MM-ddTHH:mm:ss'Z'";

    private bool visible = true;
    [Description("Specifies whether the control is displayed.")]
    public bool Visible { get { return visible; } set { visible = value; } }

    private float fontSize = 24f;
    [Description("Font size used to render the control.")]
    public float FontSize { get { return fontSize; } set { fontSize = value; } }

    private bool enabled = true;
    [Description("Specifies whether the fields can be edited. When false the form is still displayed but locked.")]
    public bool Enabled { get { return enabled; } set { enabled = value; } }

    private float bottomMargin = 80f;
    [Description("Height, in pixels, left free under the form for visualizers composed below it (e.g. the Start/Stop toggle).")]
    public float BottomMargin { get { return bottomMargin; } set { bottomMargin = value; } }

    private bool allowDirtyRepo;
    [Description("Initial value of the Allow Dirty Repo checkbox.")]
    public bool AllowDirtyRepo { get { return allowDirtyRepo; } set { allowDirtyRepo = value; } }

    private bool skipHardwareValidation;
    [Description("Initial value of the Skip Hardware Validation checkbox.")]
    public bool SkipHardwareValidation { get { return skipHardwareValidation; } set { skipHardwareValidation = value; } }

    // Editable state of the form. Lives for the lifetime of the subscription.
    class FormState
    {
        public string Experiment = string.Empty;
        public string Experimenter = string.Empty;
        public string Date = DateTimeOffset.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture);
        public string SessionName = string.Empty;
        public string Subject = string.Empty;
        public string Notes = string.Empty;
        public bool AllowDirtyRepo;
        public bool SkipHardwareValidation;
        public string LastSignature;
    }

    public IObservable<AindPhysiologySlap2.Session> Process<TTickSource>(IObservable<TTickSource> frames)
    {
        return Observable.Create<AindPhysiologySlap2.Session>(observer =>
        {
            var form = new FormState();
            form.AllowDirtyRepo = AllowDirtyRepo;
            form.SkipHardwareValidation = SkipHardwareValidation;

            var frameSub = frames.SubscribeSafe(Observer.Create<TTickSource>(
                _ =>
                {
                    // Mirrors bonsai-rx/imgui PR #29, not yet in 0.1.0.
                    unsafe { ImGui.GetIO().Handle->ConfigErrorRecoveryEnableAssert = 0; }

                    if (!Visible) return;

                    AindPhysiologySlap2.Session changed = null;
                    var recoveryState = new ImGuiErrorRecoveryState();
                    ImGuiP.ErrorRecoveryStoreState(ref recoveryState);
                    try
                    {
                        ImGui.PushFont(UiFont.Get(), FontSize);
                        UiTheme.Push();
                        changed = Draw(form);
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
                    if (changed != null) observer.OnNext(changed);
                },
                observer.OnError,
                observer.OnCompleted));

            return frameSub;
        });
    }

    // Returns the Session when any field changed since the last frame (or on the first frame),
    // otherwise null.
    AindPhysiologySlap2.Session Draw(FormState form)
    {
        var formHeight = Math.Max(60f, ImGui.GetContentRegionAvail().Y - BottomMargin);

        ImGui.BeginChild("##SessionForm", new Vector2(-1, formHeight), ImGuiChildFlags.Borders);
        DrawForm(form);
        ImGui.EndChild();

        DateTimeOffset date;
        if (!TryParseDate(form.Date, out date)) return null;
        var signature = Signature(form);
        if (signature == form.LastSignature) return null;
        form.LastSignature = signature;
        return CreateSession(form, date);
    }

    static string Signature(FormState form)
    {
        return string.Join("", new string[]
        {
            form.Experiment, form.Experimenter, form.Date, form.SessionName, form.Subject, form.Notes,
            form.AllowDirtyRepo ? "1" : "0", form.SkipHardwareValidation ? "1" : "0",
        });
    }

    // ImGuiStylePtr.ItemSpacing isn't supported by Bonsai's script-extensions compiler (CS0570),
    // so read it through the native handle.
    static Vector2 ItemSpacing()
    {
        unsafe { return ImGui.GetStyle().Handle->ItemSpacing; }
    }

    void DrawForm(FormState form)
    {
        ImGui.TextColored(AccentColor, "SESSION");
        var status = Enabled ? "Ready" : "●  Locked";
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0f, ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(status).X));
        ImGui.TextColored(Enabled ? DimColor : ErrorColor, status);
        ImGui.Separator();

        var labelWidth = ImGui.GetFontSize() * 9f;
        var actionWidth = ImGui.GetFontSize() * 4.5f;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 5f));
        if (ImGui.BeginTable("##SessionFields", 3, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("label", ImGuiTableColumnFlags.WidthFixed, labelWidth);
            ImGui.TableSetupColumn("field", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("action", ImGuiTableColumnFlags.WidthFixed, actionWidth);

            // Fields that cannot be changed are greyed out.
            ReadOnlyRow("Schema version", new AindPhysiologySlap2.Session().Version);
            ReadOnlyRow("Services version", new AindPhysiologySlap2.Session().AindBehaviorServicesPkgVersion);
            ReadOnlyRow("Commit hash", "(set by the launcher)");

            ImGui.BeginDisabled(!Enabled);

            TextRow("Experiment", "##experiment", "optional", ref form.Experiment);
            TextRow("Experimenter", "##experimenter", "comma separated", ref form.Experimenter);
            TextRow("Subject", "##subject", "required", ref form.Subject);

            // Timestamp with a button that fills in the current UTC time.
            DateTimeOffset date;
            var dateValid = TryParseDate(form.Date, out date);
            if (TextRowWithAction("Date", "##date", "yyyy-MM-ddTHH:mm:ssZ", ref form.Date, "Now##date", !dateValid))
            {
                form.Date = DateTimeOffset.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture);
            }

            // Session name with a button that generates it the way the pydantic model does.
            var hasSubject = !string.IsNullOrWhiteSpace(form.Subject);
            var generated = GenerateSessionName(hasSubject ? form.Subject : "<subject>", dateValid ? date : DateTimeOffset.UtcNow);
            if (TextRowWithAction("Session name", "##sessionName", generated, ref form.SessionName, "Auto##sessionName", false, !hasSubject))
            {
                form.SessionName = generated;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(DimColor, "Options");
            ImGui.TableNextColumn();
            ImGui.Checkbox("Allow dirty repo", ref form.AllowDirtyRepo);
            ImGui.SameLine();
            ImGui.Checkbox("Skip hardware validation", ref form.SkipHardwareValidation);

            // Notes takes whatever height is left in the form.
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(DimColor, "Notes");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var notesHeight = Math.Max(ImGui.GetFontSize() * 3.5f, ImGui.GetContentRegionAvail().Y - 4f);
            ImGui.InputTextMultiline("##notes", ref form.Notes, TextCapacity, new Vector2(-1, notesHeight));

            ImGui.EndDisabled();
            ImGui.EndTable();
        }
        ImGui.PopStyleVar();
    }

    static void ReadOnlyRow(string label, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(DimColor, label);
        ImGui.TableNextColumn();
        ImGui.BeginDisabled(true);
        ImGui.SetNextItemWidth(-1);
        var copy = value;
        ImGui.InputText("##ro" + label, ref copy, TextCapacity, ImGuiInputTextFlags.ReadOnly);
        ImGui.EndDisabled();
    }

    static void TextRow(string label, string id, string hint, ref string value)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(DimColor, label);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint(id, hint, ref value, TextCapacity);
    }

    // Returns true when the side button was pressed.
    static bool TextRowWithAction(string label, string id, string hint, ref string value, string buttonLabel, bool invalid, bool actionDisabled = false)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(DimColor, label);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (invalid) ImGui.PushStyleColor(ImGuiCol.Text, ErrorColor);
        ImGui.InputTextWithHint(id, hint, ref value, TextCapacity);
        if (invalid) ImGui.PopStyleColor();
        ImGui.TableNextColumn();
        ImGui.BeginDisabled(actionDisabled);
        var pressed = ImGui.Button(buttonLabel, new Vector2(-1, 0));
        ImGui.EndDisabled();
        return pressed;
    }

    static AindPhysiologySlap2.Session CreateSession(FormState form, DateTimeOffset date)
    {
        var session = new AindPhysiologySlap2.Session();
        session.Experiment = string.IsNullOrWhiteSpace(form.Experiment) ? null : form.Experiment.Trim();
        var experimenters = new List<string>();
        foreach (var name in form.Experimenter.Split(','))
        {
            var trimmed = name.Trim();
            if (trimmed.Length > 0) experimenters.Add(trimmed);
        }
        session.Experimenter = experimenters;
        session.Date = date;
        session.Subject = form.Subject.Trim();
        // Same rule as the pydantic model: no name given -> generated from subject and date.
        session.SessionName = string.IsNullOrWhiteSpace(form.SessionName)
            ? GenerateSessionName(session.Subject, date)
            : form.SessionName.Trim();
        session.Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes;
        session.AllowDirtyRepo = form.AllowDirtyRepo;
        session.SkipHardwareValidation = form.SkipHardwareValidation;
        return session;
    }

    static bool TryParseDate(string text, out DateTimeOffset date)
    {
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out date);
    }

    // Mirrors aind_behavior_services: f"{subject}_{format_datetime(date)}", where UTC dates end
    // in 'Z' and other offsets in +HHMM.
    static string GenerateSessionName(string subject, DateTimeOffset date)
    {
        string stamp;
        if (date.Offset == TimeSpan.Zero)
        {
            stamp = date.ToString("yyyy-MM-ddTHHmmss'Z'", CultureInfo.InvariantCulture);
        }
        else
        {
            var sign = date.Offset < TimeSpan.Zero ? "-" : "+";
            var offset = date.Offset.Duration();
            stamp = date.ToString("yyyy-MM-ddTHHmmss", CultureInfo.InvariantCulture) + sign + offset.Hours.ToString("00") + offset.Minutes.ToString("00");
        }
        return (subject ?? string.Empty).Trim() + "_" + stamp;
    }
}
