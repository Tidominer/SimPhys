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

            // --- First, check for static (initial) overlap ---
            // This part is the same as the previous static implementation.
            Vector2 delta = Position - other.Position;
            decimal cosNegR = (decimal)Math.Cos((double)-other.Rotation);
            decimal sinNegR = (decimal)Math.Sin((double)-other.Rotation);
            Vector2 rotatedCircleCenter = new Vector2(
                delta.X * cosNegR - delta.Y * sinNegR,
                delta.X * sinNegR + delta.Y * cosNegR
            );
            decimal halfWidth = other.Width / 2;
            decimal halfHeight = other.Height / 2;
            Vector2 closestPointLocal = new Vector2(
                Math.Max(-halfWidth, Math.Min(rotatedCircleCenter.X, halfWidth)),
                Math.Max(-halfHeight, Math.Min(rotatedCircleCenter.Y, halfHeight))
            );
            Vector2 localDelta = rotatedCircleCenter - closestPointLocal;

            if (localDelta.LengthSquared() <= Radius * Radius)
            {
                // Static overlap detected, return collision data for immediate resolution.
                decimal distance = localDelta.Length();
                Vector2 normalLocal = distance > 0 ? localDelta.Normalized() : Vector2.UnitX;

                decimal cosR = (decimal)Math.Cos((double)other.Rotation);
                decimal sinR = (decimal)Math.Sin((double)other.Rotation);
                Vector2 worldNormal = new Vector2(
                    normalLocal.X * cosR - normalLocal.Y * sinR,
                    normalLocal.X * sinR + normalLocal.Y * cosR
                );

                collisionData = new CollisionData
                {
                    Normal = worldNormal,
                    PenetrationDepth = Radius - distance,
                    Time = 0
                };
                return true;
            }

            // --- If no static overlap, perform Continuous Collision Detection (CCD) ---
            Vector2 relativeVelocity = this.Velocity - other.Velocity;
            if (relativeVelocity.LengthSquared().NearlyEqual(0)) return false; // No relative motion

            // Transform relative velocity into rectangle's local space
            Vector2 localVelocity = new Vector2(
                relativeVelocity.X * cosNegR - relativeVelocity.Y * sinNegR,
                relativeVelocity.X * sinNegR + relativeVelocity.Y * cosNegR
            );

            // Sweep the circle against the AABB
            decimal tEnter = 0, tExit = 1;
            decimal t;

            // Test X-axis slab
            if (localVelocity.X.NearlyEqual(0))
            {
                if (rotatedCircleCenter.X - Radius > halfWidth || rotatedCircleCenter.X + Radius < -halfWidth)
                    return false;
            }
            else
            {
                t = (-halfWidth - Radius - rotatedCircleCenter.X) / localVelocity.X;
                decimal t2 = (halfWidth + Radius - rotatedCircleCenter.X) / localVelocity.X;
                if (t > t2)
                {
                    var temp = t;
                    t = t2;
                    t2 = temp;
                }

                tEnter = Math.Max(tEnter, t);
                tExit = Math.Min(tExit, t2);
                if (tEnter > tExit) return false;
            }

            // Test Y-axis slab
            if (localVelocity.Y.NearlyEqual(0))
            {
                if (rotatedCircleCenter.Y - Radius > halfHeight || rotatedCircleCenter.Y + Radius < -halfHeight)
                    return false;
            }
            else
            {
                t = (-halfHeight - Radius - rotatedCircleCenter.Y) / localVelocity.Y;
                decimal t2 = (halfHeight + Radius - rotatedCircleCenter.Y) / localVelocity.Y;
                if (t > t2)
                {
                    var temp = t;
                    t = t2;
                    t2 = temp;
                }

                tEnter = Math.Max(tEnter, t);
                tExit = Math.Min(tExit, t2);
                if (tEnter > tExit) return false;
            }

            // Check for collision with corners (as moving point vs static circles)
            // For simplicity, this implementation currently relies on the slab test.
            // A full implementation would also solve for time of impact with the four corner vertices.
            // However, the slab test on the Minkowski sum is a very strong and often sufficient heuristic.

            if (tEnter > 0 && tEnter < 1)
            {
                // Collision found, now determine the normal
                Vector2 pointOfImpactLocal = rotatedCircleCenter + localVelocity * tEnter;
                Vector2 closestPointOnAABB = new Vector2(
                    Math.Max(-halfWidth, Math.Min(pointOfImpactLocal.X, halfWidth)),
                    Math.Max(-halfHeight, Math.Min(pointOfImpactLocal.Y, halfHeight))
                );

                Vector2 normalLocal = (pointOfImpactLocal - closestPointOnAABB).Normalized();

                // Rotate normal back to world space
                decimal cosR = (decimal)Math.Cos((double)other.Rotation);
                decimal sinR = (decimal)Math.Sin((double)other.Rotation);
                Vector2 worldNormal = new Vector2(
                    normalLocal.X * cosR - normalLocal.Y * sinR,
                    normalLocal.X * sinR + normalLocal.Y * cosR
                );

                collisionData = new CollisionData
                {
                    Normal = worldNormal,
                    Time = tEnter,
                    PenetrationDepth = 0
                };
                return true;
            }

            return false;
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
            if (IsTrigger || rectangle.IsTrigger) return;
            if (IsFrozen && rectangle.IsFrozen) return;

            decimal invMass = IsFrozen ? 0 : InverseMass;
            decimal rectInvMass = rectangle.IsFrozen ? 0 : rectangle.InverseMass;

            decimal totalInverseMass = invMass + rectInvMass;
            if (totalInverseMass.NearlyEqual(0)) return;

            // Use the MTV from collision data to separate the objects
            Vector2 mtv = collisionData.Normal * collisionData.PenetrationDepth;

            // Ensure MTV pushes objects apart correctly
            Vector2 relativePos = Position - rectangle.Position;
            if (Vector2.Dot(relativePos, mtv) < 0)
            {
                mtv = -mtv;
            }

            // Apply adjustments based on inverse mass
            Position += mtv * (invMass / totalInverseMass);
            rectangle.Position -= mtv * (rectInvMass / totalInverseMass);
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