using System;
using System.Linq;

namespace SimPhys.Entities
{
    public class Rectangle : Entity
    {
        public decimal Width { get; set; } = 1;
        public decimal Height { get; set; } = 1;
        public decimal Rotation { get; set; } // Angle in radians

        public override bool Intersects(Entity other, out CollisionData collisionData)
        {
            collisionData = null;

            var intersects = other switch
            {
                Circle otherCircle => Intersects(otherCircle, out collisionData),
                Rectangle otherRectangle => Intersects(otherRectangle, out collisionData),
                _ => false
            };

            if (intersects)
            {
                currentStepCollisions.Add(other);
                other.currentStepCollisions.Add(this);
            }
            else if (enteredCollisions.Contains(other))
            {
                OnCollisionExit?.Invoke(other);
                enteredCollisions.Remove(other);
            }

            return intersects;
        }

        public bool Intersects(Circle other, out CollisionData collisionData)
        {
            return other.Intersects(this, out collisionData);
        }

        public bool Intersects(Rectangle other, out CollisionData collisionData)
        {
            collisionData = null;
            Vector2 relativeVelocity = this.Velocity - other.Velocity;

            decimal firstCollisionTime = 0;
            decimal lastSeparationTime = 1;
            Vector2 collisionNormal = Vector2.Zero;

            // Get all axes to test from both rectangles
            Vector2[] axes = GetAxes().Concat(other.GetAxes()).ToArray();

            foreach (var axis in axes)
            {
                decimal v_proj = Vector2.Dot(relativeVelocity, axis);
                Projection p1 = Project(this, axis);
                Projection p2 = Project(other, axis);

                // Calculate distances between the intervals on the current axis
                decimal distMin = p2.Min - p1.Max; // Distance from p1's right to p2's left
                decimal distMax = p1.Min - p2.Max; // Distance from p2's right to p1's left

                // If they are already overlapping on this axis and not moving apart, they are colliding
                if (distMin < 0 && distMax < 0)
                {
                    // Static overlap case. If velocity is separating them, we might find a later collision time.
                    // But if it's not, we treat it as an immediate collision.
                    if (v_proj.NearlyEqual(0)) continue; // Can't separate on this axis, check others.
                }

                // Handle parallel movement: if separated and not moving towards each other, no collision is possible
                if (v_proj.NearlyEqual(0))
                {
                    if (distMin > 0 || distMax > 0) return false; // Separated and parallel
                    continue;
                }

                // Calculate time of entry and exit
                decimal tEnter = distMin / v_proj;
                decimal tExit = distMax / v_proj;

                // Ensure tEnter is the earlier time
                if (tEnter > tExit)
                {
                    var temp = tEnter;
                    tEnter = tExit;
                    tExit = temp;
                }

                // Update the overall collision interval
                if (tEnter > firstCollisionTime)
                {
                    firstCollisionTime = tEnter;
                    collisionNormal = axis;
                }

                if (tExit < lastSeparationTime)
                {
                    lastSeparationTime = tExit;
                }

                // If the collision interval is invalid, there is a separating axis
                if (firstCollisionTime > lastSeparationTime)
                {
                    return false;
                }
            }

            // If collision occurs within the time frame [0, 1]
            if (firstCollisionTime >= 0 && firstCollisionTime < 1)
            {
                // If the collision time is effectively zero, it's a static overlap.
                // We need to calculate penetration depth for resolution.
                if (firstCollisionTime < Epsilon)
                {
                    return IntersectsStatically(other, out collisionData);
                }

                // Otherwise, it's a continuous collision.
                Vector2 direction = other.Position - this.Position;
                if (Vector2.Dot(direction, collisionNormal) < 0)
                {
                    collisionNormal = -collisionNormal;
                }

                collisionData = new CollisionData
                {
                    Normal = collisionNormal.Normalized(),
                    Time = firstCollisionTime,
                    PenetrationDepth = 0 // No penetration yet in a continuous collision
                };
                return true;
            }

            return false;
        }

        // Helper method containing the original static SAT logic for overlapping objects.
        private bool IntersectsStatically(Rectangle other, out CollisionData collisionData)
        {
            collisionData = null;
            decimal minOverlap = decimal.MaxValue;
            Vector2 mtvAxis = Vector2.Zero;

            Vector2[] axes = GetAxes().Concat(other.GetAxes()).ToArray();

            foreach (var axis in axes)
            {
                Projection p1 = Project(this, axis);
                Projection p2 = Project(other, axis);

                decimal overlap = p1.GetOverlap(p2);
                if (overlap <= 0) return false;

                if (overlap < minOverlap)
                {
                    minOverlap = overlap;
                    mtvAxis = axis;
                }
            }

            Vector2 direction = other.Position - Position;
            if (Vector2.Dot(direction, mtvAxis) < 0) mtvAxis = -mtvAxis;

            collisionData = new CollisionData
            {
                Normal = mtvAxis.Normalized(),
                PenetrationDepth = minOverlap,
                Time = 0
            };

            return true;
        }

        #region Helper Methods for SAT
        
        // Represents the projection of a shape onto an axis
        private struct Projection
        {
            public readonly decimal Min;
            public readonly decimal Max;

            public Projection(decimal min, decimal max)
            {
                Min = min;
                Max = max;
            }

            public bool Overlaps(Projection other) => Max > other.Min && Min < other.Max;
            public decimal GetOverlap(Projection other) => Math.Min(Max, other.Max) - Math.Max(Min, other.Min);
        }

        // Projects the vertices of a rectangle onto a given axis
        private static Projection Project(Rectangle rect, Vector2 axis)
        {
            decimal min = decimal.MaxValue;
            decimal max = decimal.MinValue;
            foreach (var vertex in rect.GetVertices())
            {
                decimal dot = Vector2.Dot(vertex, axis);
                min = Math.Min(min, dot);
                max = Math.Max(max, dot);
            }
            return new Projection(min, max);
        }

        // Calculates the four vertices of the rotated rectangle in world space
        public Vector2[] GetVertices()
        {
            var vertices = new Vector2[4];
            var halfSize = new Vector2(Width / 2, Height / 2);
            var cosR = (decimal)Math.Cos((double)Rotation);
            var sinR = (decimal)Math.Sin((double)Rotation);

            vertices[0] = new Vector2(cosR * halfSize.X - sinR * halfSize.Y, sinR * halfSize.X + cosR * halfSize.Y); // Top-Right
            vertices[1] = new Vector2(cosR * -halfSize.X - sinR * halfSize.Y, sinR * -halfSize.X + cosR * halfSize.Y); // Top-Left
            vertices[2] = new Vector2(cosR * -halfSize.X - sinR * -halfSize.Y, sinR * -halfSize.X + cosR * -halfSize.Y); // Bottom-Left
            vertices[3] = new Vector2(cosR * halfSize.X - sinR * -halfSize.Y, sinR * halfSize.X + cosR * -halfSize.Y); // Bottom-Right

            for (int i = 0; i < 4; i++)
            {
                vertices[i] += Position;
            }
            return vertices;
        }

        // Gets the two unique axes of the rectangle, perpendicular to its sides
        private Vector2[] GetAxes()
        {
            var cosR = (decimal)Math.Cos((double)Rotation);
            var sinR = (decimal)Math.Sin((double)Rotation);
            return new[]
            {
                new Vector2(cosR, sinR),        // Rotated X-axis
                new Vector2(-sinR, cosR)        // Rotated Y-axis
            };
        }
        
        #endregion

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
            if (IsFrozen && other.IsFrozen) return;
            
            // Save original state
            Vector2 originalPos = Position;
            Vector2 originalOtherPos = other.Position;

            // Freeze System
            var invMass = IsFrozen ? 0 : InverseMass;
            var otherInvMass = other.IsFrozen ? 0 : other.InverseMass;
            
            //Vector2 originalVel = Velocity;
            //Vector2 originalOtherVel = other.Velocity;

            // Move to collision time
            Position += Velocity * collisionData.Time;
            other.Position += other.Velocity * collisionData.Time;

            // Calculate relative velocity
            Vector2 relativeVelocity = Velocity - other.Velocity;
            decimal velAlongNormal = Vector2.Dot(relativeVelocity, collisionData.Normal);

            // Only resolve if moving towards each other
            if (velAlongNormal > 0)
            {
                Position = originalPos;
                other.Position = originalOtherPos;
                return;
            }

            // Calculate impulse
            decimal e = Math.Min(Bounciness, other.Bounciness);
            decimal j = -(1 + e) * velAlongNormal;
            j /= (invMass + otherInvMass);
            Vector2 impulse = j * collisionData.Normal;

            // Apply impulse
            Velocity += impulse * invMass;
            other.Velocity -= impulse * otherInvMass;

            // Position correction for static collisions
            if (collisionData.PenetrationDepth > 0)
            {
                const decimal percent = 0.8m;
                const decimal slop = 0.01m;
                decimal correctionMag = (Math.Max(collisionData.PenetrationDepth - slop, 0) / 
                                       (invMass + otherInvMass)) * percent;
                Vector2 correction = collisionData.Normal * correctionMag;
            
                Position += correction * invMass;
                other.Position -= correction * otherInvMass;
            }

            // Apply remaining movement
            decimal remainingTime = 1 - collisionData.Time;
            Position += Velocity * remainingTime;
            other.Position += other.Velocity * remainingTime;
            
            // Freeze system
            if (IsFrozen)
            {
                Position = originalPos;
                Velocity = Vector2.Zero;
            }
            if (other.IsFrozen)
            {
                other.Position = originalOtherPos;
                other.Velocity = Vector2.Zero;
            }
        }
    }
}