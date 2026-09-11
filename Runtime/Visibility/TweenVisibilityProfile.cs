using UnityEngine;

namespace Deucarian.Tweens
{
    [CreateAssetMenu(menuName = "Deucarian/Tweens/Visibility Profile", fileName = "Visibility Profile")]
    public sealed class TweenVisibilityProfile : ScriptableObject
    {
        public VisibilityTweenSettings enter = VisibilityTweenSettings.Enter;
        public VisibilityTweenSettings exit = VisibilityTweenSettings.Exit;
        public bool unscaledTime = true;
        public bool reducedMotion;
    }

    public static class TweenVisibilityDefaults
    {
        public const string ResourceName = "DeucarianTweenDefaults";
        private static bool loaded;
        private static TweenVisibilityProfile profile;
        public static bool ReducedMotion { get; set; }
        public static TweenVisibilityProfile Profile
        {
            get
            {
                if (!loaded) { profile = Resources.Load<TweenVisibilityProfile>(ResourceName); loaded = true; }
                return profile;
            }
        }
        public static void Configure(TweenVisibilityProfile value) { profile = value; loaded = true; }
        internal static void Reset() { loaded = false; profile = null; ReducedMotion = false; }
    }
}
