using System;
using Deucarian.Common;
using UnityEngine;

namespace Deucarian.Tweens
{
    public enum VisibilityTweenStyle { None, Scale, Slide, Fade, ScaleAndFade }
    public enum VisibilityTweenSelection { Inherit, None, Scale, Slide, Fade, ScaleAndFade }

    [Serializable]
    public struct VisibilityTweenSettings
    {
        public VisibilityTweenStyle style;
        [Min(0)] public float seconds;
        public DeucarianEasing easing;
        public bool useCustomCurve;
        [Tooltip("Optional normalized easing curve. Playback borrows this curve and reads edits live; endpoints always settle exactly.")]
        public AnimationCurve customCurve;
        [Min(0)] public float hiddenScale;
        public Vector3 hiddenOffset;

        public static VisibilityTweenSettings Enter => new VisibilityTweenSettings {
            style = VisibilityTweenStyle.Scale, seconds = 0.22f,
            easing = DeucarianEasing.EaseOutSoftBack, hiddenScale = 0, hiddenOffset = Vector3.down * 0.25f };
        public static VisibilityTweenSettings Exit => new VisibilityTweenSettings {
            style = VisibilityTweenStyle.Scale, seconds = 0.16f,
            easing = DeucarianEasing.EaseInCubic, hiddenScale = 0, hiddenOffset = Vector3.down * 0.25f };

        public VisibilityTweenSettings Sanitized()
        {
            var value = this;
            value.seconds = FiniteNonnegative(seconds);
            value.hiddenScale = FiniteNonnegative(hiddenScale);
            if (float.IsNaN(hiddenOffset.sqrMagnitude) || float.IsInfinity(hiddenOffset.sqrMagnitude))
                value.hiddenOffset = Vector3.zero;
            return value;
        }
        private static float FiniteNonnegative(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Max(0, value);
    }

    [Serializable]
    public struct VisibilityTweenOverride
    {
        public VisibilityTweenSelection animation;
        public bool overrideTiming;
        [Min(0)] public float seconds;
        public DeucarianEasing easing;
        public bool useCustomCurve;
        [Tooltip("Optional normalized easing curve used when timing is overridden. Inactive values are preserved.")]
        public AnimationCurve customCurve;
        public bool overrideShape;
        [Min(0)] public float hiddenScale;
        public Vector3 hiddenOffset;

        public VisibilityTweenSettings Resolve(VisibilityTweenSettings inherited)
        {
            if (animation != VisibilityTweenSelection.Inherit)
                inherited.style = (VisibilityTweenStyle)((int)animation - 1);
            if (overrideTiming)
            {
                inherited.seconds = seconds;
                inherited.easing = easing;
                inherited.useCustomCurve = useCustomCurve;
                inherited.customCurve = customCurve;
            }
            if (overrideShape) { inherited.hiddenScale = hiddenScale; inherited.hiddenOffset = hiddenOffset; }
            return inherited.Sanitized();
        }
    }

    public readonly struct VisibilityTweenSample
    {
        public readonly float Scale;
        public readonly float Alpha;
        public readonly Vector3 Offset;
        public VisibilityTweenSample(float scale, float alpha, Vector3 offset)
        { Scale = scale; Alpha = alpha; Offset = offset; }
        public static VisibilityTweenSample Evaluate(VisibilityTweenSettings settings, float progress)
        {
            bool scale = settings.style == VisibilityTweenStyle.Scale || settings.style == VisibilityTweenStyle.ScaleAndFade;
            bool fade = settings.style == VisibilityTweenStyle.Fade || settings.style == VisibilityTweenStyle.ScaleAndFade;
            return new VisibilityTweenSample(
                scale ? Mathf.Max(0, Mathf.LerpUnclamped(settings.hiddenScale, 1, progress)) : 1,
                fade ? Mathf.Clamp01(progress) : 1,
                settings.style == VisibilityTweenStyle.Slide ? settings.hiddenOffset * (1 - progress) : Vector3.zero);
        }
    }
}
