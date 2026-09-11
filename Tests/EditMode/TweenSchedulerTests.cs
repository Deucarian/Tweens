using System;
using Deucarian.Diagnostics;
using NUnit.Framework;

namespace Deucarian.Tweens.Tests
{
    public sealed class TweenSchedulerTests
    {
        private sealed class Target : ITweenUpdate
        {
            public bool Alive = true;
            public bool Keep = true;
            public int Ticks;
            public int Stops;
            public Action Tick;
            public Action<TweenStopReason> Stop;
            public bool IsAlive => Alive;
            public bool Advance(float scaled, float unscaled) { Ticks++; Tick?.Invoke(); return Keep; }
            public void Stopped(TweenHandle handle, TweenStopReason reason) { Stops++; Stop?.Invoke(reason); }
        }

        [Test] public void EmptySchedulerDoesNotPerformBindingWork()
        {
            using var scheduler = new TweenScheduler();
            scheduler.Advance(1, 1);
            Assert.That(scheduler.UpdateCount, Is.Zero);
        }
        [Test] public void CompletedBindingIsRemovedBeforeItsCallback()
        {
            using var scheduler = new TweenScheduler();
            var target = new Target { Keep = false };
            TweenHandle handle = scheduler.Schedule(target);
            target.Stop = reason => { Assert.That(handle.IsActive, Is.False); Assert.That(reason, Is.EqualTo(TweenStopReason.Completed)); };
            scheduler.Advance(1, 1);
            scheduler.Advance(1, 1);
            Assert.That(target.Ticks, Is.EqualTo(1));
            Assert.That(target.Stops, Is.EqualTo(1));
            Assert.That(scheduler.FaultCount, Is.Zero, "Assertions in isolated callbacks must not be swallowed.");
        }
        [Test] public void RecycledSlotCannotBeCancelledByAnOldHandle()
        {
            using var scheduler = new TweenScheduler(1);
            var first = scheduler.Schedule(new Target()); first.Cancel();
            var second = scheduler.Schedule(new Target()); first.Cancel();
            Assert.That(second.IsActive, Is.True);
        }
        [Test] public void CallbackMayCancelAnEarlierBindingWithoutSkippingLaterWork()
        {
            using var scheduler = new TweenScheduler();
            var a = new Target(); var b = new Target(); var c = new Target(); var d = new Target();
            var first = scheduler.Schedule(a); scheduler.Schedule(b); scheduler.Schedule(c); scheduler.Schedule(d);
            b.Tick = first.Cancel;
            scheduler.Advance(1, 1);
            Assert.That(new[] { a.Ticks, b.Ticks, c.Ticks, d.Ticks }, Is.EqualTo(new[] { 1, 1, 1, 1 }));
        }
        [Test] public void CallbackScheduledBindingStartsNextTick()
        {
            using var scheduler = new TweenScheduler(1);
            var next = new Target();
            var target = new Target { Keep = false, Stop = _ => scheduler.Schedule(next) };
            scheduler.Schedule(target); scheduler.Advance(1, 1);
            Assert.That(next.Ticks, Is.Zero);
            scheduler.Advance(1, 1); Assert.That(next.Ticks, Is.EqualTo(1));
        }
        [Test] public void SelfCancellationAndReplacementRemainSafe()
        {
            using var scheduler = new TweenScheduler(1);
            var next = new Target(); var target = new Target();
            var handle = scheduler.Schedule(target);
            target.Tick = () => { handle.Cancel(); scheduler.Schedule(next); };
            scheduler.Advance(1, 1);
            Assert.That(target.Stops, Is.EqualTo(1)); Assert.That(next.Ticks, Is.Zero);
        }
        [Test] public void LostTargetIsCancelledWithoutCallingIt()
        {
            using var scheduler = new TweenScheduler();
            var target = new Target { Alive = false };
            scheduler.Schedule(target); scheduler.Advance(1, 1);
            Assert.That(target.Ticks, Is.Zero); Assert.That(target.Stops, Is.EqualTo(1));
        }
        [Test] public void BindingFailureDoesNotPreventOtherBindingsFromUpdating()
        {
            using var scheduler = new TweenScheduler();
            scheduler.Schedule(new Target { Tick = () => throw new InvalidOperationException("private payload") });
            var good = new Target(); scheduler.Schedule(good); scheduler.Advance(1, 1);
            Assert.That(good.Ticks, Is.EqualTo(1)); Assert.That(scheduler.FaultCount, Is.EqualTo(1));
            Assert.That(scheduler.ActiveCount, Is.EqualTo(1));
        }
        [Test] public void DisposeRemovesDiagnosticProviderAndRejectsNewWork()
        {
            int before = DiagnosticProviderRegistry.SnapshotProviders().Count;
            var scheduler = new TweenScheduler(); scheduler.Schedule(new Target());
            Assert.That(DiagnosticProviderRegistry.SnapshotProviders().Count, Is.EqualTo(before + 1));
            scheduler.Dispose(); scheduler.Dispose();
            Assert.That(DiagnosticProviderRegistry.SnapshotProviders().Count, Is.EqualTo(before));
            Assert.Throws<ObjectDisposedException>(() => scheduler.Schedule(new Target()));
        }
        [Test] public void WarmTickingDoesNotAllocate()
        {
            using var scheduler = new TweenScheduler(1000);
            var target = new Target();
            for (int i = 0; i < 1000; i++) scheduler.Schedule(target);
            scheduler.Advance(0.01f, 0.01f);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) scheduler.Advance(0.01f, 0.01f);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(bytes, Is.Zero);
        }
        [Test] public void WarmRegistrationAndCancellationDoNotAllocate()
        {
            using var scheduler = new TweenScheduler(1);
            var target = new Target();
            scheduler.Schedule(target).Cancel();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) scheduler.Schedule(target).Cancel();
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(bytes, Is.Zero);
        }
        [Test] public void DisposeFromCallbackCancelsRemainingWorkSafely()
        {
            using var scheduler = new TweenScheduler();
            var second = new Target();
            scheduler.Schedule(new Target { Tick = scheduler.Dispose });
            scheduler.Schedule(second);
            scheduler.Advance(1, 1);
            Assert.That(second.Ticks, Is.Zero);
            Assert.That(second.Stops, Is.EqualTo(1));
            Assert.That(scheduler.ActiveCount, Is.Zero);
            Assert.That(scheduler.FaultCount, Is.Zero);
        }
        [Test] public void WorkAvailabilityTracksOnlyTransitionsBetweenEmptyAndNonempty()
        {
            using var scheduler = new TweenScheduler();
            int calls = 0; scheduler.WorkAvailableChanged += _ => calls++;
            var a = scheduler.Schedule(new Target()); var b = scheduler.Schedule(new Target());
            a.Cancel(); Assert.That(calls, Is.EqualTo(1)); b.Cancel(); Assert.That(calls, Is.EqualTo(2));
        }
    }
}
