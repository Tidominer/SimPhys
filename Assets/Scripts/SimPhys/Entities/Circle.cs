using System;
using System.Numerics;

namespace SimPhys.Entities
{
    public class Circle : Entity
    {
        public float Radius { get; set; }

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
                currentStepCollisions.Add(other);
            else if (enteredCollisions.Contains(other))
            {
                OnCollisionExit?.Invoke(other);
                enteredCollisions.Remove(other);
            }

            return intersects;
        }

        public bool Intersects(Circle other, out CollisionData collisionData)
        {
            collisionData = null;
            
            Vector2 deltaPos = Position - other.Position;
            float distanceSquared = deltaPos.LengthSquared();
            float radiusSum = Radius + other.Radius;
            float radiusSumSquared = radiusSum * radiusSum;

            // Check current overlap
            if (distanceSquared <= radiusSumSquared)
            {
                float distance = (float)Math.Sqrt(distanceSquared);
                Vector2 normal = distance > 0 ? deltaPos / distance : Vector2.UnitX;
                collisionData = new CollisionData
                {
                    Normal = normal,
                    Time = 0f,
                    PenetrationDepth = radiusSum - distance
                };
                return true;
            }

            // Continuous collision detection
            Vector2 deltaVel = Velocity - other.Velocity;
            float a = deltaVel.LengthSquared();
            if (a == 0) return false; // No relative movement

            float b = 2 * Vector2.Dot(deltaPos, deltaVel);
            float c = deltaPos.LengthSquared() - radiusSumSquared;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return false;

            discriminant = (float)Math.Sqrt(discriminant);
            float t0 = (-b - discriminant) / (2 * a);
            float t1 = (-b + discriminant) / (2 * a);

            float t = -1;
            if (t0 >= 0 && t0 <= 1) t = t0;
            else if (t1 >= 0 && t1 <= 1) t = t1;

            if (t < 0 || t > 1) return false;

            // Calculate collision normal at time t
            Vector2 futurePos1 = Position + Velocity * t;
            Vector2 futurePos2 = other.Position + other.Velocity * t;
            Vector2 futureDelta = futurePos1 - futurePos2;
            float futureDistance = futureDelta.Length();
            Vector2 normal2 = futureDistance > 0 ? futureDelta / futureDistance : Vector2.UnitX;

            collisionData = new CollisionData
            {
                Normal = normal2,
                Time = t,
                PenetrationDepth = 0 // Just touching in continuous case
            };
            return true;
        }
        public bool Intersects(Rectangle other, out CollisionData collisionData)
        {
            collisionData = null;

            Vector2 relativeVelocity = Velocity - other.Velocity;
            Vector2 circleStart = Position;
            //Vector2 circleEnd = circleStart + relativeVelocity;
            float radius = Radius;

            float expandedLeft = other.Position.X - other.Width / 2 - radius;
            float expandedRight = other.Position.X + other.Width / 2 + radius;
            float expandedTop = other.Position.Y - other.Height / 2 - radius;
            float expandedBottom = other.Position.Y + other.Height / 2 + radius;

            float tEntry = 0f;
            float tExit = 1f;
            Vector2 normal = Vector2.Zero;

            if (Math.Abs(relativeVelocity.X) > float.Epsilon)
            {
                float t1 = (expandedLeft - circleStart.X) / relativeVelocity.X;
                float t2 = (expandedRight - circleStart.X) / relativeVelocity.X;
                if (t1 > t2) (t1, t2) = (t2, t1);

                tEntry = Math.Max(tEntry, t1);
                tExit = Math.Min(tExit, t2);

                normal.X = (t1 < t2) ? -1 : 1;
            }
            else if (circleStart.X < expandedLeft || circleStart.X > expandedRight)
                return false;

            if (Math.Abs(relativeVelocity.Y) > float.Epsilon)
            {
                float t1 = (expandedTop - circleStart.Y) / relativeVelocity.Y;
                float t2 = (expandedBottom - circleStart.Y) / relativeVelocity.Y;
                if (t1 > t2) (t1, t2) = (t2, t1);

                tEntry = Math.Max(tEntry, t1);
                tExit = Math.Min(tExit, t2);

                normal.Y = (t1 < t2) ? -1 : 1;
            }
            else if (circleStart.Y < expandedTop || circleStart.Y > expandedBottom)
                return false;

            if (tEntry > tExit || tEntry > 1 || tExit < 0) return false;

            float tCollision = Math.Max(tEntry, 0f);

            Vector2 collisionPoint = circleStart + relativeVelocity * tCollision;
            Vector2 rectCenter = other.Position;

            float closestX = Math.Clamp(collisionPoint.X, rectCenter.X - other.Width / 2,
                rectCenter.X + other.Width / 2);
            float closestY = Math.Clamp(collisionPoint.Y, rectCenter.Y - other.Height / 2,
                rectCenter.Y + other.Height / 2);

            Vector2 delta = collisionPoint - new Vector2(closestX, closestY);
            float distance = delta.Length();

            if (distance > float.Epsilon)
                normal = Vector2.Normalize(delta);

            collisionData = new CollisionData
            {
                Normal = normal,
                Time = tCollision,
                PenetrationDepth = Math.Max(0, radius - distance)
            };
            return true;
        }

        public override void ResolveCollision(Entity other, CollisionData collisionData)
        {
            if (other is Circle otherCircle)
            {
                ResolveCollision(otherCircle, collisionData);
            }
            else if (other is Rectangle otherRectangle)
            {
                ResolveCollision(otherRectangle, collisionData);
            }
        }

        public void ResolveCollision(Circle other, CollisionData collisionData)
        {
            if (IsTrigger || other.IsTrigger) return;
            
            Vector2 normal = collisionData.Normal;
            float time = collisionData.Time;
            
            // Freeze System
            var invMass = IsFrozen ? 0 : InverseMass;
            var otherInvMass = other.IsFrozen ? 0 : other.InverseMass;

            // Save original state in case we need to revert
            Vector2 originalPos = Position;
            Vector2 originalOtherPos = other.Position;

            // Move to collision time
            Position += Velocity * time;
            other.Position += other.Velocity * time;

            // Compute relative velocity along normal
            Vector2 relativeVelocity = Velocity - other.Velocity;
            float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

            // If separating, revert positions and exit
            if (velocityAlongNormal > 0)
            {
                Position = originalPos;
                other.Position = originalOtherPos;
                return;
            }

            // Calculate impulse (considering bounciness)
            float e = Math.Min(Bounciness, other.Bounciness);
            float j = -(1 + e) * velocityAlongNormal;
            j /= (invMass + otherInvMass);

            // Apply impulse
            Vector2 impulse = j * normal;
            Velocity += impulse * invMass;
            other.Velocity -= impulse * otherInvMass;

            // Positional correction for static collisions
            if (collisionData.PenetrationDepth > 0)
            {
                const float slop = 0.01f;
                const float percent = 0.2f;
                Vector2 correction = (Math.Max(collisionData.PenetrationDepth - slop, 0) /
                                      (invMass + otherInvMass)) * percent * normal;
                Position += correction * invMass;
                other.Position -= correction * otherInvMass;
            }

            // Apply remaining movement after collision
            float remainingTime = 1 - time;
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
        public void ResolveCollision(Rectangle other, CollisionData collisionData)
        {
            if (IsTrigger || other.IsTrigger) return;
            
            // Save original state in case we need to revert
            Vector2 originalPos = Position;
            Vector2 originalOtherPos = other.Position;
            
            // Freeze System
            var invMass = IsFrozen ? 0 : InverseMass;
            var otherInvMass = other.IsFrozen ? 0 : other.InverseMass;
            
            float t = collisionData.Time;
            Position += Velocity * t;
            other.Position += other.Velocity * t;

            Vector2 relativeVelocity = Velocity - other.Velocity;
            float velocityAlongNormal = Vector2.Dot(relativeVelocity, collisionData.Normal);

            if (velocityAlongNormal > 0) 
                return;

            float e = Math.Min(Bounciness, other.Bounciness);
            float j = -(1 + e) * velocityAlongNormal / (invMass + otherInvMass);
            Vector2 impulse = j * collisionData.Normal;

            Velocity += impulse * invMass;
            other.Velocity -= impulse * otherInvMass;

            const float percent = 0.8f;
            const float slop = 0.01f;
            Vector2 correction = collisionData.Normal * 
                Math.Max(collisionData.PenetrationDepth - slop, 0) / 
                (invMass + otherInvMass) * percent;

            Position += correction * invMass;
            other.Position -= correction * otherInvMass;

            float remainingTime = 1 - t;
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
        public override void ResolveBorderCollision(float minX, float maxX, float minY, float maxY)
        {
            // Check if the circle is outside the boundaries and resolve accordingly

            // Handle the X axis
            if (Position.X - Radius < minX) // Colliding with the left wall
            {
                Position = new Vector2(minX + Radius, Position.Y);
                Velocity = new Vector2(Math.Abs(Velocity.X) * Bounciness, Velocity.Y);
            }
            else if (Position.X + Radius > maxX) // Colliding with the right wall
            {
                Position = new Vector2(maxX - Radius, Position.Y);
                Velocity = new Vector2(-Math.Abs(Velocity.X) * Bounciness, Velocity.Y);
            }

            // Handle the Y axis
            if (Position.Y - Radius < minY) // Colliding with the bottom wall
            {
                Position = new Vector2(Position.X, minY + Radius);
                Velocity = new Vector2(Velocity.X, Math.Abs(Velocity.Y) * Bounciness);
            }
            else if (Position.Y + Radius > maxY) // Colliding with the top wall
            {
                Position = new Vector2(Position.X, maxY - Radius);
                Velocity = new Vector2(Velocity.X, -Math.Abs(Velocity.Y) * Bounciness);
            }
        }
    }
}