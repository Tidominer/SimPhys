using System;
using System.Numerics;
using SimPhys.Entities;

namespace SimPhys
{
    public static class CollisionResolver
    {
        public static void Resolve(CollisionManifold manifold)
        {
            Entity a = manifold.EntityA;
            Entity b = manifold.EntityB;
            Vector2 normal = manifold.Normal;

            Vector2 relVelocity = b.Velocity - a.Velocity;
            float velAlongNormal = Vector2.Dot(relVelocity, normal);
            if (velAlongNormal > 0) return;

            float e = Math.Min(a.Bounciness, b.Bounciness);
            float j = -(1 + e) * velAlongNormal;
            j /= a.InverseMass + b.InverseMass;

            Vector2 impulse = j * normal;
            a.Velocity -= a.InverseMass * impulse;
            b.Velocity += b.InverseMass * impulse;
        }
    }
}