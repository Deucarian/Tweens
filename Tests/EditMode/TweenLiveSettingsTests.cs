using Deucarian.Common;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Tweens.Tests
{
    public sealed class TweenLiveSettingsTests
    {
        private sealed class Target : IVisibilityTweenTarget
        {
            public bool IsAlive => true;
            public VisibilityTweenSample Sample;
            public void Apply(VisibilityTweenSample sample) => Sample = sample;
        }

        private static VisibilityTweenSettings Settings => new VisibilityTweenSettings {
            style = VisibilityTweenStyle.Scale, seconds = 1, easing = DeucarianEasing.Linear };

        [Test]
        public void DurationEditKeepsCurrentPhaseAndAppliesNewShapeImmediately()
        {
            using var scheduler = new TweenScheduler();
            var target = new Target();
            using var player = new VisibilityTweenPlayer(scheduler, target, false);
            player.SetVisible(true, Settings);
            int completions = 0, cancellations = 0;
            player.Completed += _ => completions++;
            player.Cancelled += _ => cancellations++;
            scheduler.Advance(.25f, .25f);
            var changed = Settings;
            changed.seconds = 2;
            changed.hiddenScale = .2f;
            player.RefreshSettings(changed);
            Assert.That(player.Progress, Is.EqualTo(.25f).Within(.0001f));
            Assert.That(target.Sample.Scale, Is.EqualTo(.4f).Within(.0001f));
            Assert.That(scheduler.ActiveCount, Is.EqualTo(1));
            Assert.That(completions, Is.Zero);
            Assert.That(cancellations, Is.Zero);
            scheduler.Advance(1.5f, 1.5f);
            Assert.That(player.IsAnimating, Is.False);
            Assert.That(player.Progress, Is.EqualTo(1));
        }

        [Test]
        public void EasingEditUsesCurrentTimeInsteadOfRestarting()
        {
            using var scheduler = new TweenScheduler();
            using var player = new VisibilityTweenPlayer(scheduler, new Target(), false);
            player.SetVisible(true, Settings);
            scheduler.Advance(.5f, .5f);
            var changed = Settings;
            changed.easing = DeucarianEasing.EaseInCubic;
            player.RefreshSettings(changed);
            Assert.That(player.Progress, Is.EqualTo(.125f).Within(.0001f));
            scheduler.Advance(.5f, .5f);
            Assert.That(player.Progress, Is.EqualTo(1));
            Assert.That(player.IsAnimating, Is.False);
        }

        [Test]
        public void RepeatedRefreshesDoNotKeepPlaybackAliveForever()
        {
            using var scheduler = new TweenScheduler();
            using var player = new VisibilityTweenPlayer(scheduler, new Target(), false);
            player.SetVisible(true, Settings);
            int completed = 0;
            player.Completed += _ => completed++;
            for (int i = 0; i < 101; i++)
            {
                player.RefreshSettings(Settings);
                scheduler.Advance(.01f, .01f);
            }
            Assert.That(player.IsAnimating, Is.False);
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void ReducedMotionEditSettlesOnceWithoutLosingCompletionReentrancy()
        {
            using var scheduler = new TweenScheduler();
            using var player = new VisibilityTweenPlayer(scheduler, new Target(), false);
            player.SetVisible(true, Settings);
            int completed = 0;
            player.Completed += visible => { completed++; if (visible) player.SetVisible(false, Settings); };
            player.RefreshSettings(Settings, false);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(player.IsAnimating, Is.True);
            Assert.That(player.TargetVisible, Is.False);
            scheduler.Advance(1, 1);
            Assert.That(completed, Is.EqualTo(2));
        }

        [Test]
        public void CustomCurveSamplesItsShapeButAlwaysSettlesExactlyAtEndpoints()
        {
            var custom = AnimationCurve.Linear(0, .2f, 1, 1.4f);
            var progress = new VisibilityProgress(false);
            progress.MoveTo(true, 1, DeucarianEasing.Linear, custom);
            Assert.That(progress.Progress, Is.Zero);
            progress.Advance(.5f);
            Assert.That(progress.Progress, Is.EqualTo(.8f).Within(.0001f));
            progress.Advance(.5f);
            Assert.That(progress.Progress, Is.EqualTo(1));
            progress.MoveTo(false, 1, DeucarianEasing.Linear, custom);
            progress.Advance(1);
            Assert.That(progress.Progress, Is.Zero);
        }

        [Test]
        public void EmptyCurveFallsBackToPresetAndOverridePreservesInactiveCurve()
        {
            var progress = new VisibilityProgress();
            progress.MoveTo(true, 1, DeucarianEasing.EaseInCubic, new AnimationCurve());
            progress.Advance(.5f);
            Assert.That(progress.Progress, Is.EqualTo(.125f).Within(.0001f));
            var inherited = Settings;
            inherited.useCustomCurve = true;
            inherited.customCurve = AnimationCurve.Linear(0, 0, 1, 1);
            var local = new VisibilityTweenOverride { seconds = .5f, customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1) };
            Assert.That(local.Resolve(inherited).customCurve, Is.SameAs(inherited.customCurve));
            local.overrideTiming = true;
            var resolved = local.Resolve(inherited);
            Assert.That(resolved.useCustomCurve, Is.False);
            Assert.That(resolved.customCurve, Is.SameAs(local.customCurve));
            local.useCustomCurve = true;
            Assert.That(local.Resolve(inherited).useCustomCurve, Is.True);
        }

        [Test]
        public void CurveEditsUseTheSameActiveSchedulerEntry()
        {
            using var scheduler = new TweenScheduler();
            using var player = new VisibilityTweenPlayer(scheduler, new Target(), false);
            var settings = Settings;
            settings.useCustomCurve = true;
            settings.customCurve = AnimationCurve.Linear(0, 0, 1, .5f);
            player.SetVisible(true, settings);
            scheduler.Advance(.5f, .5f);
            Assert.That(player.Progress, Is.EqualTo(.25f).Within(.0001f));
            settings.customCurve = AnimationCurve.Linear(0, 0, 1, 1);
            player.RefreshSettings(settings);
            Assert.That(player.Progress, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(scheduler.ActiveCount, Is.EqualTo(1));
            scheduler.Advance(.5f, .5f);
            Assert.That(player.IsAnimating, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NonFiniteCurveSamplesFallBackToSelectedPreset(bool infinity)
        {
            float invalid = infinity ? float.PositiveInfinity : float.NaN;
            var progress = new VisibilityProgress(false);
            progress.MoveTo(true, 1, DeucarianEasing.EaseInCubic,
                new AnimationCurve(new Keyframe(0, invalid), new Keyframe(1, invalid)));
            progress.Advance(.5f);
            Assert.That(progress.Progress, Is.EqualTo(.125f).Within(.0001f));
        }

        [Test]
        public void NullCurveUsesPresetAndDisabledCustomCurveIsIgnored()
        {
            var progress = new VisibilityProgress();
            progress.MoveTo(true, 1, DeucarianEasing.EaseInCubic, null);
            progress.Advance(.5f);
            Assert.That(progress.Progress, Is.EqualTo(.125f).Within(.0001f));
            using var scheduler = new TweenScheduler();
            using var player = new VisibilityTweenPlayer(scheduler, new Target(), false);
            var settings = Settings;
            settings.customCurve = AnimationCurve.Linear(0, 0, 1, 2);
            player.SetVisible(true, settings);
            scheduler.Advance(.5f, .5f);
            Assert.That(player.Progress, Is.EqualTo(.5f).Within(.0001f));
        }

        [Test]
        public void NonmonotonicAndOvershootingCurvesDoNotCompleteEarly()
        {
            var progress = new VisibilityProgress(false);
            var curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(.3f, 1.2f),
                new Keyframe(.7f, -.2f), new Keyframe(1, 1));
            int completed = 0;
            progress.Completed += _ => completed++;
            progress.MoveTo(true, 1, DeucarianEasing.Linear, curve);
            progress.Advance(.3f);
            Assert.That(progress.Progress, Is.EqualTo(1.2f).Within(.0001f));
            Assert.That(progress.IsAnimating, Is.True);
            progress.Advance(.4f);
            Assert.That(progress.Progress, Is.EqualTo(-.2f).Within(.0001f));
            Assert.That(completed, Is.Zero);
            progress.Advance(.3f);
            Assert.That(progress.Progress, Is.EqualTo(1));
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void PlaybackBorrowsCallerCurveAndNeverMutatesItsKeys()
        {
            var curve = AnimationCurve.Linear(0, 0, 1, .5f);
            var progress = new VisibilityProgress(false);
            progress.MoveTo(true, 1, DeucarianEasing.Linear, curve);
            progress.Advance(.25f);
            Assert.That(curve[1].value, Is.EqualTo(.5f));
            curve.keys = AnimationCurve.Linear(0, 0, 1, 1).keys;
            progress.Advance(.25f);
            Assert.That(progress.Progress, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(curve[1].value, Is.EqualTo(1));
        }

        [Test]
        public void LiveCurvePlaybackRefreshIsAllocationFreeAfterWarmup()
        {
            using var scheduler = new TweenScheduler();
            using var player = new VisibilityTweenPlayer(scheduler, new Target(), false);
            var settings = Settings;
            settings.useCustomCurve = true;
            settings.customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            player.SetVisible(true, settings);
            for (int i = 0; i < 20; i++) { player.RefreshSettings(settings); scheduler.Advance(.001f, .001f); }
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) { player.RefreshSettings(settings); scheduler.Advance(.001f, .001f); }
            long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(bytes, Is.Zero);
        }
    }
}
