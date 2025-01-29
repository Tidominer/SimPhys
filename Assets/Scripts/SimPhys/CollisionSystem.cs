using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SimPhys.Entities;

namespace SimPhys
{
    public class CollisionSystem
    {
        public void CheckCollisions(List<Entity> entities)
        {
            // First check continuous collisions
            CheckContinuousCollisions(entities);

            // Then check discrete collisions
            CheckDiscreteCollisions(entities);
        }
        
        public void CheckDiscreteCollisions(List<Entity> entities)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                for (int j = i + 1; j < entities.Count; j++)
                {
                    Entity a = entities[i];
                    Entity b = entities[j];

                    if (a.Intersects(b))
                    {
                        Vector2 normal;
                        float depth;
                        if (GetCollisionNormalAndDepth(a, b, out normal, out depth))
                        {
                            ResolveCollision(a, b, normal, depth);
                            a.OnEntityCollision?.Invoke(b);
                            b.OnEntityCollision?.Invoke(a);
                        }
                    }
                }
            }
        }

        private void CheckContinuousCollisions(List<Entity> entities)
        {
            foreach (Entity entity in entities)
            {
                if (entity.Velocity.LengthSquared() < 0.1f) continue;

                foreach (Entity other in entities)
                {
                    if (entity == other) continue;

                    if (PredictCollision(entity, other, out float t))
                    {
                        ResolvePredictedCollision(entity, other, t);
                    }
                }
            }
        }

        private bool PredictCollision(Entity a, Entity b, out float t)
        {
            t = 1f;
            bool collisionFound = false;

            switch (a, b)
            {
                case (Circle ca, Circle cb):
                    collisionFound = PredictCircleCircle(ca, cb, ref t);
                    break;
                case (Circle c, Rectangle r):
                    collisionFound = PredictCircleRectangle(c, r, ref t);
                    break;
                case (Rectangle r, Circle c):
                    collisionFound = PredictCircleRectangle(c, r, ref t);
                    break;
            }

            return collisionFound && t < 1;
        }

        private bool PredictCircleCircle(Circle a, Circle b, ref float t)
        {
            Vector2 relativeVelocity = b.Velocity - a.Velocity;
            Vector2 displacement = b.Position - a.Position;
            float radiusSum = a.Radius + b.Radius;

            float aCoeff = relativeVelocity.LengthSquared();
            float bCoeff = 2 * Vector2.Dot(displacement, relativeVelocity);
            float cCoeff = displacement.LengthSquared() - radiusSum * radiusSum;

            if (MathF.Abs(aCoeff) < float.Epsilon) return false;

            float discriminant = bCoeff * bCoeff - 4 * aCoeff * cCoeff;
            if (discriminant < 0) return false;

            float sqrtDisc = MathF.Sqrt(discriminant);
            float t1 = (-bCoeff - sqrtDisc) / (2 * aCoeff);
            float t2 = (-bCoeff + sqrtDisc) / (2 * aCoeff);

            t = MathF.Max(0, MathF.Min(t1, t2));
            return t <= 1;
        }

        private bool PredictCircleRectangle(Circle c, Rectangle r, ref float t)
        {
            Vector2 predictedPosition = c.Position + c.Velocity;
            Vector2 closestPoint = GetClosestPointOnRectangle(predictedPosition, r);
            Vector2 movementDirection = predictedPosition - c.Position;

            if (Vector2.DistanceSquared(closestPoint, c.Position) <= c.Radius * c.Radius)
            {
                t = 0;
                return true;
            }

            Vector2 movementNormalized = movementDirection;
            if (movementNormalized.LengthSquared() > 0)
                movementNormalized = Vector2.Normalize(movementNormalized);

            Vector2 rayStart = c.Position;
            Vector2 rayEnd = predictedPosition;
            float rayLength = movementDirection.Length();

            return RaycastRectangle(rayStart, rayEnd, r, c.Radius, out t);
        }

        private bool RaycastRectangle(Vector2 start, Vector2 end, Rectangle rect, float radius, out float t)
        {
            t = 1f;
            bool collision = false;
            Vector2 direction = end - start;
            float length = direction.Length();

            if (length < float.Epsilon)
            {
                return false;
            }

            Vector2 normalizedDir = direction / length;
            Vector2 expandedRectSize = new Vector2(rect.Width / 2 + radius, rect.Height / 2 + radius);

            Vector2 localStart = ToLocalSpace(start, rect);
            Vector2 localEnd = ToLocalSpace(end, rect);
            Vector2 localDir = localEnd - localStart;

            float tMin = 0;
            float tMax = 1;

            // Check X axis
            if (MathF.Abs(localDir.X) < float.Epsilon)
            {
                if (localStart.X < -expandedRectSize.X || localStart.X > expandedRectSize.X)
                    return false;
            }
            else
            {
                float t1 = (-expandedRectSize.X - localStart.X) / localDir.X;
                float t2 = (expandedRectSize.X - localStart.X) / localDir.X;
                tMin = MathF.Max(tMin, MathF.Min(t1, t2));
                tMax = MathF.Min(tMax, MathF.Max(t1, t2));
            }

            // Check Y axis
            if (MathF.Abs(localDir.Y) < float.Epsilon)
            {
                if (localStart.Y < -expandedRectSize.Y || localStart.Y > expandedRectSize.Y)
                    return false;
            }
            else
            {
                float t1 = (-expandedRectSize.Y - localStart.Y) / localDir.Y;
                float t2 = (expandedRectSize.Y - localStart.Y) / localDir.Y;
                tMin = MathF.Max(tMin, MathF.Min(t1, t2));
                tMax = MathF.Min(tMax, MathF.Max(t1, t2));
            }

            if (tMax >= tMin && tMin <= 1 && tMax >= 0)
            {
                t = MathF.Max(0, tMin);
                collision = true;
            }

            return collision;
        }

        private void ResolvePredictedCollision(Entity a, Entity b, float t)
        {
            // Save original state
            Vector2 aOriginalPos = a.Position;
            Vector2 bOriginalPos = b.Position;
            Vector2 aOriginalVel = a.Velocity;
            Vector2 bOriginalVel = b.Velocity;

            // Move to collision point
            a.Position += a.Velocity * t;
            b.Position += b.Velocity * t;

            // Resolve collision
            if (GetCollisionNormalAndDepth(a, b, out Vector2 normal, out float depth))
            {
                ResolveCollision(a, b, normal, depth);
            }

            // Restore original positions and adjust velocities
            a.Position = aOriginalPos;
            b.Position = bOriginalPos;
            a.Velocity = aOriginalVel * (1 - t) + a.Velocity * t;
            b.Velocity = bOriginalVel * (1 - t) + b.Velocity * t;
        }

        private bool GetCollisionNormalAndDepth(Entity a, Entity b, out Vector2 normal, out float depth)
        {
            normal = Vector2.Zero;
            depth = 0f;

            switch (a, b)
            {
                case (Circle ca, Circle cb):
                    return CircleCircleCollision(ca, cb, out normal, out depth);
                case (Circle c, Rectangle r):
                    return CircleRectangleCollision(c, r, out normal, out depth);
                case (Rectangle r, Circle c):
                    bool result = CircleRectangleCollision(c, r, out normal, out depth);
                    normal = -normal;
                    return result;
                case (Rectangle ra, Rectangle rb):
                    return RectangleRectangleCollision(ra, rb, out normal, out depth);
                default:
                    return false;
            }
        }

        private bool CircleCircleCollision(Circle a, Circle b, out Vector2 normal, out float depth)
        {
            Vector2 delta = b.Position - a.Position;
            float distance = delta.Length();
            float radiusSum = a.Radius + b.Radius;

            if (distance == 0)
            {
                normal = Vector2.UnitX;
                depth = radiusSum;
                return true;
            }

            normal = delta / distance;
            depth = radiusSum - distance;
            return true;
        }

        private bool CircleRectangleCollision(Circle circle, Rectangle rect, out Vector2 normal, out float depth)
        {
            Vector2 closestPoint = GetClosestPointOnRectangle(circle.Position, rect);
            Vector2 delta = circle.Position - closestPoint;
            float distance = delta.Length();

            normal = distance > 0 ? delta / distance : Vector2.UnitX;
            depth = circle.Radius - distance;

            return depth > 0;
        }

        private Vector2 GetClosestPointOnRectangle(Vector2 point, Rectangle rect)
        {
            Vector2 localPoint = ToLocalSpace(point, rect);
            float halfWidth = rect.Width / 2;
            float halfHeight = rect.Height / 2;

            Vector2 closestLocal = new Vector2(
                Math.Clamp(localPoint.X, -halfWidth, halfWidth),
                Math.Clamp(localPoint.Y, -halfHeight, halfHeight)
            );

            return ToWorldSpace(closestLocal, rect);
        }

        private Vector2 ToLocalSpace(Vector2 point, Rectangle rect)
        {
            Vector2 translated = point - rect.Position;
            return new Vector2(
                translated.X * MathF.Cos(rect.Rotation) + translated.Y * MathF.Sin(rect.Rotation),
                -translated.X * MathF.Sin(rect.Rotation) + translated.Y * MathF.Cos(rect.Rotation)
            );
        }

        private Vector2 ToWorldSpace(Vector2 point, Rectangle rect)
        {
            Vector2 rotated = new Vector2(
                point.X * MathF.Cos(rect.Rotation) - point.Y * MathF.Sin(rect.Rotation),
                point.X * MathF.Sin(rect.Rotation) + point.Y * MathF.Cos(rect.Rotation)
            );
            return rotated + rect.Position;
        }

        private bool RectangleRectangleCollision(Rectangle a, Rectangle b, out Vector2 normal, out float depth)
        {
            Vector2[] axes = GetAxes(a).Concat(GetAxes(b)).ToArray();
            depth = float.MaxValue;
            normal = Vector2.Zero;

            foreach (Vector2 axis in axes)
            {
                (float minA, float maxA) = Project(a, axis);
                (float minB, float maxB) = Project(b, axis);

                if (minA >= maxB || minB >= maxA)
                {
                    depth = 0;
                    return false;
                }

                float axisDepth = MathF.Min(maxB - minA, maxA - minB);
                if (axisDepth < depth)
                {
                    depth = axisDepth;
                    normal = axis;
                }
            }

            Vector2 direction = b.Position - a.Position;
            if (Vector2.Dot(direction, normal) < 0)
                normal = -normal;

            return true;
        }

        private IEnumerable<Vector2> GetAxes(Rectangle rect)
        {
            Vector2[] corners = rect.GetRotatedCorners();
            Vector2[] edges =
            {
                corners[1] - corners[0],
                corners[3] - corners[0]
            };

            foreach (Vector2 edge in edges)
            {
                Vector2 axis = Vector2.Normalize(new Vector2(-edge.Y, edge.X));
                yield return axis;
            }
        }

        private (float min, float max) Project(Rectangle rect, Vector2 axis)
        {
            Vector2[] corners = rect.GetRotatedCorners();
            float min = Vector2.Dot(corners[0], axis);
            float max = min;

            for (int i = 1; i < corners.Length; i++)
            {
                float projection = Vector2.Dot(corners[i], axis);
                min = MathF.Min(min, projection);
                max = MathF.Max(max, projection);
            }

            return (min, max);
        }

        private void ResolveCollision(Entity a, Entity b, Vector2 normal, float depth)
        {
            PositionalCorrection(a, b, normal, depth);
            VelocityResolution(a, b, normal);
        }

        private void PositionalCorrection(Entity a, Entity b, Vector2 normal, float depth)
        {
            float percent = 0.6f;
            float slop = 0.01f;
            float correction = MathF.Max(depth - slop, 0) / (a.InverseMass + b.InverseMass) * percent;

            a.Position += normal * correction * a.InverseMass;
            b.Position -= normal * correction * b.InverseMass;
        }

        private void VelocityResolution(Entity a, Entity b, Vector2 normal)
        {
            Vector2 relativeVelocity = b.Velocity - a.Velocity;
            float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

            if (velocityAlongNormal > 0) return;

            float e = MathF.Min(a.Bounciness, b.Bounciness);
            float j = -(1 + e) * velocityAlongNormal;
            j /= a.InverseMass + b.InverseMass;

            Vector2 impulse = j * normal;
            a.Velocity += impulse * a.InverseMass;
            b.Velocity -= impulse * b.InverseMass;
        }
    }
}