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

        public VisibilityProgress(bool clampProgress = true) { clamp = clampProgress; Reset(false); }
        public event Action<VisibilityProgress> Completed;
        public float Progress { get; private set; }
        public VisibilityPhase Phase { get; private set; }
        public bool IsAnimating => Phase == VisibilityPhase.Entering || Phase == VisibilityPhase.Exiting;
        public float RemainingSeconds => IsAnimating ? Mathf.Max(0, duration - elapsed) : 0;

        public float MoveTo(bool visible, float seconds, DeucarianEasing curve)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            start = Progress;
            target = visible ? 1 : 0;
            elapsed = 0;
            easing = curve;
            float distance = Mathf.Abs(target - start);
            duration = Mathf.Max(0, seconds) * distance;
            if (distance <= Epsilon) { Settle(target, false); return 0; }
            Phase = visible ? VisibilityPhase.Entering : VisibilityPhase.Exiting;
            if (duration <= Epsilon) { Settle(target, true); return 0; }
            return duration;
        }

        public bool Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsAnimating) return false;
            elapsed += Mathf.Max(0, seconds);
            float t = Mathf.Clamp01(elapsed / duration);
            Progress = Mathf.LerpUnclamped(start, target, DeucarianEasingUtility.Evaluate(easing, t));
            if (clamp) Progress = Mathf.Clamp01(Progress);
            if (t >= 1) Settle(target, true);
            return true;
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
