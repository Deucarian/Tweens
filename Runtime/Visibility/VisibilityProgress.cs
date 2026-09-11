using System;
using Deucarian.Common;
using UnityEngine;

namespace Deucarian.Tweens
{
    public enum VisibilityPhase { Hidden, Entering, Visible, Exiting }

    /// <summary>Renderer-independent reversible progress, extracted from Deucarian UI.</summary>
    public sealed class VisibilityProgress
    {
        private const float Epsilon = 0.0001f;
        private readonly bool clamp;
        private float start;
        private float target;
        private float elapsed;
        private float duration;
        private DeucarianEasing easing;
        private AnimationCurve customCurve;

        public VisibilityProgress(bool clampProgress = true) { clamp = clampProgress; Reset(false); }
        public event Action<VisibilityProgress> Completed;
        public float Progress { get; private set; }
        public VisibilityPhase Phase { get; private set; }
        public bool IsAnimating => Phase == VisibilityPhase.Entering || Phase == VisibilityPhase.Exiting;
        public float RemainingSeconds => IsAnimating ? Mathf.Max(0, duration - elapsed) : 0;

        public float MoveTo(bool visible, float seconds, DeucarianEasing curve)
            => MoveTo(visible, seconds, curve, null);

        public float MoveTo(bool visible, float seconds, DeucarianEasing curve, AnimationCurve custom)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            start = Progress;
            target = visible ? 1 : 0;
            elapsed = 0;
            easing = curve;
            customCurve = custom;
            float distance = Mathf.Abs(target - start);
            duration = Mathf.Max(0, seconds) * distance;
            if (distance <= Epsilon) { Settle(target, false); return 0; }
            Phase = visible ? VisibilityPhase.Entering : VisibilityPhase.Exiting;
            if (duration <= Epsilon) { Settle(target, true); return 0; }
            return duration;
        }

        internal void RefreshSettings(float seconds, DeucarianEasing curve, AnimationCurve custom)
        {
            if (!IsAnimating) return;
            float normalizedTime = duration <= Epsilon ? 1 : Mathf.Clamp01(elapsed / duration);
            duration = Mathf.Max(0, seconds) * Mathf.Abs(target - start);
            elapsed = normalizedTime * duration;
            easing = curve;
            customCurve = custom;
            if (duration <= Epsilon) Settle(target, true);
            else Sample(normalizedTime);
        }

        public bool Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsAnimating) return false;
            elapsed += Mathf.Max(0, seconds);
            float t = Mathf.Clamp01(elapsed / duration);
            Sample(t);
            if (t >= 1) Settle(target, true);
            return true;
        }

        private void Sample(float t)
        {
            float eased = DeucarianEasingUtility.Evaluate(easing, t);
            if (customCurve != null && customCurve.length >= 2 && t > 0 && t < 1)
            {
                float custom = customCurve.Evaluate(t);
                if (!float.IsNaN(custom) && !float.IsInfinity(custom)) eased = custom;
            }
            Progress = Mathf.LerpUnclamped(start, target, eased);
            if (clamp) Progress = Mathf.Clamp01(Progress);
        }

        public void Complete() { if (IsAnimating) Settle(target, true); }
        public void Reset(bool visible) => Settle(visible ? 1 : 0, false);
        public void SetProgress(float progress)
        {
            if (float.IsNaN(progress) || float.IsInfinity(progress))
                throw new ArgumentOutOfRangeException(nameof(progress));
            Settle(Mathf.Clamp01(progress), false);
        }
        private void Settle(float value, bool notify)
        {
            Progress = start = target = value;
            elapsed = duration = 0;
            Phase = value <= Epsilon ? VisibilityPhase.Hidden : VisibilityPhase.Visible;
            if (notify) Completed?.Invoke(this);
        }
    }
}
