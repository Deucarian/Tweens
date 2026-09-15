using UnityEngine;

namespace Deucarian.Tweens
{
    public enum VisibilityScaleOrigin { RendererBoundsCenter, TransformOrigin }

    /// <summary>Captures a scale anchor once, without changing imported meshes or their hierarchy.</summary>
    internal static class TransformVisibilityPivot
    {
        public static Vector3 Resolve(Transform visual, VisibilityScaleOrigin origin)
        {
            if (origin == VisibilityScaleOrigin.TransformOrigin) return Vector3.zero;
            bool found = false;
            Bounds bounds = default;
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                // Canvas, particles and trails have separate presentation semantics.
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                Bounds local = renderer.localBounds;
                if (!IsFinite(local.center) || !IsFinite(local.extents)) continue;
                Matrix4x4 toVisual = visual.worldToLocalMatrix * renderer.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = local.center + Vector3.Scale(local.extents, new Vector3(
                        (corner & 1) == 0 ? -1 : 1,
                        (corner & 2) == 0 ? -1 : 1,
                        (corner & 4) == 0 ? -1 : 1));
                    point = toVisual.MultiplyPoint3x4(point);
                    if (!IsFinite(point)) continue;
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            return found ? bounds.center : Vector3.zero;
        }

        private static bool IsFinite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
