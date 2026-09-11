using System;
using Deucarian.Common;
using UnityEngine;

namespace Deucarian.Tweens
{
    public static class TweenRuntime
    {
        private static TweenDriver driver;
        private static bool quitting;
        public static bool IsCreated => driver != null;
        public static int ActiveCount => driver == null ? 0 : driver.Scheduler.ActiveCount;

        public static TweenScheduler Scheduler
        {
            get
            {
                if (!Application.isPlaying || quitting)
                    throw new InvalidOperationException("Runtime tweens require a running player. Use an explicit scheduler for editor previews.");
                if (driver == null)
                {
                    var root = new GameObject("[Deucarian Tweens]");
                    UnityEngine.Object.DontDestroyOnLoad(root);
                    driver = root.AddComponent<TweenDriver>();
                    driver.Initialize();
                }
                return driver.Scheduler;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if (driver != null) { driver.Shutdown(); UnityObjectUtility.DestroySafely(driver.gameObject); }
            driver = null;
            quitting = false;
            TweenVisibilityDefaults.Reset();
        }

        internal static void Quitting() { quitting = true; }
        internal static void Released(TweenDriver value) { if (driver == value) driver = null; }
    }
}
