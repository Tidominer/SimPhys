using System;
using SimPhys.Entities;

namespace SimPhys
{
    public class CollisionManifold
    {
        public Entity EntityA { get; set; }
        public Entity EntityB { get; set; }
        public Vector2 Normal { get; set; }
        public float Depth { get; set; }
        public float Time { get; set; }
    }
    
    public class Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 v, float s) => new Vector2(v.X * s, v.Y * s);
        public static Vector2 operator *(float s, Vector2 v) => v * s;
        public static Vector2 operator *(Vector2 a, Vector2 b) => new Vector2(a.X * b.X, a.Y * b.Y);

        public static Vector2 Min(Vector2 a, Vector2 b) => 
            new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));

        public static Vector2 Max(Vector2 a, Vector2 b) => 
            new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

        public float LengthSquared() => X * X + Y * Y;
        public float Length() => (float)Math.Sqrt(LengthSquared());
        public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;

        public Vector2 Normalized()
        {
            float length = Length();
            return length > float.Epsilon ? this * (1f / length) : Vector2.Zero;
        }
        
        public static Vector2 Zero { get; } = new Vector2(0, 0);

        public override string ToString() => $"({X:0.00}, {Y:0.00})";
    }
    
    public static class VectorExtensions
    {
        public static float GetAxis(this Vector2 v, int axis) => axis == 0 ? v.X : v.Y;
    }
}