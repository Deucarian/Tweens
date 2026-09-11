using System;
using Deucarian.Common;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Tweens.Tests
{
    public sealed class VisibilityTweenTests
    {
        private sealed class Target : IVisibilityTweenTarget
        {
            public bool IsAlive => true;
            public VisibilityTweenSample Sample;
            public void Apply(VisibilityTweenSample sample) { Sample = sample; }
        }
        private static VisibilityTweenSettings Linear => new VisibilityTweenSettings {
            style = VisibilityTweenStyle.Scale, seconds = 1, easing = DeucarianEasing.Linear, hiddenScale = 0 };

        [Test] public void ReversalStartsFromCurrentPresentationWithoutJumping()
        {
            using var scheduler = new TweenScheduler(); var target = new Target();
            using var player = new VisibilityTweenPlayer(scheduler, target, false);
            player.SetVisible(true, Linear); scheduler.Advance(0.4f, 0.4f);
            player.SetVisible(false, Linear);
            Assert.That(target.Sample.Scale, Is.EqualTo(0.4f).Within(0.001f));
            scheduler.Advance(0.4f, 0.4f); Assert.That(target.Sample.Scale, Is.Zero.Within(0.001f));
        }
        [Test] public void RepeatedVisibleCommandDoesNotRestartItsTween()
        {
            using var scheduler = new TweenScheduler(); var target = new Target();
            using var player = new VisibilityTweenPlayer(scheduler, target, false);
            player.SetVisible(true, Linear); scheduler.Advance(0.4f, 0.4f);
            player.SetVisible(true, Linear); scheduler.Advance(0.6f, 0.6f);
            Assert.That(player.IsAnimating, Is.False); Assert.That(player.Progress, Is.EqualTo(1));
        }
        [Test] public void CompletionCanStartANewTween()
        {
            using var scheduler = new TweenScheduler(); var target = new Target();
            using var player = new VisibilityTweenPlayer(scheduler, target, false);
            player.Completed += visible => { if (visible) player.SetVisible(false, Linear); };
            player.SetVisible(true, Linear); scheduler.Advance(1, 1);
            Assert.That(player.IsAnimating, Is.True); Assert.That(player.TargetVisible, Is.False);
            scheduler.Advance(1, 1); Assert.That(player.IsAnimating, Is.False);
        }
        [Test] public void ImmediateCommandCancelsPendingCompletion()
        {
            using var scheduler = new TweenScheduler(); var target = new Target();
            using var player = new VisibilityTweenPlayer(scheduler, target, true);
            int calls = 0; player.Completed += _ => calls++;
            player.SetVisible(false, Linear); player.Snap(true, Linear); scheduler.Advance(2, 2);
            Assert.That(calls, Is.Zero); Assert.That(player.Progress, Is.EqualTo(1));
        }
        [Test] public void UnscaledTweenRunsWhileScaledTimeIsPaused()
        {
            using var scheduler = new TweenScheduler();
            using var player = new VisibilityTweenPlayer(scheduler, new Target(), false);
            player.SetVisible(true, Linear, true, true); scheduler.Advance(0, 1);
            Assert.That(player.IsAnimating, Is.False);
        }
        [Test] public void NoneCompletesWithoutRegisteringWork()
        {
            using var scheduler = new TweenScheduler();
            using var player = new VisibilityTweenPlayer(scheduler, new Target(), false);
            var settings = Linear; settings.style = VisibilityTweenStyle.None;
            player.SetVisible(true, settings);
            Assert.That(scheduler.ActiveCount, Is.Zero); Assert.That(player.Progress, Is.EqualTo(1));
        }
        [Test] public void OverrideInheritsTimingUnlessExplicitlyChanged()
        {
            var value = new VisibilityTweenOverride { animation = VisibilityTweenSelection.Slide };
            var result = value.Resolve(Linear);
            Assert.That(result.style, Is.EqualTo(VisibilityTweenStyle.Slide)); Assert.That(result.seconds, Is.EqualTo(1));
        }
        [Test] public void InvalidDurationsCannotPoisonTheScheduler()
        {
            var progress = new VisibilityProgress();
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.MoveTo(true, float.NaN, DeucarianEasing.Linear));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.Advance(float.PositiveInfinity));
        }
    }
}
