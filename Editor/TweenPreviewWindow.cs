using System;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Tweens.Editor
{
    public sealed class TweenPreviewWindow : EditorWindow
    {
        public const string ToolId = "deucarian.tweens.preview";
        [SerializeField] private TweenVisibilityProfile profile;
        [SerializeField] private bool useSnapshot;
        [SerializeField] private VisibilityTweenSettings snapshotEnter;
        [SerializeField] private VisibilityTweenSettings snapshotExit;
        [SerializeField] private float unitsToPixels = 100;
        private DeucarianEditorPageSession navigation;
        private DeucarianEditorWorkspace workspace;
        private TweenPreviewElement preview;
        private TweenSettingsForm settings;
        private SerializedObject serializedProfile;
        private VisualElement formHost;

        public static void Open() => DeucarianEditorNavigation.Open(null, ToolId);
        public static void Open(VisibilityTweenSettings enter, VisibilityTweenSettings exit, float unitsToPixels = 100)
        {
            var window = DeucarianEditorWindowPages.GetStandalone<TweenPreviewWindow>("Tweens");
            window.profile = null; window.useSnapshot = true;
            window.snapshotEnter = enter; window.snapshotExit = exit;
            window.unitsToPixels = unitsToPixels;
            window.CreateGUI(); window.Show();
        }

        public static void Open(TweenVisibilityProfile selected)
            => DeucarianEditorNavigation.Open(null, ToolId, selected == null ? null :
                "profile:" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selected)));

        public void CreateGUI()
        {
            navigation?.Dispose();
            navigation = new DeucarianEditorPageSession(this, ToolId, BuildPage, Activate, deactivateHome: Stop);
        }

        internal static IDeucarianEditorPage CreatePage()
            => DeucarianEditorWindowPages.Create<TweenPreviewWindow>((window, root) => window.BuildPage(root),
                activate: (window, route) => window.Activate(route), deactivate: window => window.Stop(),
                update: window => window.settings?.Refresh());

        private void BuildPage(VisualElement root)
        {
            Release();
            workspace = new DeucarianEditorWorkspace(root, Application.productName);
            workspace.Title.text = "Tweens";
            workspace.Subtitle.text = "Shape how objects appear and disappear.";
            DeucarianEditorWorkspaceNavigation.Populate(workspace, ToolId);
            DeucarianEditorWorkspaceControls.Show(workspace.Tabs, false);
            DeucarianEditorWorkspaceControls.Show(workspace.Footer, false);
            var scope = new DeucarianEditorWorkspaceForm(workspace.Scope);
            scope.Asset("tween-profile", "Profile", typeof(TweenVisibilityProfile), () => profile,
                value => SelectProfile((TweenVisibilityProfile)value));
            workspace.Scope.Add(DeucarianEditorWorkspaceControls.Button("Create profile", CreateProfile));
            workspace.Scope.AddToClassList("dw-profile-scope");
            formHost = DeucarianEditorWorkspaceControls.Scroll("tween-settings");
            var previewHost = DeucarianEditorWorkspaceControls.Scroll("tween-preview-pane");
            previewHost.Add(DeucarianEditorWorkspaceControls.Label("Live preview", "dw-section-title"));
            preview = new TweenPreviewElement(() => profile != null ? profile.enter : useSnapshot ? snapshotEnter : VisibilityTweenSettings.Enter,
                () => profile != null ? profile.exit : useSnapshot ? snapshotExit : VisibilityTweenSettings.Exit, unitsToPixels,
                () => profile != null && profile.reducedMotion, () => profile == null || profile.unscaledTime);
            previewHost.Add(preview);
            var split = DeucarianEditorWorkspaceControls.Split(formHost, previewHost);
            split.AddToClassList("dw-balanced-preview");
            workspace.Content.Add(split);
            RebuildSettings();
            root.RegisterCallback<AttachToPanelEvent>(_ => { Undo.undoRedoPerformed -= Refresh; Undo.undoRedoPerformed += Refresh; });
            root.RegisterCallback<DetachFromPanelEvent>(_ => { Undo.undoRedoPerformed -= Refresh; Stop(); });
        }

        private void Activate(string route)
        {
            if (route != null && route.StartsWith("profile:", StringComparison.Ordinal))
                SelectProfile(AssetDatabase.LoadAssetAtPath<TweenVisibilityProfile>(AssetDatabase.GUIDToAssetPath(route.Substring(8))));
            Refresh();
        }

        private void SelectProfile(TweenVisibilityProfile value)
        {
            profile = value; useSnapshot = false;
            workspace.Scope.Q<UnityEditor.UIElements.ObjectField>()?.SetValueWithoutNotify(profile);
            RebuildSettings();
            preview.RefreshSettings();
        }

        private void RebuildSettings()
        {
            settings = null; serializedProfile?.Dispose(); serializedProfile = null;
            formHost.Clear();
            if (profile == null)
            {
                formHost.Add(DeucarianEditorWorkspaceControls.Label(useSnapshot ? "Resolved animation" : "Built-in defaults", "dw-section-title"));
                formHost.Add(DeucarianEditorWorkspaceControls.Label("Choose or create a profile to edit. The preview is ready to try.", "dw-note"));
            }
            else
            {
                serializedProfile = new SerializedObject(profile);
                settings = new TweenSettingsForm(formHost, serializedProfile, preview.RefreshSettings);
                settings.Direction("enter", false); settings.Direction("exit", false);
                settings.ProfileOptions(); settings.Refresh();
            }
            workspace.FooterLeading.text = profile == null ? "Preview only" : "Editing " + profile.name;
            workspace.FooterTrailing.text = "No scene changes";
        }

        private void CreateProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create visibility profile", "Visibility Profile", "asset", "Choose where to save the profile.");
            if (string.IsNullOrEmpty(path)) return;
            var asset = CreateInstance<TweenVisibilityProfile>();
            AssetDatabase.CreateAsset(asset, path);
            SelectProfile(asset);
        }

        private void Refresh() { settings?.Refresh(); preview?.RefreshSettings(); }
        private void Stop() => preview?.Stop();
        private void Release() { preview?.Dispose(); workspace?.Dispose(); serializedProfile?.Dispose(); serializedProfile = null; settings = null; }
        private void OnDisable() { Undo.undoRedoPerformed -= Refresh; navigation?.Dispose(); navigation = null; Release(); }
    }

    [InitializeOnLoad]
    internal static class TweenToolRegistration
    {
        private static readonly IDisposable Registration;
        static TweenToolRegistration()
        {
            Registration = DeucarianToolRegistry.Register(new DeucarianToolDescriptor(
                TweenPreviewWindow.ToolId, "Tweens", "Edit and preview visibility profiles.",
                DeucarianControlCenterArea.Authoring, TweenPreviewWindow.Open, "com.deucarian.tweens",
                DeucarianEditorIconIds.Sample, new[] { "animation", "tween", "visibility", "preview" }, 220,
                createPage: TweenPreviewWindow.CreatePage));
        }
    }
}
