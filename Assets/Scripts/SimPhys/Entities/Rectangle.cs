using System;
using System.Numerics;

namespace SimPhys.Entities
{
    public class Rectangle : Entity
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Rotation { get; set; }
        

        public override bool Intersects(Entity other)
        {
            if (other is Rectangle rect)
            {
                // Get rectangle corners in world space
                Vector2[] cornersA = GetRotatedCorners(this);
                Vector2[] cornersB = GetRotatedCorners(rect);

                // Get axes to test (normals of edges)
                Vector2[] axes =
                {
                    Vector2.Normalize(cornersA[1] - cornersA[0]),
                    Vector2.Normalize(cornersA[3] - cornersA[0]),
                    Vector2.Normalize(cornersB[1] - cornersB[0]),
                    Vector2.Normalize(cornersB[3] - cornersB[0])
                };

                // Perform SAT test on each axis
                foreach (Vector2 axis in axes)
                {
                    if (!OverlapOnAxis(cornersA, cornersB, axis))
                        return false;
                }

                return true;
            }
            else if (other is Circle circle)
            {
                return circle.Intersects(this); // Use circle's method to handle the collision
            }

            return false;
        }

        public Vector2[] GetRotatedCorners() => GetRotatedCorners(this);
        public Vector2[] GetRotatedCorners(Rectangle rect)
        {
            float cosTheta = MathF.Cos(rect.Rotation);
            float sinTheta = MathF.Sin(rect.Rotation);

            Vector2[] localCorners =
            {
                new Vector2(-rect.Width / 2, -rect.Height / 2),
                new Vector2(rect.Width / 2, -rect.Height / 2),
                new Vector2(rect.Width / 2, rect.Height / 2),
                new Vector2(-rect.Width / 2, rect.Height / 2)
            };

            Vector2[] worldCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                worldCorners[i] = new Vector2(
                    cosTheta * localCorners[i].X - sinTheta * localCorners[i].Y + rect.Position.X,
                    sinTheta * localCorners[i].X + cosTheta * localCorners[i].Y + rect.Position.Y
                );
            }

            return worldCorners;
        }

        private bool OverlapOnAxis(Vector2[] cornersA, Vector2[] cornersB, Vector2 axis)
        {
            (float minA, float maxA) = ProjectCornersOntoAxis(cornersA, axis);
            (float minB, float maxB) = ProjectCornersOntoAxis(cornersB, axis);

            return maxA >= minB && maxB >= minA; // Overlapping check
        }

        private (float min, float max) ProjectCornersOntoAxis(Vector2[] corners, Vector2 axis)
        {
            float min = Vector2.Dot(corners[0], axis);
            float max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                float projection = Vector2.Dot(corners[i], axis);
                if (projection < min) min = projection;
                if (projection > max) max = projection;
            }

            return (min, max);
        }

        public void ResolveCollision(Entity entity)
        {
            if (entity is Circle s)
                ResolveCollision(s);
            else if (entity is Rectangle r)
                ResolveCollision(r);
        }
/*
        public void ResolveCollision(Rectangle rectangle)
        {
            // Use the Separating Axis Theorem (SAT) for collision detection
            Vector2[] axes = GetAxes(this).Concat(GetAxes(rectangle)).ToArray();
            float smallestOverlap = float.MaxValue;
            Vector2 smallestAxis = new Vector2();

            foreach (Vector2 axis in axes)
            {
                (float minA, float maxA) = ProjectOntoAxis(this, axis);
                (float minB, float maxB) = ProjectOntoAxis(rectangle, axis);

                float overlap = Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB);
                if (overlap <= 0)
                {
                    return; // No collision
                }

                if (overlap < smallestOverlap)
                {
                    smallestOverlap = overlap;
                    smallestAxis = axis;
                }
            }

            // Resolve collision
            Vector2 relativeVelocity = rectangle.Velocity - this.Velocity;
            float velocityAlongNormal = Vector2.Dot(relativeVelocity, smallestAxis);

            if (velocityAlongNormal > 0)
                return;

            float restitution = Mathf.Min(this.Bounciness, rectangle.Bounciness);

            float impulseScalar = -(1 + restitution) * velocityAlongNormal;
            impulseScalar /= 1 / this.Mass + 1 / rectangle.Mass;

            Vector2 impulse = impulseScalar * smallestAxis;
            this.Velocity -= impulse / this.Mass;
            rectangle.Velocity += impulse / rectangle.Mass;

            // Correct positions to avoid sinking
            float correction = smallestOverlap / (1 / this.Mass + 1 / rectangle.Mass);
            Vector2 correctionVector = correction * smallestAxis;
            this.Position -= correctionVector / this.Mass;
            rectangle.Position += correctionVector / rectangle.Mass;
        }

        public void ResolveCollision(Circle circle)
        {
            // Transform the sphere's position into the rectangle's local space
            Vector2 rectangleCenter = Position;
            float cosRotation = Mathf.Cos(Rotation);
            float sinRotation = Mathf.Sin(Rotation);

            Vector2 relativePosition = circle.Position - rectangleCenter;
            Vector2 rotatedPosition = new Vector2(
                cosRotation * relativePosition.x + sinRotation * relativePosition.y,
                -sinRotation * relativePosition.x + cosRotation * relativePosition.y
            );

            // Find the closest point on the rectangle to the sphere's center
            float closestX = Mathf.Clamp(rotatedPosition.x, -Width / 2, Width / 2);
            float closestY = Mathf.Clamp(rotatedPosition.y, -Height / 2, Height / 2);
            Vector2 closestPoint = new Vector2(closestX, closestY);

            // Calculate the distance from the sphere's center to the closest point
            Vector2 distance = rotatedPosition - closestPoint;
            float distanceMagnitude = distance.magnitude;

            // Check if there is a collision
            if (distanceMagnitude < circle.Radius)
            {
                // Calculate the collision normal
                Vector2 collisionNormal = distance.normalized;
                Vector2 worldCollisionNormal = new Vector2(
                    cosRotation * collisionNormal.x - sinRotation * collisionNormal.y,
                    sinRotation * collisionNormal.x + cosRotation * collisionNormal.y
                );

                // Calculate relative velocity
                Vector2 relativeVelocity = circle.Velocity - this.Velocity;

                // Calculate velocity along the normal
                float velocityAlongNormal = Vector2.Dot(relativeVelocity, worldCollisionNormal);

                // Ignore if moving apart
                if (velocityAlongNormal > 0)
                    return;

                // Calculate restitution
                float restitution = Mathf.Min(this.Bounciness, circle.Bounciness);

                // Calculate impulse scalar
                float impulseScalar = -(1 + restitution) * velocityAlongNormal;
                impulseScalar /= 1 / this.Mass + 1 / circle.Mass;

                // Apply impulse
                Vector2 impulse = impulseScalar * worldCollisionNormal;
                this.Velocity += impulse / this.Mass;
                circle.Velocity -= impulse / circle.Mass;

                // Correct positions to avoid sinking
                float overlap = circle.Radius - distanceMagnitude;
                Vector2 correctionVector = overlap * worldCollisionNormal;
                circle.Position += correctionVector * (1 / circle.Mass);
                this.Position -= correctionVector * (1 / this.Mass);
            }
        }

        private static (float min, float max) ProjectOntoAxis(Rectangle rect, Vector2 axis)
        {
            Vector2[] corners = GetCorners(rect);
            float min = Vector2.Dot(corners[0], axis);
            float max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                float projection = Vector2.Dot(corners[i], axis);
                if (projection < min)
                    min = projection;
                if (projection > max)
                    max = projection;
            }

            return (min, max);
        }

        private static Vector2[] GetAxes(Rectangle rect)
        {
            Vector2[] corners = GetCorners(rect);
            return new Vector2[]
            {
                (corners[1] - corners[0]).normalized,
                (corners[3] - corners[0]).normalized
            };
        }

        private static Vector2[] GetCorners(Rectangle rect)
        {
            float cos = Mathf.Cos(rect.Rotation);
            float sin = Mathf.Sin(rect.Rotation);

            Vector2 halfExtents = new Vector2(rect.Width / 2, rect.Height / 2);

            Vector2[] corners = new Vector2[4];
            corners[0] = rect.Position + new Vector2(cos * (-halfExtents.x) - sin * (-halfExtents.y),
                sin * (-halfExtents.x) + cos * (-halfExtents.y));
            corners[1] = rect.Position + new Vector2(cos * (halfExtents.x) - sin * (-halfExtents.y),
                sin * (halfExtents.x) + cos * (-halfExtents.y));
            corners[2] = rect.Position + new Vector2(cos * (halfExtents.x) - sin * (halfExtents.y),
                sin * (halfExtents.x) + cos * (halfExtents.y));
            corners[3] = rect.Position + new Vector2(cos * (-halfExtents.x) - sin * (halfExtents.y),
                sin * (-halfExtents.x) + cos * (halfExtents.y));

            return corners;
        }*/
    }
}