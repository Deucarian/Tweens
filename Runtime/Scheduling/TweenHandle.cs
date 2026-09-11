using System;

namespace Deucarian.Tweens
{
    /// <summary>Generation-checked handle; an old handle can never cancel a recycled slot.</summary>
    public readonly struct TweenHandle : IEquatable<TweenHandle>
    {
        internal readonly TweenScheduler Owner;
        internal readonly int Slot;
        internal readonly ulong Generation;
        internal TweenHandle(TweenScheduler owner, int slot, ulong generation)
        { Owner = owner; Slot = slot; Generation = generation; }

        public bool IsActive => Owner != null && Owner.Contains(this);
        public void Cancel() => Owner?.Cancel(this);
        public bool Equals(TweenHandle other) => ReferenceEquals(Owner, other.Owner) &&
            Slot == other.Slot && Generation == other.Generation;
        public override bool Equals(object obj) => obj is TweenHandle other && Equals(other);
        public override int GetHashCode() => (Owner == null ? 0 : Owner.GetHashCode()) ^ Slot ^ Generation.GetHashCode();
        public static bool operator ==(TweenHandle left, TweenHandle right) => left.Equals(right);
        public static bool operator !=(TweenHandle left, TweenHandle right) => !left.Equals(right);
    }
}
