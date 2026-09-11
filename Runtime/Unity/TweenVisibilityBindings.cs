using UnityEngine;

namespace Deucarian.Tweens
{
    public static class TweenVisibilityBindings
    {
        /// <summary>Resolve once: project defaults, closest subtree profile, stable ID, then per-object overrides.</summary>
        public static TransformVisibilityBinding Create(TweenScheduler scheduler, GameObject target,
            string stableId = null, TweenedVisibility authored = null)
        {
            if (target == null) throw new System.ArgumentNullException(nameof(target));
            if (authored == null) authored = target.GetComponent<TweenedVisibility>();
            var binding = new TransformVisibilityBinding(scheduler, target,
                authored != null ? authored.visualRoot : target.transform,
                VisibilityTweenSettings.Enter, VisibilityTweenSettings.Exit);
            Configure(binding, target, stableId, authored);
            return binding;
        }

        public static void Configure(TransformVisibilityBinding binding, GameObject target,
            string stableId = null, TweenedVisibility authored = null)
        {
            Resolve(target, stableId, authored, out var enter, out var exit, out var profile);
            binding.Enter = enter;
            binding.Exit = exit;
            binding.UnscaledTime = profile == null || profile.unscaledTime;
            binding.ReducedMotion = profile != null && profile.reducedMotion;
        }

        public static void Resolve(GameObject target, string stableId, TweenedVisibility authored,
            out VisibilityTweenSettings enter, out VisibilityTweenSettings exit, out TweenVisibilityProfile profile)
        {
            if (target == null) throw new System.ArgumentNullException(nameof(target));
            TweenVisibilityScope scope = target.GetComponentInParent<TweenVisibilityScope>(true);
            if (authored == null) authored = target.GetComponent<TweenedVisibility>();
            profile = authored != null && authored.profile != null ? authored.profile :
                scope != null && scope.profile != null ? scope.profile : TweenVisibilityDefaults.Profile;
            enter = profile != null ? profile.enter : VisibilityTweenSettings.Enter;
            exit = profile != null ? profile.exit : VisibilityTweenSettings.Exit;
            if (scope != null && scope.stableOverrides != null && scope.stableOverrides.TryGet(stableId, out var value))
            { enter = value.enter.Resolve(enter); exit = value.exit.Resolve(exit); }
            if (authored != null) { enter = authored.enter.Resolve(enter); exit = authored.exit.Resolve(exit); }
        }
    }
}
