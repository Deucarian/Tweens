using Deucarian.Tweens.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Tweens.Tests
{
    public sealed class TweenPreviewTests
    {
        [Test] public void ScrubUsesSelectedExitSettingsAndReducedMotionSettles()
        {
            var enter = VisibilityTweenSettings.Enter;
            var exit = VisibilityTweenSettings.Exit;
            enter.hiddenScale = 0.1f; exit.hiddenScale = 0.6f;
            using var preview = new TweenPreviewElement(() => enter, () => exit, reducedMotion: () => true);
            preview.Play(false);
            Assert.That(preview.IsAnimating, Is.False);
            preview.Scrub(0);
            Assert.That(preview.Q<VisualElement>("tween-preview-object").style.scale.value.value.x, Is.EqualTo(.6f).Within(.001));
            preview.Play(true); preview.Scrub(0);
            Assert.That(preview.Q<VisualElement>("tween-preview-object").style.scale.value.value.x, Is.EqualTo(.1f).Within(.001));
        }

        [UnityEngine.TestTools.UnityTest] public System.Collections.IEnumerator InspectorPreservesInactiveOverridesAndSupportsUndo()
        {
            var go = new GameObject("Inspector test");
            var value = go.AddComponent<TweenedVisibility>();
            value.enter.seconds = .73f;
            var editor = UnityEditor.Editor.CreateEditor(value);
            var window = ScriptableObject.CreateInstance<PreviewTestWindow>();
            try
            {
                var root = editor.CreateInspectorGUI();
                window.Show();
                window.rootVisualElement.Add(root);
                yield return null;
                Assert.That(root.Q("workspace-navigation"), Is.Null);
                Assert.That(root.Q("workspace-scale-slider"), Is.Null);
                var toggle = root.Q<Toggle>("enter.overrideTiming");
                Assert.That(toggle, Is.Not.Null);
                toggle.value = true;
                root.Q<FloatField>("enter.seconds").value = .45f;
                Assert.That(value.enter.seconds, Is.EqualTo(.45f));
                Assert.That(value.exit.overrideTiming, Is.False);
                Undo.IncrementCurrentGroup();
                toggle.value = false;
                Undo.FlushUndoRecordObjects();
                Assert.That(value.enter.seconds, Is.EqualTo(.45f), "Disabling an override must not erase its authored value.");
                Undo.PerformUndo();
                Assert.That(value.enter.overrideTiming, Is.True);
            }
            finally { window.Close(); Object.DestroyImmediate(editor); Object.DestroyImmediate(go); }
        }

        [UnityEngine.TestTools.UnityTest] public System.Collections.IEnumerator CachedPreviewStopsOnDetachAndWorksOnReturn()
        {
            var window = ScriptableObject.CreateInstance<PreviewTestWindow>();
            using var preview = new TweenPreviewElement(() => VisibilityTweenSettings.Enter, () => VisibilityTweenSettings.Exit);
            try
            {
                window.Show(); window.rootVisualElement.Add(preview);
                yield return null;
                preview.Q<Toggle>().value = true;
                preview.Play(true);
                preview.RemoveFromHierarchy();
                Assert.That(preview.IsAnimating, Is.False);
                Assert.That(preview.Q<Toggle>().value, Is.False);
                window.rootVisualElement.Add(preview);
                yield return null;
                preview.Play(false);
                Assert.That(preview.IsAlive, Is.True);
                Assert.That(preview.IsAnimating, Is.True);
            }
            finally { window.Close(); }
        }

        public sealed class PreviewTestWindow : EditorWindow { }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator DestroyingInspectedTargetStopsAnAttachedLoop()
        {
            var target = new GameObject("Inspector lifetime test");
            var editor = UnityEditor.Editor.CreateEditor(target.AddComponent<TweenedVisibility>());
            var window = ScriptableObject.CreateInstance<PreviewTestWindow>();
            try
            {
                var root = editor.CreateInspectorGUI();
                window.Show(); window.rootVisualElement.Add(root);
                yield return null;
                var preview = root.Q<TweenPreviewElement>();
                preview.Q<Toggle>().value = true;
                preview.Play(true);
                Object.DestroyImmediate(target);
                double deadline = EditorApplication.timeSinceStartup + 2;
                while (preview.IsAlive && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(preview.IsAlive, Is.False);
                Assert.That(preview.IsAnimating, Is.False);
                UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
            }
            finally { window.Close(); Object.DestroyImmediate(editor); if (target != null) Object.DestroyImmediate(target); }
        }

        [Test] public void TweensContributesAnInWindowPage()
        {
            Assert.That(Deucarian.Editor.DeucarianToolRegistry.TryGet(TweenPreviewWindow.ToolId, out var tool), Is.True);
            Assert.That(tool.CreatePage, Is.Not.Null);
            using var page = tool.CreatePage();
            Assert.That(page.Root.Q("tween-profile"), Is.Not.Null);
            Assert.That(page.Root.Q("tween-preview"), Is.Not.Null);
            page.Deactivate(); page.Activate(null);
            page.Root.Q<TweenPreviewElement>().Play(true);
        }
        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator StopAlsoClearsLoopControl()
        {
            using var preview = new TweenPreviewElement(() => VisibilityTweenSettings.Enter, () => VisibilityTweenSettings.Exit);
            var window = ScriptableObject.CreateInstance<PreviewTestWindow>();
            try
            {
                window.Show(); window.rootVisualElement.Add(preview);
                yield return null;
                preview.Q<Toggle>().value = true;
                Assert.That(preview.IsLooping, Is.True);
                preview.Play(false); preview.Stop();
                Assert.That(preview.Q<Toggle>().value, Is.False);
                Assert.That(preview.IsAnimating, Is.False);
            }
            finally { window.Close(); }
        }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator EditingLiveSettingsKeepsLoopAndPlaybackRunning()
        {
            var settings = VisibilityTweenSettings.Enter;
            settings.seconds = 1;
            using var preview = new TweenPreviewElement(() => settings, () => settings);
            var window = ScriptableObject.CreateInstance<PreviewTestWindow>();
            try
            {
                window.Show(); window.rootVisualElement.Add(preview);
                yield return null;
                preview.Q<Toggle>().value = true;
                preview.Play(true);
                settings.hiddenScale = .35f;
                settings.seconds = 2;
                preview.RefreshSettings();
                Assert.That(preview.IsLooping, Is.True);
                Assert.That(preview.Q<Toggle>().value, Is.True);
                Assert.That(preview.IsAnimating, Is.True);
                Assert.That(preview.Q<VisualElement>("tween-preview-object").style.scale.value.value.x,
                    Is.EqualTo(.35f).Within(.001f));
            }
            finally { window.Close(); }
        }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator SwitchingProfileInsideWorkspaceKeepsLoopRunning()
        {
            var profile = ScriptableObject.CreateInstance<TweenVisibilityProfile>();
            Assert.That(Deucarian.Editor.DeucarianToolRegistry.TryGet(TweenPreviewWindow.ToolId, out var tool), Is.True);
            using var page = tool.CreatePage();
            var window = ScriptableObject.CreateInstance<PreviewTestWindow>();
            try
            {
                window.Show(); window.rootVisualElement.Add(page.Root);
                yield return null;
                var preview = page.Root.Q<TweenPreviewElement>();
                preview.Q<Toggle>().value = true;
                preview.Play(true);
                page.Root.Q<UnityEditor.UIElements.ObjectField>("tween-profile").value = profile;
                Assert.That(preview.IsLooping, Is.True);
                Assert.That(preview.IsAnimating, Is.True);
            }
            finally { window.Close(); Object.DestroyImmediate(profile); }
        }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator ProfileInspectorChangesAndUndoRedoDoNotClearLoop()
        {
            var profile = ScriptableObject.CreateInstance<TweenVisibilityProfile>();
            var editor = UnityEditor.Editor.CreateEditor(profile);
            var window = ScriptableObject.CreateInstance<PreviewTestWindow>();
            try
            {
                var root = editor.CreateInspectorGUI();
                window.Show();
                window.rootVisualElement.Add(root);
                yield return null;
                var preview = root.Q<TweenPreviewElement>();
                preview.Q<Toggle>().value = true;
                preview.Play(true);
                Undo.IncrementCurrentGroup();
                root.Q<FloatField>("enter.seconds").value = .72f;
                Undo.FlushUndoRecordObjects();
                Assert.That(profile.enter.seconds, Is.EqualTo(.72f));
                Assert.That(preview.IsLooping, Is.True);
                Assert.That(preview.IsAnimating, Is.True);
                Undo.PerformUndo();
                Assert.That(preview.IsLooping, Is.True);
                Assert.That(preview.IsAnimating, Is.True);
                Undo.PerformRedo();
                Assert.That(preview.IsLooping, Is.True);
                Assert.That(preview.IsAnimating, Is.True);
            }
            finally { window.Close(); Object.DestroyImmediate(editor); Object.DestroyImmediate(profile); }
        }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator CustomCurveEditorCreatesAndPreservesAuthoredCurve()
        {
            var profile = ScriptableObject.CreateInstance<TweenVisibilityProfile>();
            var editor = UnityEditor.Editor.CreateEditor(profile);
            var window = ScriptableObject.CreateInstance<PreviewTestWindow>();
            try
            {
                var root = editor.CreateInspectorGUI();
                window.Show(); window.rootVisualElement.Add(root);
                yield return null;
                var toggle = root.Q<Toggle>("enter.useCustomCurve");
                var curve = root.Q<UnityEditor.UIElements.CurveField>("enter.customCurve");
                toggle.value = true;
                Assert.That(profile.enter.useCustomCurve, Is.True);
                Assert.That(profile.enter.customCurve.length, Is.EqualTo(2));
                curve.value = AnimationCurve.Linear(0, 0, 1, .5f);
                toggle.value = false;
                Assert.That(profile.enter.customCurve.Evaluate(.5f), Is.EqualTo(.25f).Within(.001f));
                toggle.value = true;
                Assert.That(profile.enter.customCurve.Evaluate(.5f), Is.EqualTo(.25f).Within(.001f));
            }
            finally { window.Close(); Object.DestroyImmediate(editor); Object.DestroyImmediate(profile); }
        }
        [Test] public void NoAnimationExitShowsHiddenInsteadOfStuckExiting()
        {
            var settings = VisibilityTweenSettings.Exit; settings.style = VisibilityTweenStyle.None;
            using var preview = new TweenPreviewElement(() => settings, () => settings);
            preview.Play(false);
            Assert.That(preview.IsAnimating, Is.False);
            Assert.That(preview.Q<VisualElement>("tween-preview-object").style.opacity.value, Is.Zero);
        }
    }
}
