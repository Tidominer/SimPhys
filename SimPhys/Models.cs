namespace SimPhys
{
    public class CollisionData
    {
        public Vector2 Normal { get; set; } // Collision normal (direction)
        public decimal Time { get; set; }     // Time of impact (0 for current overlap)
        public decimal PenetrationDepth { get; set; } // Depth of penetration for static collisions
    }

    public static class Extensions
    {
        public static UnityEngine.Vector2 ToUnityVector(this Vector2 vector2) => new UnityEngine.Vector2((float)vector2.X, (float)vector2.Y);
        public static UnityEngine.Vector2 ToUnityVector(this System.Numerics.Vector2 vector2) => new UnityEngine.Vector2(vector2.X, vector2.Y);
        public static Vector2 ToSimPhysVector(this UnityEngine.Vector2 vector2) => new Vector2((decimal)vector2.x, (decimal)vector2.y);
        public static Vector2 ToSimPhysVector(this System.Numerics.Vector2 vector2) => new Vector2((decimal)vector2.X, (decimal)vector2.Y);
        public static System.Numerics.Vector2 ToSystemVector(this UnityEngine.Vector2 vector2) => new System.Numerics.Vector2(vector2.x, vector2.y);
        
        public static decimal Sqrt(this decimal x, decimal epsilon = 0.0M)
        {
            if (x < 0) throw new System.OverflowException("Cannot calculate square root from a negative number");

            decimal current = (decimal)System.Math.Sqrt((double)x), previous;
            do
            {
                previous = current;
                if (previous == 0.0M) return 0;
                current = (previous + x / previous) / 2;
            }
            while (System.Math.Abs(previous - current) > epsilon);
            return current;
        }
        
        public static bool NearlyEqual(this decimal a, decimal b, decimal epsilon = 0.000000001m)
        {
            return System.Math.Abs(a - b) < epsilon;
        }
    }

    public struct Vector2
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }

        // Constructor
        public Vector2(decimal x, decimal y)
        {
            X = x;
            Y = y;
        }

        // Vector2.Zero and Vector2.One
        public static Vector2 Zero => new Vector2(0, 0);
        public static Vector2 One => new Vector2(1, 1);
        public static Vector2 UnitX => new Vector2(1, 0);
        public static Vector2 UnitY => new Vector2(0, 1);

        // Operators

        // Addition
        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        // Subtraction
        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }
        
        // Negative
        public static Vector2 operator -(Vector2 vector)
        {
            return new Vector2(-vector.X, -vector.Y);
        }

        // Scalar Multiplication (decimal)
        public static Vector2 operator *(Vector2 a, decimal scalar)
        {
            return new Vector2(a.X * scalar, a.Y * scalar);
        }

        // Scalar Multiplication (int)
        public static Vector2 operator *(Vector2 a, int scalar)
        {
            return a * (decimal)scalar;
        }

        // Scalar Multiplication (float)
        public static Vector2 operator *(Vector2 a, float scalar)
        {
            return a * (decimal)scalar;
        }

        // Scalar Division (decimal)
        public static Vector2 operator /(Vector2 a, decimal scalar)
        {
            if (scalar == 0) throw new System.DivideByZeroException("Cannot divide by zero.");
            return new Vector2(a.X / scalar, a.Y / scalar);
        }

        // Scalar Division (int)
        public static Vector2 operator /(Vector2 a, int scalar)
        {
            return a / (decimal)scalar;
        }

        // Scalar Division (float)
        public static Vector2 operator /(Vector2 a, float scalar)
        {
            return a / (decimal)scalar;
        }

        // Scalar Multiplication (decimal)
        public static Vector2 operator *(decimal scalar, Vector2 a)
        {
            return new Vector2(a.X * scalar, a.Y * scalar);
        }

// Scalar Multiplication (int)
        public static Vector2 operator *(int scalar, Vector2 a)
        {
            return new Vector2(a.X * scalar, a.Y * scalar);
        }

// Scalar Multiplication (float)
        public static Vector2 operator *(float scalar, Vector2 a)
        {
            return new Vector2(a.X * (decimal)scalar, a.Y * (decimal)scalar);
        }

// Scalar Division (decimal)
        public static Vector2 operator /(decimal scalar, Vector2 a)
        {
            if (a.X == 0 || a.Y == 0) throw new System.DivideByZeroException("Cannot divide by zero vector.");
            return new Vector2(scalar / a.X, scalar / a.Y);
        }

// Scalar Division (int)
        public static Vector2 operator /(int scalar, Vector2 a)
        {
            return (decimal)scalar / a;
        }

// Scalar Division (float)
        public static Vector2 operator /(float scalar, Vector2 a)
        {
            return (decimal)scalar / a;
        }


        // Instance Methods

        // Length (magnitude)
        public decimal Length()
        {
            return (X * X + Y * Y).Sqrt();
        }

        // LengthSquared (squared magnitude)
        public decimal LengthSquared()
        {
            return X * X + Y * Y;
        }

        // Normalized
        public Vector2 Normalized()
        {
            var length = Length();
            if (length == 0) return Zero;
            return this / length;
        }

        // Static Methods

        // Dot Product
        public static decimal Dot(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        // Normalize a Vector2
        public static Vector2 Normalize(Vector2 vector)
        {
            return vector.Normalized();
        }

        // Distance between two Vector2 points
        public static decimal Distance(Vector2 a, Vector2 b)
        {
            return (a - b).Length();
        }

        // Override ToString for easy debugging
        public override string ToString()
        {
            return $"Vector2({X}, {Y})";
        }
    }
}