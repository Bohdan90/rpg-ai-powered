using System;

namespace RPG.Core
{
    public readonly struct UnitId : IEquatable<UnitId>, IComparable<UnitId>
    {
        public int Value { get; }
        public UnitId(int value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
        public int CompareTo(UnitId other) => Value.CompareTo(other.Value);
        public bool Equals(UnitId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is UnitId other && Equals(other);
        public override int GetHashCode() => Value; // Collection support only; never a seed or tie key.
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static bool operator ==(UnitId left, UnitId right) => left.Equals(right);
        public static bool operator !=(UnitId left, UnitId right) => !left.Equals(right);
    }
}
