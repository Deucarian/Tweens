using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Tweens.Editor
{
    /// <summary>One serialized form for profile and per-direction override authoring.</summary>
    internal sealed class TweenSettingsForm
    {
        private readonly SerializedObject source;
        private readonly Action changed;
        private readonly List<Action> visibility = new List<Action>();
        public DeucarianEditorWorkspaceForm Form { get; }

        public TweenSettingsForm(VisualElement root, SerializedObject source, Action changed = null)
        {
            this.source = source;
            this.changed = changed;
            Form = new DeucarianEditorWorkspaceForm(root);
            Form.EnabledWhen(() => CanEdit);
        }

        public void Direction(string path, bool overrides, Func<VisibilityTweenSettings> resolved = null)
        {
            var form = Form.Section(ObjectNames.NicifyVariableName(path));
            Choice(form, path + (overrides ? ".animation" : ".style"), "Animation");
            if (overrides) Checkbox(form, path + ".overrideTiming", "Override timing");
            var timingRoot = new VisualElement(); form.Root.Add(timingRoot);
            var timing = new DeucarianEditorWorkspaceForm(timingRoot);
            visibility.Add(timing.Refresh);
            timing.NumberWithSlider(path + ".seconds", "Duration (s)", 0, 1,
                () => Property(path + ".seconds").floatValue,
                value => Write(path + ".seconds", p => p.floatValue = Nonnegative(value)));
            Choice(timing, path + ".easing", "Easing");
            if (!overrides) return;
            visibility.Add(() => DeucarianEditorWorkspaceControls.Show(timingRoot, Property(path + ".overrideTiming").boolValue));
            if (resolved != null)
            {
                var animation = form.ReadOnly(path + "-resolved-animation", "Resolved animation", () => ObjectNames.NicifyVariableName(resolved().style.ToString()));
                var duration = form.ReadOnly(path + "-resolved", "Resolved timing", () => DescribeTiming(resolved()));
                form.VisibleWhen(animation, () => Property(path + ".animation").enumValueIndex == 0);
                form.VisibleWhen(duration, () => !Property(path + ".overrideTiming").boolValue);
            }
            var shape = form.Section("Shape override", true);
            Checkbox(shape, path + ".overrideShape", "Override shape");
            var values = new VisualElement(); shape.Root.Add(values);
            var fields = new DeucarianEditorWorkspaceForm(values);
            Shape(fields, path);
            visibility.Add(() => { values.SetEnabled(Property(path + ".overrideShape").boolValue); fields.Refresh(); });
        }

        private void Shape(DeucarianEditorWorkspaceForm shape, string path)
        {
            Number(shape, path + ".hiddenScale", "Hidden scale");
            shape.Vector(path + ".hiddenOffset", "Slide offset", () => Property(path + ".hiddenOffset").vector3Value,
                value => Write(path + ".hiddenOffset", p => p.vector3Value = value));
        }

        public void ProfileOptions()
        {
            Bool(Form, "unscaledTime", "Unscaled time");
            Bool(Form, "reducedMotion", "Reduced motion");
            var shape = Form.Section("Shape settings", true);
            Shape(shape.Section("Enter"), "enter"); Shape(shape.Section("Exit"), "exit");
        }

        private void Checkbox(DeucarianEditorWorkspaceForm form, string path, string label)
        {
            var field = new Toggle { name = path };
            field.AddToClassList("dw-checkbox");
            form.Root.Add(DeucarianEditorWorkspaceControls.Field(label, field));
            field.RegisterValueChangedCallback(evt => Write(path, p => p.boolValue = evt.newValue));
            visibility.Add(() => field.SetValueWithoutNotify(Property(path).boolValue));
        }

        public void Bool(DeucarianEditorWorkspaceForm form, string path, string label)
            => form.Toggle(path, label, () => Property(path).boolValue,
                value => Write(path, p => p.boolValue = value));

        public void Text(DeucarianEditorWorkspaceForm form, string path, string label)
            => form.Text(path, label, () => Property(path).stringValue,
                value => Write(path, p => p.stringValue = value));

        public void Asset(DeucarianEditorWorkspaceForm form, string path, string label, Type type, bool scene = false)
        {
            var field = form.Asset(path, label, type, () => Property(path).objectReferenceValue,
                value => Write(path, p => p.objectReferenceValue = value));
            field.allowSceneObjects = scene;
        }

        public void Refresh()
        {
            if (source.targetObject == null) return;
            source.UpdateIfRequiredOrScript();
            Form.Refresh();
            foreach (var update in visibility) update();
        }

        private void Choice(DeucarianEditorWorkspaceForm form, string path, string label)
            => form.Choice(path, label, Property(path).enumDisplayNames,
                () => Property(path).enumValueIndex, value => Write(path, p => p.enumValueIndex = value));

        private void Number(DeucarianEditorWorkspaceForm form, string path, string label)
            => form.Number(path, label, () => Property(path).floatValue,
                value => Write(path, p => p.floatValue = Nonnegative(value)));

        private static float Nonnegative(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Max(0, value);

        private SerializedProperty Property(string path) => source.FindProperty(path);

        private bool CanEdit
        {
            get
            {
                if (source.targetObject == null) return false;
                string path = AssetDatabase.GetAssetPath(source.targetObject).Replace('\\', '/');
                return string.IsNullOrEmpty(path) || path.StartsWith("Assets/", StringComparison.Ordinal) && AssetDatabase.IsOpenForEdit(path);
            }
        }

        private void Write(string path, Action<SerializedProperty> write)
        {
            if (!CanEdit) return;
            source.Update();
            write(Property(path));
            source.ApplyModifiedProperties();
            Refresh();
            changed?.Invoke();
        }

        private static string DescribeTiming(VisibilityTweenSettings value)
            => value.seconds.ToString("0.##") +
               " s · " + ObjectNames.NicifyVariableName(value.easing.ToString());
    }
}
