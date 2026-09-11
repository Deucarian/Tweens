using Deucarian.Common;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Tweens.Tests
{
    public sealed class TransformVisibilityTests
    {
        private GameObject root;
        private TweenScheduler scheduler;
        private TransformVisibilityBinding binding;
        [SetUp] public void Setup()
        {
            root = new GameObject("Visibility target");
            root.transform.localScale = new Vector3(2, 3, 4);
            scheduler = new TweenScheduler();
            var settings = VisibilityTweenSettings.Enter;
            settings.seconds = 1; settings.easing = DeucarianEasing.Linear;
            binding = new TransformVisibilityBinding(scheduler, root, root.transform, settings, settings);
        }
        [TearDown] public void Teardown()
        {
            binding.Dispose(); scheduler.Dispose();
            if (root != null) Object.DestroyImmediate(root);
            TweenVisibilityDefaults.Configure(null); TweenVisibilityDefaults.ReducedMotion = false;
        }
        [Test] public void HideDisablesOnlyAfterCompletionAndRestoresAuthoredPose()
        {
            binding.SetVisible(false); scheduler.Advance(0.5f, 0.5f);
            Assert.That(root.activeSelf, Is.True); Assert.That(root.transform.localScale.x, Is.EqualTo(1));
            scheduler.Advance(0.5f, 0.5f);
            Assert.That(root.activeSelf, Is.False); Assert.That(root.transform.localScale, Is.EqualTo(new Vector3(2, 3, 4)));
        }
        [Test] public void ReShowCancelsPendingDestroy()
        {
            binding.HideAndDestroy(); scheduler.Advance(0.4f, 0.4f);
            binding.SetVisible(true); scheduler.Advance(1, 1);
            Assert.That(root != null, Is.True); Assert.That(root.activeSelf, Is.True);
        }
        [Test] public void ReShowFromCompletionCallbackCancelsDestructiveCompletion()
        {
            binding.Completed += visible => { if (!visible) binding.SetVisible(true); };
            binding.HideAndDestroy(); scheduler.Advance(1, 1);
            Assert.That(root != null, Is.True); Assert.That(root.activeSelf, Is.True);
            Assert.That(binding.IsAnimating, Is.True);
        }
        [Test] public void PoolReturnIsCalledExactlyOnceAfterHidden()
        {
            int returns = 0;
            binding.HideAndRelease(value => { Assert.That(value.activeSelf, Is.False); returns++; });
            scheduler.Advance(1, 1); scheduler.Advance(1, 1);
            Assert.That(returns, Is.EqualTo(1)); Assert.That(scheduler.FaultCount, Is.Zero);
        }
        [Test] public void BaselineRestoreCancelsPoolReturn()
        {
            int returns = 0; binding.HideAndRelease(_ => returns++);
            scheduler.Advance(0.4f, 0.4f); binding.RestoreImmediate(true); scheduler.Advance(1, 1);
            Assert.That(returns, Is.Zero); Assert.That(root.activeSelf, Is.True);
            Assert.That(root.transform.localScale, Is.EqualTo(new Vector3(2, 3, 4)));
        }
        [Test] public void DestroyedTargetIsRemovedWithoutFailure()
        {
            binding.SetVisible(false); Object.DestroyImmediate(root); scheduler.Advance(1, 1);
            Assert.That(scheduler.ActiveCount, Is.Zero); Assert.That(scheduler.FaultCount, Is.Zero);
        }
        [Test] public void ExternallyDisabledTargetCancelsWithoutDestructiveCallback()
        {
            int returns = 0; binding.HideAndRelease(_ => returns++);
            root.SetActive(false); scheduler.Advance(1, 1);
            Assert.That(returns, Is.Zero); Assert.That(scheduler.ActiveCount, Is.Zero);
            Assert.That(root.transform.localScale, Is.EqualTo(new Vector3(2, 3, 4)));
        }
        [Test] public void ReducedMotionSettlesImmediately()
        {
            TweenVisibilityDefaults.ReducedMotion = true;
            binding.SetVisible(false);
            Assert.That(root.activeSelf, Is.False); Assert.That(scheduler.ActiveCount, Is.Zero);
        }
        [Test] public void StableIdentifierAndObjectOverridesHaveDeterministicPrecedence()
        {
            var profile = ScriptableObject.CreateInstance<TweenVisibilityProfile>();
            var overrides = ScriptableObject.CreateInstance<TweenVisibilityOverrides>();
            try
            {
                profile.enter.style = VisibilityTweenStyle.Scale;
                var scope = root.AddComponent<TweenVisibilityScope>(); scope.profile = profile; scope.stableOverrides = overrides;
                overrides.entries = new[] { new StableVisibilityOverride { stableId = "element-123",
                    enter = new VisibilityTweenOverride { animation = VisibilityTweenSelection.Slide } } };
                using var stable = TweenVisibilityBindings.Create(scheduler, root, "element-123");
                Assert.That(stable.Enter.style, Is.EqualTo(VisibilityTweenStyle.Slide));
                var authored = root.AddComponent<TweenedVisibility>();
                authored.enter = new VisibilityTweenOverride { animation = VisibilityTweenSelection.None };
                using var instance = TweenVisibilityBindings.Create(scheduler, root, "element-123");
                Assert.That(instance.Enter.style, Is.EqualTo(VisibilityTweenStyle.None));
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(overrides); }
        }
    }
}
