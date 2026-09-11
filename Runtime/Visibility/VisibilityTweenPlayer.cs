using System;

namespace Deucarian.Tweens
{
    public interface IVisibilityTweenTarget
    {
        bool IsAlive { get; }
        void Apply(VisibilityTweenSample sample);
    }

    /// <summary>Reusable presentation binding. The caller remains the owner of desired visibility.</summary>
    public sealed class VisibilityTweenPlayer : ITweenUpdate, IDisposable
    {
        private readonly TweenScheduler scheduler;
        private readonly IVisibilityTweenTarget target;
        private readonly VisibilityProgress progress = new VisibilityProgress(false);
        private VisibilityTweenSettings settings;
        private TweenHandle handle;
        private bool unscaled;
        private bool disposed;
        private ulong revision;
        public VisibilityTweenPlayer(TweenScheduler scheduler, IVisibilityTweenTarget target, bool initiallyVisible)
        {
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            progress.Reset(initiallyVisible);
            TargetVisible = initiallyVisible;
        }
        public event Action<bool> Completed;
        public event Action<TweenStopReason> Cancelled;
        public bool TargetVisible { get; private set; }
        public bool IsAnimating => handle.IsActive;
        public bool IsAlive => !disposed && target.IsAlive;
        public float Progress => progress.Progress;

        public void SetVisible(bool visible, VisibilityTweenSettings value, bool animate = true, bool useUnscaledTime = true)
        {
            if (disposed) throw new ObjectDisposedException(nameof(VisibilityTweenPlayer));
            if (TargetVisible == visible && IsAnimating && animate) return;
            revision++;
            StopWithoutNotification();
            TargetVisible = visible;
            settings = value.Sanitized();
            unscaled = useUnscaledTime;
            progress.MoveTo(visible, animate && settings.style != VisibilityTweenStyle.None ? settings.seconds : 0, settings.easing);
            if (!animate || settings.style == VisibilityTweenStyle.None) progress.Reset(visible);
            ulong current = revision;
            if (target.IsAlive) target.Apply(VisibilityTweenSample.Evaluate(settings, progress.Progress));
            if (current != revision || disposed) return;
            if (progress.IsAnimating) handle = scheduler.Schedule(this);
            else Completed?.Invoke(visible);
        }

        public void Snap(bool visible, VisibilityTweenSettings value)
        {
            revision++;
            StopWithoutNotification();
            TargetVisible = visible;
            settings = value.Sanitized();
            progress.Reset(visible);
            if (target.IsAlive) target.Apply(VisibilityTweenSample.Evaluate(settings, progress.Progress));
        }

        public void Cancel() { handle.Cancel(); }
        public bool Advance(float scaledSeconds, float unscaledSeconds)
        {
            ulong current = revision;
            progress.Advance(unscaled ? unscaledSeconds : scaledSeconds);
            target.Apply(VisibilityTweenSample.Evaluate(settings, progress.Progress));
            return current != revision || progress.IsAnimating;
        }

        public void Stopped(TweenHandle stopped, TweenStopReason reason)
        {
            if (handle != stopped) return;
            handle = default;
            if (reason == TweenStopReason.Completed) Completed?.Invoke(TargetVisible);
            else Cancelled?.Invoke(reason);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            revision++;
            StopWithoutNotification();
            Completed = null;
            Cancelled = null;
        }
        private void StopWithoutNotification()
        {
            TweenHandle previous = handle;
            handle = default;
            previous.Cancel();
        }
    }
}
