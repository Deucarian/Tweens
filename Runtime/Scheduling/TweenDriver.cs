using UnityEngine;

namespace Deucarian.Tweens
{
    [AddComponentMenu("")]
    internal sealed class TweenDriver : MonoBehaviour
    {
        internal TweenScheduler Scheduler { get; private set; }
        internal void Initialize()
        {
            Scheduler = new TweenScheduler();
            Scheduler.WorkAvailableChanged += SetWorking;
            enabled = false;
        }
        private void SetWorking(bool available) { enabled = available; }
        private void Update() { Scheduler.Advance(Time.deltaTime, Time.unscaledDeltaTime); }
        private void OnApplicationQuit() { TweenRuntime.Quitting(); Shutdown(); }
        private void OnDestroy() { Shutdown(); TweenRuntime.Released(this); }
        internal void Shutdown()
        {
            if (Scheduler == null) return;
            Scheduler.WorkAvailableChanged -= SetWorking;
            Scheduler.Dispose();
            enabled = false;
        }
    }
}
