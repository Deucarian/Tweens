using UnityEngine;

namespace Deucarian.Tweens
{
    /// <summary>Per-object authoring and UnityEvent commands. No per-object Update method.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Deucarian/Tweens/Tweened Visibility")]
    public sealed class TweenedVisibility : MonoBehaviour
    {
        [Tooltip("Prefer a visual child so animation does not change logical poses or physics.")]
        public Transform visualRoot;
        public TweenVisibilityProfile profile;
        public VisibilityTweenOverride enter;
        public VisibilityTweenOverride exit;
        public bool showOnEnable = true;
        [Tooltip("Optional stable identifier for the closest scope's per-element overrides.")]
        public string stableId;
        private TransformVisibilityBinding binding;
        public bool IsAnimating => binding != null && binding.IsAnimating;

        public void Show() => GetBinding().SetVisible(true);
        public void Hide() => GetBinding().SetVisible(false);
        public void HideAndDestroy() => GetBinding().HideAndDestroy();
        public void ShowImmediate() => GetBinding().RestoreImmediate(true);
        public void HideImmediate() => GetBinding().RestoreImmediate(false);

        public TransformVisibilityBinding GetBinding(string elementId = null)
        {
            bool identityChanged = elementId != null && stableId != elementId;
            if (elementId != null) stableId = elementId;
            if (binding == null || binding.IsDisposed)
                binding = TweenVisibilityBindings.Create(TweenRuntime.Scheduler, gameObject, stableId, this);
            else if (identityChanged)
                TweenVisibilityBindings.Configure(binding, gameObject, stableId, this);
            return binding;
        }

        private void OnEnable()
        {
            if (Application.isPlaying && showOnEnable && !(binding?.IsApplying ?? false))
                GetBinding().ShowFromHidden();
        }
        private void OnDisable() => binding?.ExternalDisable();
        private void OnDestroy() { binding?.Dispose(); binding = null; }
    }
}
