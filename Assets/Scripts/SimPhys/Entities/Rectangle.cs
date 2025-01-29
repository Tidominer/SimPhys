using System;
using System.Linq;
using System.Numerics;

namespace SimPhys.Entities
{
    public class Rectangle : Entity
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public override bool Intersects(Entity other, out CollisionData collisionData)
        {
            collisionData = null;

            if (other is Circle otherCircle)
            {
                return Intersects(otherCircle, out collisionData);
            }

            if (other is Rectangle otherRectangle)
            {
                return Intersects(otherRectangle, out collisionData);
            }

            return false;
        }

        public bool Intersects(Circle other, out CollisionData collisionData)
        {
            return other.Intersects(this, out collisionData);
        }

        public bool Intersects(Rectangle other, out CollisionData collisionData)
        {
            collisionData = null;
            
            Vector2 relativeVelocity = Velocity - other.Velocity;
            Vector2 thisCenter = Position;
            Vector2 otherCenter = other.Position;

            // Calculate half dimensions
            float hwA = Width / 2;
            float hhA = Height / 2;
            float hwB = other.Width / 2;
            float hhB = other.Height / 2;

            // Current bounding coordinates
            float leftA = thisCenter.X - hwA;
            float rightA = thisCenter.X + hwA;
            float topA = thisCenter.Y - hhA;
            float bottomA = thisCenter.Y + hhA;

            float leftB = otherCenter.X - hwB;
            float rightB = otherCenter.X + hwB;
            float topB = otherCenter.Y - hhB;
            float bottomB = otherCenter.Y + hhB;

            // Static collision check
            bool overlapX = rightA > leftB && leftA < rightB;
            bool overlapY = bottomA > topB && topA < bottomB;

            if (overlapX && overlapY)
            {
                // Calculate minimum translation vector
                float[] overlaps =
                {
                    rightA - leftB, // Left overlap
                    rightB - leftA, // Right overlap
                    bottomA - topB, // Top overlap
                    bottomB - topA // Bottom overlap
                };

                float minOverlap = overlaps.Min();
                int index = Array.IndexOf(overlaps, minOverlap);

                Vector2 normal2 = index switch
                {
                    0 => new Vector2(-1, 0), // Left
                    1 => new Vector2(1, 0), // Right
                    2 => new Vector2(0, -1), // Top
                    3 => new Vector2(0, 1), // Bottom
                    _ => Vector2.Zero
                };

                collisionData = new CollisionData
                {
                    Normal = normal2,
                    Time = 0f,
                    PenetrationDepth = minOverlap
                };
                return true;
            }

            // Continuous collision detection
            if (relativeVelocity == Vector2.Zero) return false;

            // Calculate time of impact for each axis
            (float entry, float exit) CalculateAxisTimes(float aMin, float aMax, float bMin, float bMax, float velocity)
            {
                if (velocity > 0)
                    return ((bMin - aMax) / velocity, (bMax - aMin) / velocity);
                if (velocity < 0)
                    return ((bMax - aMin) / velocity, (bMin - aMax) / velocity);
                return (float.NegativeInfinity, float.PositiveInfinity);
            }

            var xTimes = CalculateAxisTimes(leftA, rightA, leftB, rightB, relativeVelocity.X);
            var yTimes = CalculateAxisTimes(topA, bottomA, topB, bottomB, relativeVelocity.Y);

            float tEntry = Math.Max(xTimes.entry, yTimes.entry);
            float tExit = Math.Min(xTimes.exit, yTimes.exit);

            if (tEntry > tExit || tEntry < 0 || tEntry > 1) return false;

            // Determine collision normal
            Vector2 normal;
            if (xTimes.entry > yTimes.entry)
            {
                normal = relativeVelocity.X > 0
                    ? new Vector2(-1, 0)
                    : new Vector2(1, 0);
            }
            else
            {
                normal = relativeVelocity.Y > 0
                    ? new Vector2(0, -1)
                    : new Vector2(0, 1);
            }

            collisionData = new CollisionData
            {
                Normal = normal,
                Time = tEntry,
                PenetrationDepth = 0 // Continuous collision just touches
            };
            return true;
        }

        public override void ResolveCollision(Entity other, CollisionData collisionData)
        {
            if (other is Circle otherCircle)
            {
                ResolveCollision(otherCircle, collisionData);
            }
            if (other is Rectangle otherRectangle)
            {
                ResolveCollision(otherRectangle, collisionData);
            }
        }

        public void ResolveCollision(Circle other, CollisionData collisionData)
        {
            other.ResolveCollision(this, collisionData);
        }

        public void ResolveCollision(Rectangle other, CollisionData collisionData)
        {
            // Save original state
            Vector2 originalPos = Position;
            Vector2 originalOtherPos = other.Position;
            //Vector2 originalVel = Velocity;
            //Vector2 originalOtherVel = other.Velocity;

            // Move to collision time
            Position += Velocity * collisionData.Time;
            other.Position += other.Velocity * collisionData.Time;

            // Calculate relative velocity
            Vector2 relativeVelocity = Velocity - other.Velocity;
            float velAlongNormal = Vector2.Dot(relativeVelocity, collisionData.Normal);

            // Only resolve if moving towards each other
            if (velAlongNormal > 0)
            {
                Position = originalPos;
                other.Position = originalOtherPos;
                return;
            }

            // Calculate impulse
            float e = Math.Min(Bounciness, other.Bounciness);
            float j = -(1 + e) * velAlongNormal;
            j /= (InverseMass + other.InverseMass);
            Vector2 impulse = j * collisionData.Normal;

            // Apply impulse
            Velocity += impulse * InverseMass;
            other.Velocity -= impulse * other.InverseMass;

            // Position correction for static collisions
            if (collisionData.PenetrationDepth > 0)
            {
                const float percent = 0.8f;
                const float slop = 0.01f;
                float correctionMag = (Math.Max(collisionData.PenetrationDepth - slop, 0) / 
                                       (InverseMass + other.InverseMass)) * percent;
                Vector2 correction = collisionData.Normal * correctionMag;
            
                Position += correction * InverseMass;
                other.Position -= correction * other.InverseMass;
            }

            // Apply remaining movement
            float remainingTime = 1 - collisionData.Time;
            Position += Velocity * remainingTime;
            other.Position += other.Velocity * remainingTime;
        }
    }
}