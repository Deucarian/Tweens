using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.Tweens.Tests
{
    public sealed class TweenPlaySessionTests
    {
        private bool configured;
        private bool previousEnabled;
        private EnterPlayModeOptions previousOptions;

        [UnityTearDown] public IEnumerator RestoreEditorOptions()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (configured)
            {
                EditorSettings.enterPlayModeOptionsEnabled = previousEnabled;
                EditorSettings.enterPlayModeOptions = previousOptions;
                configured = false;
            }
        }

        [UnityTest] public IEnumerator NewSessionResetsDriverAndPreferencesWithoutDomainReload()
        {
            previousEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousOptions = EditorSettings.enterPlayModeOptions;
            configured = true;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            yield return new EnterPlayMode();
            TweenScheduler first = TweenRuntime.Scheduler;
            TweenVisibilityDefaults.ReducedMotion = true;
            yield return new ExitPlayMode();
            Assert.That(first.IsDisposed, Is.True);
            yield return new EnterPlayMode();
            Assert.That(TweenVisibilityDefaults.ReducedMotion, Is.False);
            Assert.That(TweenRuntime.Scheduler, Is.Not.SameAs(first));
            Assert.That(TweenRuntime.ActiveCount, Is.Zero);
            yield return new ExitPlayMode();
        }
    }
}
