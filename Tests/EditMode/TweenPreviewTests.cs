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
        [Test] public void StopAlsoClearsLoopControl()
        {
            using var preview = new TweenPreviewElement(() => VisibilityTweenSettings.Enter, () => VisibilityTweenSettings.Exit);
            preview.Q<Toggle>().value = true;
            preview.Play(false); preview.Stop();
            Assert.That(preview.Q<Toggle>().value, Is.False);
            Assert.That(preview.IsAnimating, Is.False);
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
