using System;

namespace SimPhys.Entities
{
    public class Circle : Entity
    {
        public decimal Radius { get; set; } = 0.5m;

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
            collisionData = null;
            
            Vector2 deltaPos = Position - other.Position;
            decimal distanceSquared = deltaPos.LengthSquared();
            decimal radiusSum = Radius + other.Radius;
            decimal radiusSumSquared = radiusSum * radiusSum;

            // Check current overlap
            if (distanceSquared <= radiusSumSquared)
            {
                decimal distance = distanceSquared.Sqrt();
                Vector2 normal = distance > 0 ? deltaPos / distance : Vector2.UnitX;
                collisionData = new CollisionData
                {
                    Normal = normal,
                    Time = 0,
                    PenetrationDepth = radiusSum - distance
                };
                return true;
            }

            // Continuous collision detection
            Vector2 deltaVel = Velocity - other.Velocity;
            decimal a = deltaVel.LengthSquared();
            if (a.NearlyEqual(0)) return false; // No relative movement

            decimal b = 2 * Vector2.Dot(deltaPos, deltaVel);
            decimal c = deltaPos.LengthSquared() - radiusSumSquared;

            decimal discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return false;

            discriminant = discriminant.Sqrt();
            decimal t0 = (-b - discriminant) / (2 * a);
            decimal t1 = (-b + discriminant) / (2 * a);

            decimal t = -1;
            if (t0 >= 0 && t0 <= 1) t = t0;
            else if (t1 >= 0 && t1 <= 1) t = t1;

            if (t < 0 || t > 1) return false;

            // Calculate collision normal at time t
            Vector2 futurePos1 = Position + Velocity * t;
            Vector2 futurePos2 = other.Position + other.Velocity * t;
            Vector2 futureDelta = futurePos1 - futurePos2;
            decimal futureDistance = futureDelta.Length();
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
            decimal radius = Radius;

            decimal expandedLeft = other.Position.X - (other.Width / 2) - radius;
            decimal expandedRight = other.Position.X + (other.Width / 2) + radius;
            decimal expandedTop = other.Position.Y - (other.Height / 2) - radius;
            decimal expandedBottom = other.Position.Y + (other.Height / 2) + radius;

            decimal tEntry = 0m;
            decimal tExit = 1m;
            Vector2 normal = Vector2.Zero;

            if (Math.Abs(relativeVelocity.X) > Epsilon)
            {
                decimal t1 = (expandedLeft - circleStart.X) / relativeVelocity.X;
                decimal t2 = (expandedRight - circleStart.X) / relativeVelocity.X;
                if (t1 > t2) (t1, t2) = (t2, t1);

                tEntry = Math.Max(tEntry, t1);
                tExit = Math.Min(tExit, t2);

                normal.X = (t1 < t2) ? -1 : 1;
            }
            else if (circleStart.X < expandedLeft || circleStart.X > expandedRight)
                return false;

            if (Math.Abs(relativeVelocity.Y) > Epsilon)
            {
                decimal t1 = (expandedTop - circleStart.Y) / relativeVelocity.Y;
                decimal t2 = (expandedBottom - circleStart.Y) / relativeVelocity.Y;
                if (t1 > t2) (t1, t2) = (t2, t1);

                tEntry = Math.Max(tEntry, t1);
                tExit = Math.Min(tExit, t2);

                normal.Y = (t1 < t2) ? -1 : 1;
            }
            else if (circleStart.Y < expandedTop || circleStart.Y > expandedBottom)
                return false;

            if (tEntry > tExit || tEntry > 1 || tExit < 0) return false;

            decimal tCollision = Math.Max(tEntry, 0);

            Vector2 collisionPoint = circleStart + relativeVelocity * tCollision;
            Vector2 rectCenter = other.Position;

            decimal closestX = Math.Min(rectCenter.X + (other.Width / 2), collisionPoint.X);
            closestX = Math.Max(closestX, rectCenter.X - (other.Width / 2));

            decimal closestY = Math.Min(collisionPoint.Y, rectCenter.Y + (other.Height / 2));
            closestY = Math.Max(closestY, rectCenter.Y - (other.Height / 2));

            Vector2 delta = collisionPoint - new Vector2(closestX, closestY);
            decimal distance = delta.Length();

            if (distance > Epsilon)
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
            if (IsFrozen && other.IsFrozen) return;
            
            Vector2 normal = collisionData.Normal;
            decimal time = collisionData.Time;
            
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
            decimal velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

            // If separating, revert positions and exit
            if (velocityAlongNormal > 0)
            {
                Position = originalPos;
                other.Position = originalOtherPos;
                return;
            }

            // Calculate impulse (considering bounciness)
            decimal e = Math.Min(Bounciness, other.Bounciness);
            decimal j = -(1 + e) * velocityAlongNormal;
            j /= (invMass + otherInvMass);

            // Apply impulse
            Vector2 impulse = normal * j;
            Velocity += impulse * invMass;
            other.Velocity -= impulse * otherInvMass;

            // Positional correction for static collisions
            if (collisionData.PenetrationDepth > 0)
            {
                const decimal slop = (decimal)0.01;
                const decimal percent = (decimal)0.2;
                Vector2 correction = (Math.Max(collisionData.PenetrationDepth - slop, 0) /
                                      (invMass + otherInvMass)) * percent * normal;
                Position += correction * invMass;
                other.Position -= correction * otherInvMass;
            }

            // Apply remaining movement after collision
            decimal remainingTime = 1 - time;
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
            if (IsFrozen && other.IsFrozen) return;
            
            // Save original state in case we need to revert
            Vector2 originalPos = Position;
            Vector2 originalOtherPos = other.Position;
            
            // Freeze System
            var invMass = IsFrozen ? 0 : InverseMass;
            var otherInvMass = other.IsFrozen ? 0 : other.InverseMass;
            
            decimal t = collisionData.Time;
            Position += Velocity * t;
            other.Position += other.Velocity * t;

            Vector2 relativeVelocity = Velocity - other.Velocity;
            decimal velocityAlongNormal = Vector2.Dot(relativeVelocity, collisionData.Normal);

            if (velocityAlongNormal > 0) 
                return;

            decimal e = Math.Min(Bounciness, other.Bounciness);
            decimal j = -(1 + e) * velocityAlongNormal / (invMass + otherInvMass);
            Vector2 impulse = j * collisionData.Normal;

            Velocity += impulse * invMass;
            other.Velocity -= impulse * otherInvMass;

            const decimal percent = 0.8m;
            const decimal slop = 0.01m;
            Vector2 correction = collisionData.Normal * 
                Math.Max(collisionData.PenetrationDepth - slop, 0) / 
                (invMass + otherInvMass) * percent;

            Position += correction * invMass;
            other.Position -= correction * otherInvMass;

            decimal remainingTime = 1 - t;
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
            else if (other is Rectangle otherRectangle)
            {
                ForceResolveCollision(otherRectangle, collisionData);
            }
        }
        
        public void ForceResolveCollision(Circle other, CollisionData collisionData)
        {
            // Exit if either entity is a trigger (no physical resolution needed)
            if (IsTrigger || other.IsTrigger) return;
            if (IsFrozen && other.IsFrozen) return;

            // Calculate the penetration depth and normal
            decimal penetrationDepth = collisionData.PenetrationDepth;
            Vector2 normal = collisionData.Normal;

            // Handle frozen/static entities
            decimal invMass = IsFrozen ? 0 : InverseMass;
            decimal otherInvMass = other.IsFrozen ? 0 : other.InverseMass;

            // Avoid division by zero
            decimal totalInverseMass = invMass + otherInvMass;
            if (totalInverseMass == 0) return;

            // Calculate positional adjustments
            Vector2 separation = (penetrationDepth / totalInverseMass) * normal;
            Vector2 thisAdjustment = separation * invMass;
            Vector2 otherAdjustment = -separation * otherInvMass;

            // Apply adjustments to positions
            Position += thisAdjustment;
            other.Position += otherAdjustment;
        }
        public void ForceResolveCollision(Rectangle rectangle, CollisionData collisionData)
        {
            // Exit if either entity is a trigger (no physical resolution needed)
            if (IsTrigger || rectangle.IsTrigger) return;
            if (IsFrozen && rectangle.IsFrozen) return;

            // Get the closest point on the rectangle to the circle's center
            Vector2 closestPoint = GetClosestPointOnRectangle(rectangle);
            
            Vector2 GetClosestPointOnRectangle(Rectangle r)
            {
                // Calculate the half extents of the rectangle
                decimal halfWidth = r.Width / 2;
                decimal halfHeight = r.Height / 2;

                // Clamp the circle's position to the bounds of the rectangle
                decimal closestX = Math.Max(Position.X, r.Position.X - halfWidth);
                closestX = Math.Min(closestX, r.Position.X + halfWidth);

                decimal closestY = Math.Max(Position.Y, r.Position.Y - halfHeight);
                closestY = Math.Min(closestY, r.Position.Y + halfHeight);

                return new Vector2(closestX, closestY);
            }

            // Calculate the direction and distance from the closest point to the circle's center
            Vector2 direction = Position - closestPoint;
            decimal distance = direction.Length();

            // If the circle is not actually intersecting the rectangle, exit
            if (distance >= Radius) return;

            // Normalize the direction to get the separation normal
            direction = direction.Normalized();

            // Calculate the penetration depth
            decimal penetrationDepth = Radius - distance;

            // Handle frozen/static entities
            decimal invMass = IsFrozen ? 0 : InverseMass;
            decimal rectInvMass = rectangle.IsFrozen ? 0 : rectangle.InverseMass;

            // Avoid division by zero
            decimal totalInverseMass = invMass + rectInvMass;
            if (totalInverseMass == 0) return;

            // Calculate positional adjustments
            Vector2 separation = (penetrationDepth / totalInverseMass) * direction;
            Vector2 circleAdjustment = separation * invMass;
            Vector2 rectAdjustment = -separation * rectInvMass;

            // Apply adjustments to positions
            Position += circleAdjustment;
            rectangle.Position += rectAdjustment;
        }
        
        public override void ResolveBorderCollision(decimal minX, decimal maxX, decimal minY, decimal maxY)
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