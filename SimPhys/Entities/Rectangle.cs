using System;
using System.Linq;

namespace SimPhys.Entities
{
    public class Rectangle : Entity
    {
        public decimal Width { get; set; }
        public decimal Height { get; set; }

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
            
            Vector2 relativeVelocity = Velocity - other.Velocity;
            Vector2 thisCenter = Position;
            Vector2 otherCenter = other.Position;

            // Calculate half dimensions
            decimal hwA = Width / 2;
            decimal hhA = Height / 2;
            decimal hwB = other.Width / 2;
            decimal hhB = other.Height / 2;

            // Current bounding coordinates
            decimal leftA = thisCenter.X - hwA;
            decimal rightA = thisCenter.X + hwA;
            decimal topA = thisCenter.Y - hhA;
            decimal bottomA = thisCenter.Y + hhA;

            decimal leftB = otherCenter.X - hwB;
            decimal rightB = otherCenter.X + hwB;
            decimal topB = otherCenter.Y - hhB;
            decimal bottomB = otherCenter.Y + hhB;

            // Static collision check
            bool overlapX = rightA > leftB && leftA < rightB;
            bool overlapY = bottomA > topB && topA < bottomB;

            if (overlapX && overlapY)
            {
                // Calculate minimum translation vector
                decimal[] overlaps =
                {
                    rightA - leftB, // Left overlap
                    rightB - leftA, // Right overlap
                    bottomA - topB, // Top overlap
                    bottomB - topA // Bottom overlap
                };

                decimal minOverlap = overlaps.Min();
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
                    Time = 0m,
                    PenetrationDepth = minOverlap
                };
                return true;
            }

            // Continuous collision detection
            if (relativeVelocity.Length().NearlyEqual(0)) return false;

            // Calculate time of impact for each axis
            (decimal entry, decimal exit) CalculateAxisTimes(decimal aMin, decimal aMax, decimal bMin, decimal bMax, decimal velocity)
            {
                if (velocity > 0)
                    return ((bMin - aMax) / velocity, (bMax - aMin) / velocity);
                if (velocity < 0)
                    return ((bMax - aMin) / velocity, (bMin - aMax) / velocity);
                return (-99999, 99999);
            }

            var xTimes = CalculateAxisTimes(leftA, rightA, leftB, rightB, relativeVelocity.X);
            var yTimes = CalculateAxisTimes(topA, bottomA, topB, bottomB, relativeVelocity.Y);

            decimal tEntry = Math.Max(xTimes.entry, yTimes.entry);
            decimal tExit = Math.Min(xTimes.exit, yTimes.exit);

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
            // Exit if either entity is a trigger (no physical resolution needed)
            if (IsTrigger || other.IsTrigger) return;
            if (IsFrozen && other.IsFrozen) return;

            // Calculate the overlap along each axis
            decimal deltaX = Math.Abs(Position.X - other.Position.X);
            decimal deltaY = Math.Abs(Position.Y - other.Position.Y);

            decimal halfWidthThis = Width / 2;
            decimal halfHeightThis = Height / 2;
            decimal halfWidthOther = other.Width / 2;
            decimal halfHeightOther = other.Height / 2;

            decimal overlapX = (halfWidthThis + halfWidthOther) - deltaX;
            decimal overlapY = (halfHeightThis + halfHeightOther) - deltaY;

            // If there's no overlap, exit
            if (overlapX <= 0 || overlapY <= 0) return;

            // Determine the axis with the smallest overlap (MTV direction)
            Vector2 separation;
            if (overlapX < overlapY)
            {
                // Resolve along the X-axis
                separation = new Vector2(overlapX * Math.Sign(Position.X - other.Position.X), 0);
            }
            else
            {
                // Resolve along the Y-axis
                separation = new Vector2(0, overlapY * Math.Sign(Position.Y - other.Position.Y));
            }

            // Handle frozen/static entities
            decimal invMass = IsFrozen ? 0 : InverseMass;
            decimal otherInvMass = other.IsFrozen ? 0 : other.InverseMass;

            // Avoid division by zero
            decimal totalInverseMass = invMass + otherInvMass;
            if (totalInverseMass == 0) return;

            // Calculate positional adjustments
            Vector2 thisAdjustment = separation * invMass / totalInverseMass;
            Vector2 otherAdjustment = -separation * otherInvMass / totalInverseMass;

            // Apply adjustments to positions
            Position += thisAdjustment;
            other.Position += otherAdjustment;
        }
        
        public void ForceResolveCollision(Circle circle, CollisionData collisionData)
        {
            circle.ForceResolveCollision(this, collisionData);
        }


        public override void ResolveBorderCollision(decimal minX, decimal maxX, decimal minY, decimal maxY)
        {
            // Check collision with each boundary and resolve accordingly

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