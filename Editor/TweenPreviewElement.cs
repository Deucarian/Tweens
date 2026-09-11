using System;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Tweens.Editor
{
    /// <summary>Isolated preview using the runtime scheduler and sampling policy. Never edits scene objects.</summary>
    public sealed class TweenPreviewElement : VisualElement, IVisibilityTweenTarget, IDisposable
    {
        private readonly Func<VisibilityTweenSettings> enter;
        private readonly Func<VisibilityTweenSettings> exit;
        private readonly TweenScheduler scheduler = new TweenScheduler(1);
        private readonly VisibilityTweenPlayer player;
        private readonly VisualElement preview;
        private readonly Slider scrub;
        private readonly Label status;
        private readonly Label percentage;
        private readonly Toggle loopToggle;
        private readonly float unitsToPixels;
        private readonly Func<bool> reducedMotion;
        private readonly Func<bool> unscaledTime;
        private bool entering = true;
        private bool disposed;
        private bool listening;
        private bool loop;
        private double previousTime;
        private double nextLoopTime;
        private bool loopVisible;
        public bool IsAlive => !disposed;
        public bool IsAnimating => player.IsAnimating;

        public TweenPreviewElement(Func<VisibilityTweenSettings> enter, Func<VisibilityTweenSettings> exit, float unitsToPixels = 100,
            Func<bool> reducedMotion = null, Func<bool> unscaledTime = null)
        {
            this.enter = enter ?? throw new ArgumentNullException(nameof(enter));
            this.exit = exit ?? throw new ArgumentNullException(nameof(exit));
            this.unitsToPixels = unitsToPixels;
            this.reducedMotion = reducedMotion;
            this.unscaledTime = unscaledTime;
            name = "tween-preview";
            AddToClassList("dw-visibility-preview");
            VisualElement stage = DeucarianEditorWorkspaceControls.Region("tween-preview-stage", "dw-preview-stage");
            stage.Add(new BaselineOutline());
            preview = DeucarianEditorWorkspaceControls.Region("tween-preview-object", "dw-preview-object");
            preview.AddToClassList("dw-motion-specimen");
            preview.Add(DeucarianEditorWorkspaceControls.Label("Preview object"));
            stage.Add(preview);
            Add(stage);
            var controls = DeucarianEditorWorkspaceControls.Actions(
                DeucarianEditorWorkspaceControls.IconButton("Enter", DeucarianEditorIconIds.Play, () => Play(true), DeucarianEditorButtonRole.Primary),
                DeucarianEditorWorkspaceControls.IconButton("Exit", DeucarianEditorIconIds.Play, () => Play(false)),
                DeucarianEditorWorkspaceControls.IconButton("Stop", DeucarianEditorIconIds.Stop, Stop));
            loopToggle = new Toggle { tooltip = "Repeat enter and exit" }; loopToggle.AddToClassList("dw-checkbox");
            loopToggle.tooltip = "Repeat enter and exit";
            loopToggle.RegisterValueChangedCallback(e => { loop = e.newValue; if (loop) { nextLoopTime = 0; Listen(); } else if (!player.IsAnimating) Unlisten(); });
            Add(controls);
            var repeat = DeucarianEditorWorkspaceControls.Region(null, "dw-inline-checkbox");
            repeat.Add(loopToggle); repeat.Add(DeucarianEditorWorkspaceControls.Label("Loop")); controls.Add(repeat);
            scrub = new DeucarianEditorSlider(0, 1) { value = 1, name = "tween-preview-progress" };
            scrub.RegisterValueChangedCallback(e => Scrub(e.newValue));
            var progress = DeucarianEditorWorkspaceControls.Region(null, "dw-range-pair");
            percentage = DeucarianEditorWorkspaceControls.Label("100%", "dw-value-badge");
            progress.Add(scrub); progress.Add(percentage);
            Add(DeucarianEditorWorkspaceControls.Field("Visibility", progress));
            status = DeucarianEditorWorkspaceControls.Label("Preview only · no scene changes", "dw-muted");
            Add(status);
            player = new VisibilityTweenPlayer(scheduler, this, true);
            player.Completed += OnCompleted;
            scheduler.WorkAvailableChanged += OnWork;
            RegisterCallback<DetachFromPanelEvent>(_ => Stop());
        }

        public void Play(bool visible)
        {
            if (disposed) return;
            entering = visible;
            var settings = visible ? enter() : exit();
            if (!player.IsAnimating) player.Snap(!visible, settings);
            status.text = visible ? "Entering…" : "Exiting…";
            player.SetVisible(visible, settings, !(reducedMotion?.Invoke() ?? false), unscaledTime?.Invoke() ?? true);
            if (player.IsAnimating) Listen();
        }

        public void Stop()
        {
            if (disposed) return;
            loop = false;
            loopToggle.SetValueWithoutNotify(false);
            player.Cancel();
            Unlisten();
            status.text = "Paused preview.";
        }

        public void Scrub(float visibility)
        {
            if (disposed) return;
            Stop();
            float value = Mathf.Clamp01(visibility);
            scrub.SetValueWithoutNotify(value);
            percentage.text = value.ToString("P0");
            Apply(VisibilityTweenSample.Evaluate(entering ? enter() : exit(), value));
            status.text = entering ? "Enter · paused preview" : "Exit · paused preview";
        }

        public void Apply(VisibilityTweenSample sample)
        {
            preview.style.scale = new Scale(new Vector3(sample.Scale, sample.Scale, 1));
            preview.style.opacity = sample.Alpha;
            preview.style.translate = new Translate(sample.Offset.x * unitsToPixels, sample.Offset.y * -unitsToPixels, 0);
        }

        private void OnCompleted(bool visible)
        {
            if (!visible) preview.style.opacity = 0;
            loopVisible = !visible;
            nextLoopTime = EditorApplication.timeSinceStartup + 0.45;
            status.text = visible ? "Visible." : "Hidden.";
            if (!loop) Unlisten();
        }
        private void OnWork(bool work) { if (work || loop) Listen(); else Unlisten(); }
        private void Listen()
        {
            if (listening || disposed) return;
            listening = true;
            previousTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Advance;
        }
        private void Unlisten()
        {
            listening = false;
            EditorApplication.update -= Advance;
        }
        private void Advance()
        {
            if (disposed) return;
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)Math.Max(0, now - previousTime);
            previousTime = now;
            scheduler.Advance(delta, delta);
            scrub.SetValueWithoutNotify(Mathf.Clamp01(player.Progress));
            percentage.text = Mathf.Clamp01(player.Progress).ToString("P0");
            if (loop && !player.IsAnimating && now >= nextLoopTime) Play(loopVisible);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Unlisten();
            player.Dispose();
            scheduler.Dispose();
        }

        private sealed class BaselineOutline : VisualElement
        {
            internal BaselineOutline()
            {
                name = "tween-preview-baseline";
                pickingMode = PickingMode.Ignore;
                AddToClassList("dw-preview-baseline");
                generateVisualContent += Draw;
            }

            private void Draw(MeshGenerationContext context)
            {
                if (contentRect.width < 2 || contentRect.height < 2) return;
                const int dashesPerSide = 12;
                var mesh = context.Allocate(dashesPerSide * 16, dashesPerSide * 24);
                Color32 tint = resolvedStyle.borderTopColor;
                ushort vertex = 0;
                for (int side = 0; side < 4; side++)
                for (int dash = 0; dash < dashesPerSide; dash++)
                {
                    bool horizontal = side < 2;
                    float length = horizontal ? contentRect.width : contentRect.height;
                    float start = (dash + .2f) * length / dashesPerSide;
                    float end = (dash + .7f) * length / dashesPerSide;
                    var rect = horizontal
                        ? new Rect(start, side == 0 ? 0 : contentRect.height - 2, end - start, 2)
                        : new Rect(side == 2 ? 0 : contentRect.width - 2, start, 2, end - start);
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMin, rect.yMin, Vertex.nearZ), tint = tint });
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMax, rect.yMin, Vertex.nearZ), tint = tint });
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMax, rect.yMax, Vertex.nearZ), tint = tint });
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMin, rect.yMax, Vertex.nearZ), tint = tint });
                    mesh.SetNextIndex(vertex); mesh.SetNextIndex((ushort)(vertex + 1)); mesh.SetNextIndex((ushort)(vertex + 2));
                    mesh.SetNextIndex(vertex); mesh.SetNextIndex((ushort)(vertex + 2)); mesh.SetNextIndex((ushort)(vertex + 3));
                    vertex += 4;
                }
            }
        }
    }
}
