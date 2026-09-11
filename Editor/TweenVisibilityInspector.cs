using Deucarian.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Deucarian.Tweens.Editor
{
    [CustomEditor(typeof(TweenedVisibility))]
    public sealed class TweenVisibilityInspector : UnityEditor.Editor
    {
        private TweenInspectorView view;
        public override VisualElement CreateInspectorGUI()
        {
            view?.Dispose();
            view = TweenInspectorView.ForObject(serializedObject, (TweenedVisibility)target);
            return view.Root;
        }
        private void OnDisable() => view?.Dispose();
    }

    [CustomEditor(typeof(TweenVisibilityProfile))]
    public sealed class TweenVisibilityProfileInspector : UnityEditor.Editor
    {
        private TweenInspectorView view;
        public override VisualElement CreateInspectorGUI()
        {
            view?.Dispose();
            view = TweenInspectorView.ForProfile(serializedObject, (TweenVisibilityProfile)target);
            return view.Root;
        }
        private void OnDisable() => view?.Dispose();
    }
}
