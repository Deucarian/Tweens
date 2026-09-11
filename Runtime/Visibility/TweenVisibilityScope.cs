using UnityEngine;

namespace Deucarian.Tweens
{
    /// <summary>Authored defaults for a subtree. Resolved at binding time, never scanned per frame.</summary>
    [DisallowMultipleComponent]
    public sealed class TweenVisibilityScope : MonoBehaviour
    {
        public TweenVisibilityProfile profile;
        public TweenVisibilityOverrides stableOverrides;
    }

}
