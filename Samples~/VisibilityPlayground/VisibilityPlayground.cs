using System.Collections.Generic;
using Deucarian.Common;
using UnityEngine;

namespace Deucarian.Tweens.Samples
{
    /// <summary>Playable lifecycle demo. Its controls stay alive while every animated target is hidden.</summary>
    public sealed class VisibilityPlayground : MonoBehaviour
    {
        [Range(1, 1000)] public int objectCount = 100;
        [SerializeField] private Material previewMaterial;
        private readonly List<TransformVisibilityBinding> bindings = new List<TransformVisibilityBinding>();
        private readonly HashSet<GameObject> pool = new HashSet<GameObject>();
        private GameObject stage;
        private Material material;
        private bool reduced;

        private void Start()
        {
            stage = new GameObject("Visibility playground stage");
            stage.transform.SetParent(transform, false);
            var cameraRoot = new GameObject("Playground camera");
            cameraRoot.transform.SetParent(stage.transform);
            var camera = cameraRoot.AddComponent<Camera>();
            camera.transform.localPosition = new Vector3(0, 0, -20);
            camera.orthographic = true; camera.orthographicSize = 7;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.055f, 0.075f);
            var lightRoot = new GameObject("Playground light");
            lightRoot.transform.SetParent(stage.transform);
            var light = lightRoot.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.5f;
            lightRoot.transform.rotation = Quaternion.Euler(35, -30, 0);
            var pipeline = QualitySettings.renderPipeline != null
                ? QualitySettings.renderPipeline : UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            Shader shader = pipeline == null
                ? Shader.Find("Standard") : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (previewMaterial == null && shader != null) { material = new Material(shader); material.color = new Color(0.65f, 0.4f, 0.9f); }
            int columns = Mathf.CeilToInt(Mathf.Sqrt(objectCount));
            float spacing = 10f / columns;
            for (int i = 0; i < objectCount; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Animated object " + i;
                cube.transform.SetParent(stage.transform, false);
                cube.transform.localPosition = new Vector3((i % columns - (columns - 1) * 0.5f) * spacing,
                    (i / columns - (columns - 1) * 0.5f) * spacing, 0);
                cube.transform.localScale = Vector3.one * spacing * 0.65f;
                if (previewMaterial != null || material != null) cube.GetComponent<Renderer>().sharedMaterial = previewMaterial != null ? previewMaterial : material;
                var enter = VisibilityTweenSettings.Enter;
                var exit = VisibilityTweenSettings.Exit;
                if (i % 2 != 0) { enter.style = exit.style = VisibilityTweenStyle.Slide; enter.hiddenOffset = exit.hiddenOffset = Vector3.down * spacing; }
                var binding = new TransformVisibilityBinding(TweenRuntime.Scheduler, cube, cube.transform, enter, exit);
                bindings.Add(binding); binding.ShowFromHidden();
            }
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 430, 220), GUI.skin.box);
            GUILayout.Label("Shared visibility animations — " + bindings.Count + " objects");
            GUILayout.Label("Alternate scale / slide. Reverse while moving to test cancellation.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Show all")) SetVisible(true);
            if (GUILayout.Button("Hide all")) SetVisible(false);
            if (GUILayout.Button("Restore now")) foreach (var value in bindings) value.RestoreImmediate(true);
            GUILayout.EndHorizontal();
            reduced = GUILayout.Toggle(reduced, "Reduced motion (settle next command immediately)");
            if (GUILayout.Button("Hide and pool first visible object"))
                foreach (var value in bindings) if (value.IsAlive && value.TargetVisible) { value.HideAndRelease(ReturnToPool); break; }
            if (GUILayout.Button("Hide and destroy first remaining object"))
                foreach (var value in bindings) if (value.IsAlive) { value.HideAndDestroy(); break; }
            GUILayout.Label("Active scheduler entries: " + TweenRuntime.ActiveCount + " | Pooled: " + pool.Count);
            GUILayout.EndArea();
        }
        private void SetVisible(bool visible)
        {
            if (visible) pool.Clear();
            foreach (var value in bindings) { value.ReducedMotion = reduced; value.SetVisible(visible); }
        }
        private void ReturnToPool(GameObject value) => pool.Add(value);
        private void OnDestroy()
        {
            foreach (var value in bindings) value.Dispose();
            bindings.Clear();
            pool.Clear();
            UnityObjectUtility.DestroySafely(material);
            UnityObjectUtility.DestroySafely(stage);
        }
    }
}
