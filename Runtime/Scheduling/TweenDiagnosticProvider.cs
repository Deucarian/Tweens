using System.Threading;
using Deucarian.Diagnostics;

namespace Deucarian.Tweens
{
    internal sealed class TweenDiagnosticProvider : IDiagnosticProvider
    {
        private static long nextId;
        private readonly TweenScheduler scheduler;
        public TweenDiagnosticProvider(TweenScheduler scheduler)
        { this.scheduler = scheduler; ProviderId = "tweens." + Interlocked.Increment(ref nextId); }
        public string ProviderId { get; }
        public string DisplayName => "Tweens";
        public void Collect(DiagnosticReportBuilder builder)
        {
            builder.AddSection(ProviderId, DisplayName)
                .AddItem("active", "Active tweens", scheduler.ActiveCount.ToString())
                .AddItem("peak", "Peak active tweens", scheduler.PeakActiveCount.ToString())
                .AddItem("capacity", "Allocated slots", scheduler.Capacity.ToString())
                .AddItem("updates", "Binding updates", scheduler.UpdateCount.ToString())
                .AddItem("faults", "Binding/callback failures", scheduler.FaultCount.ToString(),
                    scheduler.FaultCount == 0 ? DiagnosticSeverity.Info : DiagnosticSeverity.Error,
                    "Failures cancel the affected tween. Target names, payloads and exception messages are not collected.");
        }
    }
}
