using System;
using Deucarian.Common;
using UnityEngine;

namespace Deucarian.Tweens
{
    /// <summary>
    /// Explicit show/hide lifetime adapter. A single cached binding can represent an imported
    /// element without adding a MonoBehaviour to it. No material instances are created.
    /// </summary>
    public sealed class TransformVisibilityBinding : IVisibilityTweenTarget, IDisposable
    {
        private readonly GameObject root;
        private readonly Transform visual;
        private readonly CanvasGroup canvas;
        private readonly VisibilityTweenPlayer player;
        private readonly Vector3 visibleScale;
        private readonly Vector3 visiblePosition;
        private readonly float visibleAlpha;
        private readonly bool blocksRaycasts;
        private readonly bool interactable;
        private Action<GameObject> release;
        private bool destroyAfterHide;
        private bool disposed;
        private float appliedScale = 1;
        private float appliedAlpha = 1;
        private Vector3 appliedOffset;
        private ulong revision;
        public VisibilityTweenSettings Enter { get; set; }
        public VisibilityTweenSettings Exit { get; set; }
        public bool UnscaledTime { get; set; } = true;
        public bool ReducedMotion { get; set; }
        private int applyingDepth;
        public bool IsApplying => applyingDepth > 0;
        public bool IsDisposed => disposed;
        public bool IsAlive => !disposed && root != null && visual != null;
        bool IVisibilityTweenTarget.IsAlive => IsAlive && root.activeInHierarchy;
        public bool IsAnimating => player.IsAnimating;
        public bool TargetVisible => player.TargetVisible;
        public bool SupportsFade => canvas != null;
        public event Action<bool> Completed;

        public TransformVisibilityBinding(TweenScheduler scheduler, GameObject root, Transform visual,
            VisibilityTweenSettings enter, VisibilityTweenSettings exit)
        {
            this.root = root != null ? root : throw new ArgumentNullException(nameof(root));
            this.visual = visual != null ? visual : root.transform;
            canvas = this.visual.GetComponent<CanvasGroup>();
            visibleScale = this.visual.localScale;
            visiblePosition = this.visual.localPosition;
            visibleAlpha = canvas != null ? canvas.alpha : 1;
            blocksRaycasts = canvas != null && canvas.blocksRaycasts;
            interactable = canvas != null && canvas.interactable;
            Enter = enter;
            Exit = exit;
            player = new VisibilityTweenPlayer(scheduler, this, root.activeSelf);
            player.Completed += OnCompleted;
            player.Cancelled += OnCancelled;
        }

        public void SetVisible(bool visible, bool animate = true)
        {
            if (!IsAlive) return;
            revision++;
            // Re-showing cancels any old destructive completion, including halfway through an exit.
            if (visible) { destroyAfterHide = false; release = null; }
            VisibilityTweenSettings settings = visible ? Enter : Exit;
            applyingDepth++;
            try
            {
                if (visible && !root.activeSelf)
                {
                    player.Snap(false, settings);
                    root.SetActive(true);
                }
                if (canvas != null) { canvas.blocksRaycasts = visible && blocksRaycasts; canvas.interactable = visible && interactable; }
                bool canAnimate = animate && !ReducedMotion && !TweenVisibilityDefaults.ReducedMotion && root.activeInHierarchy;
                // A mesh fade needs a deliberate renderer adapter; never instantiate materials implicitly.
                if (canvas == null && settings.style == VisibilityTweenStyle.Fade) canAnimate = false;
                player.SetVisible(visible, settings, canAnimate, UnscaledTime);
            }
            finally { applyingDepth--; }
        }

        public void ShowFromHidden(bool animate = true)
        {
            if (!IsAlive) return;
            player.Snap(false, Enter);
            SetVisible(true, animate);
        }

        public void HideAndDestroy(bool animate = true)
        {
            if (!IsAlive) return;
            destroyAfterHide = true;
            release = null;
            SetVisible(false, animate);
        }

        public void HideAndRelease(Action<GameObject> returnToPool, bool animate = true)
        {
            if (returnToPool == null) throw new ArgumentNullException(nameof(returnToPool));
            if (!IsAlive) return;
            destroyAfterHide = false;
            release = returnToPool;
            SetVisible(false, animate);
        }

        public void RestoreImmediate(bool visible)
        {
            if (!IsAlive) return;
            revision++;
            destroyAfterHide = false;
            release = null;
            player.Snap(visible, visible ? Enter : Exit);
            RestorePose();
            applyingDepth++;
            try { root.SetActive(visible); }
            finally { applyingDepth--; }
        }

        public void ExternalDisable()
        {
            if (disposed || IsApplying) return;
            revision++;
            destroyAfterHide = false;
            release = null;
            player.Snap(false, Exit);
            RestorePose();
        }

        public void Apply(VisibilityTweenSample sample)
        {
            if (!IsAlive) return;
            // Avoid native property writes for channels this animation does not change.
            // The visual's authored pose is owned by this binding for its lifetime.
            if (appliedScale != sample.Scale)
            { visual.localScale = visibleScale * sample.Scale; appliedScale = sample.Scale; }
            if (appliedOffset != sample.Offset)
            { visual.localPosition = visiblePosition + sample.Offset; appliedOffset = sample.Offset; }
            if (canvas != null && appliedAlpha != sample.Alpha)
            { canvas.alpha = visibleAlpha * sample.Alpha; appliedAlpha = sample.Alpha; }
        }

        private void OnCompleted(bool visible)
        {
            if (!IsAlive) return;
            ulong completedRevision = revision;
            bool destroy = destroyAfterHide;
            Action<GameObject> returnToPool = release;
            destroyAfterHide = false;
            release = null;
            RestorePose();
            if (!visible)
            {
                applyingDepth++;
                try { root.SetActive(false); }
                finally { applyingDepth--; }
                if (completedRevision != revision || !IsAlive) return;
                Completed?.Invoke(false);
                if (completedRevision != revision || !IsAlive) return;
                if (destroy) UnityObjectUtility.DestroySafely(root);
                else returnToPool?.Invoke(root);
            }
            else Completed?.Invoke(true);
        }

        private void OnCancelled(TweenStopReason reason)
        {
            destroyAfterHide = false;
            release = null;
            RestorePose();
        }

        private void RestorePose()
        {
            appliedScale = appliedAlpha = 1;
            appliedOffset = Vector3.zero;
            if (visual != null) { visual.localScale = visibleScale; visual.localPosition = visiblePosition; }
            if (canvas != null)
            { canvas.alpha = visibleAlpha; canvas.blocksRaycasts = blocksRaycasts; canvas.interactable = interactable; }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            player.Dispose();
            RestorePose();
            release = null;
            Completed = null;
        }
    }
}
