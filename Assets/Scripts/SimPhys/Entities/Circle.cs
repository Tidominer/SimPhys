using System;

namespace SimPhys.Entities
{
    public class Circle : Entity
    {
        public float Radius { get; set; }

        public override bool CheckCollision(Entity other, float deltaTime, out CollisionManifold manifold)
        {
            manifold = new CollisionManifold();
            if (other is Circle otherCircle)
            {
                return CheckCircleCollision(otherCircle, deltaTime, ref manifold);
            }

            if (other is Rectangle rect)
            {
                return CheckRectangleCollision(rect, deltaTime, ref manifold);
            }

            return false;
        }

        private bool CheckCircleCollision(Circle other, float deltaTime, ref CollisionManifold manifold)
        {
            Vector2 deltaP = other.Position - Position;
            Vector2 deltaV = other.Velocity - Velocity;
            float radiusSum = Radius + other.Radius;

            float a = deltaV.LengthSquared();
            if (Math.Abs(a) < float.Epsilon)
            {
                float distanceSq = deltaP.LengthSquared();
                if (distanceSq > radiusSum * radiusSum) return false;

                manifold.EntityA = this;
                manifold.EntityB = other;
                manifold.Normal = deltaP.Normalized();
                manifold.Depth = radiusSum - deltaP.Length();
                manifold.Time = 0;
                return true;
            }

            float b = 2 * Vector2.Dot(deltaP, deltaV);
            float c = deltaP.LengthSquared() - radiusSum * radiusSum;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return false;

            float sqrtD = (float)Math.Sqrt(discriminant);
            float t1 = (-b - sqrtD) / (2 * a);
            float t2 = (-b + sqrtD) / (2 * a);
            float t = Math.Min(t1, t2);

            if (t < 0 || t > deltaTime) return false;

            Vector2 futureA = Position + Velocity * t;
            Vector2 futureB = other.Position + other.Velocity * t;
            Vector2 normal = (futureB - futureA).Normalized();

            manifold.EntityA = this;
            manifold.EntityB = other;
            manifold.Normal = normal;
            manifold.Depth = radiusSum - (futureB - futureA).Length();
            manifold.Time = t;
            return true;
        }

        public bool CheckRectangleCollision(Rectangle rect, float deltaTime, ref CollisionManifold manifold)
        {
            Vector2 rectCenter = rect.Position + new Vector2(rect.Width / 2, rect.Height / 2);
            Vector2 closestPoint = new Vector2(
                Math.Clamp(Position.X, rect.Position.X, rect.Position.X + rect.Width),
                Math.Clamp(Position.Y, rect.Position.Y, rect.Position.Y + rect.Height)
            );

            Vector2 delta = Position - closestPoint;
            float distanceSq = delta.LengthSquared();
            float radius = Radius + 0.001f; // Small buffer

            // Static collision check
            if (distanceSq < radius * radius)
            {
                manifold.EntityA = this;
                manifold.EntityB = rect;
                manifold.Normal = delta.Normalized();
                manifold.Depth = radius - (float)Math.Sqrt(distanceSq);
                manifold.Time = 0;
                return true;
            }

            // Dynamic collision check
            Vector2 relVelocity = Velocity - rect.Velocity;
            Vector2 rayEnd = Position + relVelocity * deltaTime;
            Vector2 rayDir = rayEnd - Position;

            // Handle zero velocity case
            if (rayDir.LengthSquared() < float.Epsilon) return false;

            float t = 0;
            Vector2 normal = Vector2.Zero;
            float nearestT = float.MaxValue;

            // Check against rectangle sides
            CheckAxis(rect.Position.X, rect.Position.X + rect.Width, Position.X, rayDir.X, Position.Y, rayDir.Y,
                rect.Position.Y, rect.Position.Y + rect.Height, ref nearestT, ref normal, 0);
            CheckAxis(rect.Position.Y, rect.Position.Y + rect.Height, Position.Y, rayDir.Y, Position.X, rayDir.X,
                rect.Position.X, rect.Position.X + rect.Width, ref nearestT, ref normal, 1);

            if (nearestT <= deltaTime)
            {
                manifold.EntityA = this;
                manifold.EntityB = rect;
                manifold.Normal = normal;
                manifold.Depth = Radius - (Position + relVelocity * nearestT - closestPoint).Length();
                manifold.Time = nearestT;
                return true;
            }

            return false;
        }

        private void CheckAxis(float rectMin, float rectMax, float pos, float dir, float otherPos, float otherDir,
            float otherMin, float otherMax, ref float nearestT, ref Vector2 normal, int axis)
        {
            if (Math.Abs(dir) < float.Epsilon) return;

            float tEnter = (rectMin - pos - Radius) / dir;
            float tExit = (rectMax - pos + Radius) / dir;

            if (tEnter > tExit) (tEnter, tExit) = (tExit, tEnter);

            tEnter = Math.Max(tEnter, 0);
            tExit = Math.Min(tExit, 1);

            if (tEnter < nearestT)
            {
                float intersectOther = otherPos + otherDir * tEnter;
                if (intersectOther >= otherMin && intersectOther <= otherMax)
                {
                    nearestT = tEnter;
                    normal = axis == 0 ? new Vector2(-Math.Sign(dir), 0) : new Vector2(0, -Math.Sign(dir));
                }
            }
        }
    }
}