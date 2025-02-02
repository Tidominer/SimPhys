using System.Numerics;

namespace SimPhys
{
    public class CollisionData
    {
        public Vector2 Normal { get; set; } // Collision normal (direction)
        public float Time { get; set; }     // Time of impact (0 for current overlap)
        public float PenetrationDepth { get; set; } // Depth of penetration for static collisions
    }

    public static class Extensions
    {
        public static UnityEngine.Vector2 ToUnityVector(this Vector2 vector2) => new UnityEngine.Vector2(vector2.X, vector2.Y);
        public static UnityEngine.Vector3 ToUnityVector(this Vector3 vector3) => new UnityEngine.Vector3(vector3.X, vector3.Y, vector3.Z);
        public static Vector2 ToSystemVector(this UnityEngine.Vector2 vector2) => new Vector2(vector2.x, vector2.y);
        public static Vector3 ToSystemVector(this UnityEngine.Vector3 vector3) => new Vector3(vector3.x, vector3.y, vector3.z);
    }
}