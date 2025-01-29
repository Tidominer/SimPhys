using System.Numerics;

namespace SimPhys
{
    public class CollisionData
    {
        public Vector2 Normal { get; set; } // Collision normal (direction)
        public float Time { get; set; }     // Time of impact (0 for current overlap)
        public float PenetrationDepth { get; set; } // Depth of penetration for static collisions
    }
}