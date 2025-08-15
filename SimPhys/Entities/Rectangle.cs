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
            decimal minOverlap = decimal.MaxValue;
            Vector2 mtvAxis = Vector2.Zero;

            // Get all unique axes from both rectangles
            Vector2[] axes = GetAxes().Concat(other.GetAxes()).ToArray();

            foreach (var axis in axes)
            {
                Projection p1 = Project(this, axis);
                Projection p2 = Project(other, axis);

                if (!p1.Overlaps(p2))
                {
                    // Found a separating axis, no collision
                    return false;
                }
                else
                {
                    // An overlap was found. Check if it is the minimum overlap so far.
                    decimal overlap = p1.GetOverlap(p2);
                    if (overlap < minOverlap)
                    {
                        minOverlap = overlap;
                        mtvAxis = axis;
                    }
                }
            }
            
            // Ensure the collision normal is pointing away from the first rectangle
            Vector2 direction = other.Position - Position;
            if (Vector2.Dot(direction, mtvAxis) < 0)
            {
                mtvAxis = -mtvAxis;
            }

            collisionData = new CollisionData
            {
                Normal = mtvAxis.Normalized(),
                PenetrationDepth = minOverlap,
                Time = 0 // SAT is a static test; time of impact is considered immediate
            };

            return true;
        }

        public override void ForceResolveCollision(Entity other, CollisionData collisionData)
        {
            if (other is Circle otherCircle)
            {
                ForceResolveCollision(otherCircle, collisionData);
            }
            if (other is Rectangle otherRectangle)
            {
                ForceResolveCollision(otherRectangle, collisionData);
            }
        }

        public void ForceResolveCollision(Rectangle other, CollisionData collisionData)
        {
            if (IsTrigger || other.IsTrigger) return;
            if (IsFrozen && other.IsFrozen) return;

            decimal invMass = IsFrozen ? 0 : InverseMass;
            decimal otherInvMass = other.IsFrozen ? 0 : other.InverseMass;

            decimal totalInverseMass = invMass + otherInvMass;
            if (totalInverseMass.NearlyEqual(0)) return;

            // The Minimum Translation Vector (MTV) is the smallest push to separate the objects
            Vector2 mtv = collisionData.Normal * collisionData.PenetrationDepth;

            // Ensure the MTV is pushing the objects apart
            Vector2 relativePos = Position - other.Position;
            if (Vector2.Dot(relativePos, mtv) < 0)
            {
                mtv = -mtv;
            }
            
            // Move the entities apart based on their inverse mass
            Position += mtv * (invMass / totalInverseMass);
            other.Position -= mtv * (otherInvMass / totalInverseMass);
        }
        
        public void ForceResolveCollision(Circle circle, CollisionData collisionData)
        {
            circle.ForceResolveCollision(this, collisionData);
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

        // Unchanged methods are omitted for brevity...
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
        
        public override void ResolveBorderCollision(decimal minX, decimal maxX, decimal minY, decimal maxY)
        {
            // Note: This border collision method is for AABB and will not work correctly for rotated rectangles.
            // A more advanced solution using the rectangle's vertices would be required for accurate border collision.
            
            // Handle collision on the X axis
            if (Position.X - (decimal)(Width / 2) < minX) // Colliding with the left wall
            {
                Position = new Vector2(minX + (decimal)(Width / 2), Position.Y);
                Velocity = new Vector2(Math.Abs(Velocity.X) * Bounciness, Velocity.Y);
            }
            else if (Position.X + (decimal)(Width / 2) > maxX) // Colliding with the right wall
            {
                Position = new Vector2(maxX - (decimal)(Width / 2), Position.Y);
                Velocity = new Vector2(-Math.Abs(Velocity.X) * Bounciness, Velocity.Y);
            }

            // Handle collision on the Y axis
            if (Position.Y - (decimal)(Height / 2) < minY) // Colliding with the bottom wall
            {
                Position = new Vector2(Position.X, minY + (decimal)(Height / 2));
                Velocity = new Vector2(Velocity.X, Math.Abs(Velocity.Y) * Bounciness);
            }
            else if (Position.Y + (decimal)(Height / 2) > maxY) // Colliding with the top wall
            {
                Position = new Vector2(Position.X, maxY - (decimal)(Height / 2));
                Velocity = new Vector2(Velocity.X, -Math.Abs(Velocity.Y) * Bounciness);
            }
        }
    }
}