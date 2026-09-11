namespace Deucarian.Tweens
{
    public enum TweenStopReason { Completed, Cancelled, TargetLost, Faulted, Disposed }

    /// <summary>A reusable binding. Return false once its requested work is finished.</summary>
    public interface ITweenUpdate
    {
        bool IsAlive { get; }
        bool Advance(float scaledSeconds, float unscaledSeconds);
        void Stopped(TweenHandle handle, TweenStopReason reason);
    }
}
