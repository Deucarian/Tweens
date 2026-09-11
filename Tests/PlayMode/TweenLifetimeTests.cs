using System.Collections;
using Deucarian.Common;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deucarian.Tweens.Tests
{
    public sealed class TweenLifetimeTests
    {
        private GameObject root;
        private TransformVisibilityBinding binding;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            binding?.Dispose();
            if (root != null) Object.Destroy(root);
            TweenVisibilityDefaults.Configure(null);
            TweenVisibilityDefaults.ReducedMotion = false;
            yield return null;
        }
        private void CreateTarget()
        {
            root = new GameObject("Tween lifetime target");
            var settings = VisibilityTweenSettings.Enter;
            settings.seconds = 0.06f; settings.easing = DeucarianEasing.Linear;
            binding = new TransformVisibilityBinding(TweenRuntime.Scheduler, root, root.transform, settings, settings);
        }
        [UnityTest] public IEnumerator HideCompletesOnIndependentRunnerWhenOwnerComponentDisabled()
        {
            CreateTarget();
            var owner = root.AddComponent<TweenLifetimeOwner>();
            owner.enabled = false;
            binding.SetVisible(false);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(root.activeSelf, Is.False);
            Assert.That(TweenRuntime.ActiveCount, Is.Zero);
        }
        [UnityTest] public IEnumerator UnscaledAnimationCompletesWhenGameTimeIsPaused()
        {
            CreateTarget(); Time.timeScale = 0;
            binding.SetVisible(false);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(root.activeSelf, Is.False);
            Assert.That(TweenRuntime.ActiveCount, Is.Zero);
        }
        [UnityTest] public IEnumerator DisabledParentCancelsAndNeverReturnsChildToPool()
        {
            CreateTarget();
            var child = new GameObject("Child"); child.transform.SetParent(root.transform);
            using var childBinding = TweenVisibilityBindings.Create(TweenRuntime.Scheduler, child);
            int returned = 0;
            childBinding.HideAndRelease(_ => returned++);
            root.SetActive(false);
            yield return null; yield return null;
            Assert.That(returned, Is.Zero);
            Assert.That(childBinding.IsAnimating, Is.False);
            Assert.That(child.transform.localScale, Is.EqualTo(Vector3.one));
        }
        [UnityTest] public IEnumerator DestroyIsDeferredUntilExitCompletes()
        {
            CreateTarget(); binding.HideAndDestroy();
            Assert.That(root != null, Is.True);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(root == null, Is.True);
            Assert.That(TweenRuntime.ActiveCount, Is.Zero);
        }
        [UnityTest] public IEnumerator SceneUnloadReleasesTargetsButRunnerCanAnimateNewScene()
        {
            CreateTarget();
            Scene scene = SceneManager.CreateScene("Tween temporary scene");
            SceneManager.MoveGameObjectToScene(root, scene);
            binding.SetVisible(false);
            yield return SceneManager.UnloadSceneAsync(scene);
            yield return null;
            Assert.That(TweenRuntime.ActiveCount, Is.Zero);
            binding.Dispose();
            CreateTarget(); binding.SetVisible(false);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(root.activeSelf, Is.False);
        }
        [UnityTest] public IEnumerator AuthoredComponentReusesBindingAndRestoresScaleAcrossEnableCycles()
        {
            root = new GameObject("Authored target"); root.SetActive(false);
            root.transform.localScale = new Vector3(2, 3, 4);
            var authored = root.AddComponent<TweenedVisibility>();
            root.SetActive(true);
            binding = authored.GetBinding();
            yield return new WaitForSecondsRealtime(0.4f);
            authored.Hide();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(root.activeSelf, Is.False);
            authored.Show();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(authored.GetBinding(), Is.SameAs(binding));
            Assert.That(root.transform.localScale, Is.EqualTo(new Vector3(2, 3, 4)));
            Assert.That(TweenRuntime.ActiveCount, Is.Zero);
        }
    }
}
