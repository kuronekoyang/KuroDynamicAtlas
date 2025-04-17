using System;
using System.Runtime.CompilerServices;

namespace kuro
{
    [Serializable]
    public struct SpriteId : IEquatable<SpriteId>
    {
        public string Name;

        public SpriteId(string name)
        {
            Name = name;
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => string.IsNullOrEmpty(Name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(SpriteId other)
        {
            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is SpriteId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(SpriteId left, SpriteId right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(SpriteId left, SpriteId right) => !left.Equals(right);

        public override string ToString()
        {
            return Name;
        }
    }
}