using System;

namespace ExampleProject.Framework.Utils
{
    public class Coordinate : IEquatable<Coordinate>
    {
        public int X { get; }
        public int Y { get; }

        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Coordinate? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object? obj) => Equals(obj as Coordinate);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"Coordinate{{x={X}, y={Y}}}";
    }
}
