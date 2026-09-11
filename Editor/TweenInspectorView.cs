using System;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Tweens.Editor
{
    internal sealed class TweenInspectorView : IDisposable
    {
        private readonly TweenSettingsForm settings;
        private readonly TweenPreviewElement preview;
        private readonly IVisualElementScheduledItem refresh;
        private readonly Action refreshSource;
        public VisualElement Root { get; }

        private TweenInspectorView(VisualElement root, TweenSettingsForm settings, TweenPreviewElement preview, Action refreshSource = null)
        {
            Root = root; this.settings = settings; this.preview = preview; this.refreshSource = refreshSource;
            var section = new DeucarianEditorWorkspaceForm(root).Section("Preview");
            section.Note(() => "Resolved object settings");
            section.Root.Add(preview);
            refresh = root.schedule.Execute(Refresh).Every(250);
            root.RegisterCallback<AttachToPanelEvent>(_ => { Undo.undoRedoPerformed -= OnUndo; Undo.undoRedoPerformed += OnUndo; });
            root.RegisterCallback<DetachFromPanelEvent>(_ => { Undo.undoRedoPerformed -= OnUndo; preview.Stop(); });
            Refresh();
        }

        public static TweenInspectorView ForProfile(SerializedObject source, TweenVisibilityProfile value)
        {
            var root = DeucarianEditorInspector.CreateToolkit("Visibility profile");
            var preview = new TweenPreviewElement(() => value.enter, () => value.exit, reducedMotion: () => value.reducedMotion,
                unscaledTime: () => value.unscaledTime);
            var fields = new VisualElement(); root.Add(fields);
            var settings = new TweenSettingsForm(fields, source, preview.RefreshSettings);
            settings.Direction("enter", false); settings.Direction("exit", false); settings.ProfileOptions();
            return new TweenInspectorView(root, settings, preview);
        }

        public static TweenInspectorView ForObject(SerializedObject source, TweenedVisibility value)
        {
            var root = DeucarianEditorInspector.CreateToolkit("Tweened visibility");
            var preview = new TweenPreviewElement(() => Resolve(value, true), () => Resolve(value, false),
                reducedMotion: () => TweenVisibilityDefaults.ReducedMotion || (Profile(value)?.reducedMotion ?? false),
                unscaledTime: () => Profile(value)?.unscaledTime ?? true);
            var fields = new VisualElement(); root.Add(fields);
            var settings = new TweenSettingsForm(fields, source, preview.RefreshSettings);
            settings.Asset(settings.Form, "visualRoot", "Visual root", typeof(Transform), true);
            settings.Asset(settings.Form, "profile", "Profile", typeof(TweenVisibilityProfile));
            var sourceRow = DeucarianEditorFeatureSection.Information(Source(value));
            sourceRow.name = "tween-source"; fields.Add(sourceRow);
            var open = DeucarianEditorWorkspaceControls.IconButton("Open profile", DeucarianEditorIconIds.ExternalLink,
                () => { if (Profile(value) != null) TweenPreviewWindow.Open(Profile(value)); });
            sourceRow.Add(open);
            settings.Direction("enter", true, () => Resolve(value, true));
            settings.Direction("exit", true, () => Resolve(value, false));
            fields.Add(DeucarianEditorWorkspaceControls.Divider());
            settings.Bool(settings.Form, "showOnEnable", "Show on enable");
            var advanced = settings.Form.Section("Advanced", true);
            settings.Text(advanced, "stableId", "Stable ID");
            advanced.Note(() => "Use Show / Hide for animated visibility. Direct disable or destroy cancels immediately.");
            advanced.Note(() => "Fade requires a CanvasGroup or a consumer renderer adapter.");
            return new TweenInspectorView(root, settings, preview, () => {
                sourceRow.Q<Label>().text = Source(value); open.SetEnabled(Profile(value) != null);
            });
        }

        private static TweenVisibilityProfile Profile(TweenedVisibility value)
        {
            TweenVisibilityBindings.Resolve(value.gameObject, value.stableId, value, out _, out _, out var profile);
            return profile;
        }

        private static VisibilityTweenSettings Resolve(TweenedVisibility value, bool entering)
        {
            TweenVisibilityBindings.Resolve(value.gameObject, value.stableId, value, out var enter, out var exit, out _);
            return entering ? enter : exit;
        }

        private static string Source(TweenedVisibility value)
        {
            var profile = Profile(value);
            var scope = value.GetComponentInParent<TweenVisibilityScope>(true);
            string result = profile == null ? "Built-in defaults" :
                (value.profile != null ? "Object · " : scope != null && scope.profile != null ? "Scope · " : "Project · ") + profile.name;
            if (scope != null && scope.stableOverrides != null && scope.stableOverrides.TryGet(value.stableId, out _))
                result += " · stable ID: " + value.stableId;
            return result;
        }

        private void Refresh() { settings.Refresh(); refreshSource?.Invoke(); }
        private void OnUndo() { Refresh(); preview.RefreshSettings(); }
        public void Dispose() { Undo.undoRedoPerformed -= OnUndo; refresh.Pause(); preview.Dispose(); }
    }
}
