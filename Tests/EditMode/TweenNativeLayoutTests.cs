using System.Collections;
using Deucarian.Editor;
using Deucarian.Tweens.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.Tweens.Tests
{
    public sealed class TweenNativeLayoutTests
    {
        private sealed class Host : EditorWindow { }

        [UnityTest]
        public IEnumerator DirectionOverridesUseIndependentResolvedReadoutsAndDurationSliders()
        {
            var target = new GameObject("Tween Inspector fixture");
            var value = target.AddComponent<TweenedVisibility>();
            var editor = UnityEditor.Editor.CreateEditor(value);
            var window = ScriptableObject.CreateInstance<Host>();
            try
            {
                window.position = new Rect(80, 80, 460, 900); window.Show();
                var root = editor.CreateInspectorGUI(); window.rootVisualElement.Add(root);
                for (int i = 0; i < 16; i++) yield return null;
                Assert.That(root.Q<Slider>("enter.seconds-slider"), Is.Not.Null);
                Assert.That(root.Q<Slider>("exit.seconds-slider"), Is.Not.Null);
                Assert.That(root.Q<Label>("exit-resolved").parent.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                root.Q<Toggle>("enter.overrideTiming").value = true;
                root.Q<FloatField>("enter.seconds").value = .28f;
                Assert.That(value.enter.seconds, Is.EqualTo(.28f));
                Assert.That(value.exit.overrideTiming, Is.False);
                Assert.That(root.Q<Label>("enter-resolved").parent.style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(root.Q<Label>("exit-resolved").parent.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                var preview = root.Q<TweenPreviewElement>();
                preview.Scrub(.65f);
                Assert.That(preview.Q<Label>(className: "dw-value-badge").text, Does.Contain("65"));
                Assert.That(root.Q("workspace-navigation"), Is.Null);
                Assert.That(root.Q("workspace-scale-slider"), Is.Null);
                for (int i = 0; i < 8; i++) yield return null;
                var baseline = preview.Q("tween-preview-baseline");
                var stage = preview.Q("tween-preview-stage");
                Assert.That(baseline, Is.Not.Null);
                Assert.That(baseline.pickingMode, Is.EqualTo(PickingMode.Ignore));
                Assert.That(baseline.worldBound.center.x, Is.EqualTo(stage.worldBound.center.x).Within(2));
                Assert.That(baseline.worldBound.center.y, Is.EqualTo(stage.worldBound.center.y).Within(2));
                Assert.That(baseline.worldBound.width, Is.EqualTo(150).Within(2));
                var rest = baseline.worldBound;
                preview.Scrub(.2f);
                for (int i = 0; i < 3; i++) yield return null;
                Assert.That(baseline.worldBound, Is.EqualTo(rest), "The reference outline must not animate with the specimen.");
                foreach (var field in root.Query<FloatField>().ToList())
                {
                    if (field.resolvedStyle.display == DisplayStyle.None || field.worldBound.width < 1) continue;
                    Assert.That(field.worldBound.xMax, Is.LessThanOrEqualTo(root.worldBound.xMax + 1), field.name);
                }
            }
            finally { window.Close(); Object.DestroyImmediate(editor); Undo.ClearUndo(value); Object.DestroyImmediate(target); }
        }
    }
}
