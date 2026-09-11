using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Tweens
{
    [Serializable]
    public struct StableVisibilityOverride
    {
        public string stableId;
        public VisibilityTweenOverride enter;
        public VisibilityTweenOverride exit;
    }

    [CreateAssetMenu(menuName = "Deucarian/Tweens/Stable Visibility Overrides", fileName = "Visibility Overrides")]
    public sealed class TweenVisibilityOverrides : ScriptableObject
    {
        public StableVisibilityOverride[] entries = Array.Empty<StableVisibilityOverride>();
        private Dictionary<string, StableVisibilityOverride> index;
        public bool TryGet(string stableId, out StableVisibilityOverride value)
        {
            value = default;
            if (string.IsNullOrEmpty(stableId)) return false;
            if (index == null)
            {
                index = new Dictionary<string, StableVisibilityOverride>(StringComparer.Ordinal);
                foreach (StableVisibilityOverride entry in entries ?? Array.Empty<StableVisibilityOverride>())
                    if (!string.IsNullOrEmpty(entry.stableId)) index[entry.stableId] = entry;
            }
            return index.TryGetValue(stableId, out value);
        }
        public void Invalidate() { index = null; }
        private void OnValidate() => Invalidate();
    }
}
