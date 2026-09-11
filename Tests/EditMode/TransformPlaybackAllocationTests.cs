using System;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.Tweens.Tests
{
    public sealed class TransformPlaybackAllocationTests
    {
        [TestCase(100)]
        [TestCase(1000)]
        public void WarmTransformPlaybackDoesNotAllocate(int count)
        {
            var root = new GameObject("Tween allocation test");
            var bindings = new TransformVisibilityBinding[count];
            using var scheduler = new TweenScheduler(count);
            try
            {
                var settings = VisibilityTweenSettings.Exit;
                settings.seconds = 1;
                for (int i = 0; i < count; i++)
                {
                    var target = new GameObject("Target");
                    target.transform.SetParent(root.transform, false);
                    bindings[i] = new TransformVisibilityBinding(scheduler, target, target.transform, settings, settings);
                    bindings[i].SetVisible(false);
                }
                scheduler.Advance(0.001f, 0.001f);
                var timer = Stopwatch.StartNew();
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int frame = 0; frame < 100; frame++) scheduler.Advance(0.001f, 0.001f);
                long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                timer.Stop();
                Assert.That(bytes, Is.Zero);
                Assert.That(scheduler.ActiveCount, Is.EqualTo(count));
                Assert.That(scheduler.FaultCount, Is.Zero);
                TestContext.Out.WriteLine(count + " transforms x 100 ticks: " +
                    timer.Elapsed.TotalMilliseconds.ToString("F2") + " ms total; " + bytes + " managed bytes.");
            }
            finally
            {
                foreach (var binding in bindings) binding?.Dispose();
                Object.DestroyImmediate(root);
            }
        }
    }
}
