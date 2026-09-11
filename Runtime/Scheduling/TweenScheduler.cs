using System;
using Deucarian.Diagnostics;

namespace Deucarian.Tweens
{
    /// <summary>
    /// Main-thread, active-only scheduler. Registrations made during a tick start next tick.
    /// Swap removal and generation handles allow callbacks to cancel or start other tweens.
    /// Storage growth is explicit; warmed-up registration and ticking do not allocate.
    /// </summary>
    public sealed class TweenScheduler : IDisposable
    {
        private struct Slot
        {
            public ITweenUpdate Target;
            public int DenseIndex;
            public int NextFree;
            public ulong Generation;
        }

        private Slot[] slots;
        private int[] active;
        private TweenHandle[] frameHandles;
        private int nextSlot;
        private int freeHead = -1;
        private bool advancing;
        private bool clearing;
        private bool disposed;
        private readonly DiagnosticProviderRegistration diagnostics;

        public TweenScheduler(int capacity = 64)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            slots = new Slot[capacity];
            active = new int[capacity];
            frameHandles = new TweenHandle[capacity];
            diagnostics = DiagnosticProviderRegistry.Register(new TweenDiagnosticProvider(this));
        }

        public event Action<bool> WorkAvailableChanged;
        public int ActiveCount { get; private set; }
        public int PeakActiveCount { get; private set; }
        public int Capacity => slots.Length;
        public long UpdateCount { get; private set; }
        public long FaultCount { get; private set; }
        public bool IsDisposed => disposed;

        public void EnsureCapacity(int capacity)
        {
            ThrowIfDisposed();
            if (capacity <= slots.Length) return;
            Array.Resize(ref slots, capacity);
            Array.Resize(ref active, capacity);
            Array.Resize(ref frameHandles, capacity);
        }

        public TweenHandle Schedule(ITweenUpdate target)
        {
            ThrowIfDisposed();
            if (clearing) throw new InvalidOperationException("Cannot schedule while clearing tweens.");
            if (target == null) throw new ArgumentNullException(nameof(target));
            int id;
            if (freeHead >= 0) { id = freeHead; freeHead = slots[id].NextFree; }
            else
            {
                if (nextSlot == slots.Length) EnsureCapacity(checked(slots.Length * 2));
                id = nextSlot++;
            }
            ulong generation = slots[id].Generation + 1;
            if (generation == 0) generation = 1;
            slots[id] = new Slot { Target = target, DenseIndex = ActiveCount,
                Generation = generation, NextFree = -1 };
            active[ActiveCount++] = id;
            PeakActiveCount = Math.Max(PeakActiveCount, ActiveCount);
            if (ActiveCount == 1) NotifyWork(true);
            return new TweenHandle(this, id, generation);
        }

        public bool Contains(TweenHandle handle) => ReferenceEquals(handle.Owner, this) &&
            handle.Slot >= 0 && handle.Slot < nextSlot &&
            slots[handle.Slot].Target != null && slots[handle.Slot].Generation == handle.Generation;

        public void Cancel(TweenHandle handle) => Remove(handle, TweenStopReason.Cancelled);

        public void Advance(float scaledSeconds, float unscaledSeconds)
        {
            ThrowIfDisposed();
            ValidateDelta(scaledSeconds);
            ValidateDelta(unscaledSeconds);
            if (advancing) throw new InvalidOperationException("A tween tick cannot be nested.");
            if (ActiveCount == 0) return;
            advancing = true;
            try
            {
                // Reuse a handle snapshot: swap-removal can move an unvisited target behind
                // the iteration cursor. Generation checks also exclude replacements/new work.
                int count = ActiveCount;
                TweenHandle[] snapshot = frameHandles;
                for (int i = 0; i < count; i++)
                {
                    int id = active[i];
                    snapshot[i] = new TweenHandle(this, id, slots[id].Generation);
                }
                for (int index = 0; index < count; index++)
                {
                    TweenHandle handle = snapshot[index];
                    snapshot[index] = default;
                    if (!Contains(handle)) continue;
                    ITweenUpdate target = slots[handle.Slot].Target;
                    try
                    {
                        if (!target.IsAlive) Remove(handle, TweenStopReason.TargetLost);
                        else
                        {
                            UpdateCount++;
                            if (!target.Advance(scaledSeconds, unscaledSeconds))
                                Remove(handle, TweenStopReason.Completed);
                        }
                    }
                    catch (Exception)
                    {
                        FaultCount++;
                        Remove(handle, TweenStopReason.Faulted);
                    }
                }
            }
            finally { advancing = false; }
        }

        public void Clear() => Clear(TweenStopReason.Cancelled);

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Clear(TweenStopReason.Disposed);
            WorkAvailableChanged = null;
            diagnostics.Dispose();
        }

        private void Clear(TweenStopReason reason)
        {
            if (clearing) return;
            clearing = true;
            try
            {
                while (ActiveCount > 0)
                {
                    int id = active[ActiveCount - 1];
                    Remove(new TweenHandle(this, id, slots[id].Generation), reason);
                }
            }
            finally { clearing = false; }
        }

        private void Remove(TweenHandle handle, TweenStopReason reason)
        {
            if (!Contains(handle)) return;
            int id = handle.Slot;
            Slot slot = slots[id];
            int lastId = active[--ActiveCount];
            active[slot.DenseIndex] = lastId;
            slots[lastId].DenseIndex = slot.DenseIndex;
            slots[id].Target = null;
            slots[id].NextFree = freeHead;
            freeHead = id;
            if (ActiveCount == 0) NotifyWork(false);
            try { slot.Target.Stopped(handle, reason); }
            catch (Exception) { FaultCount++; }
        }

        private void NotifyWork(bool available)
        {
            try { WorkAvailableChanged?.Invoke(available); }
            catch (Exception) { FaultCount++; }
        }

        private static void ValidateDelta(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(TweenScheduler));
        }
    }
}
