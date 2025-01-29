using System;
using System.Numerics;

namespace SimPhys.Entities
{
    public class Circle : Entity
    {
        public float Radius { get; set; }
        
        public override bool Intersects(Entity other)
        {
            if (other is Circle circle)
            {
                float distanceSquared = (circle.Position - this.Position).LengthSquared();
                float radiusSum = this.Radius + circle.Radius;
                return distanceSquared < (radiusSum * radiusSum);
            }
            else if (other is Rectangle rect)
            {
                // Convert circle center to rectangle's local space
                float cosTheta = MathF.Cos(-rect.Rotation);
                float sinTheta = MathF.Sin(-rect.Rotation);
        
                Vector2 localCirclePos = new Vector2(
                    cosTheta * (this.Position.X - rect.Position.X) - sinTheta * (this.Position.Y - rect.Position.Y),
                    sinTheta * (this.Position.X - rect.Position.X) + cosTheta * (this.Position.Y - rect.Position.Y)
                );

                // Find the closest point on the axis-aligned rectangle
                float closestX = Math.Clamp(localCirclePos.X, -rect.Width / 2, rect.Width / 2);
                float closestY = Math.Clamp(localCirclePos.Y, -rect.Height / 2, rect.Height / 2);
        
                // Compute the distance between this closest point and the circle’s center in local space
                float distanceX = localCirclePos.X - closestX;
                float distanceY = localCirclePos.Y - closestY;
                float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);

                // If the distance is less than the circle’s radius, they collide
                return distanceSquared < (this.Radius * this.Radius);
            }

            return false;
        }

        /*public void ResolveBorderCollision(Vector2 boxSize)
        {
            var targetPos = Position + Velocity;

            if (targetPos.x >= boxSize.x - Radius) // Right boundary
            {
                float t = (boxSize.x - Radius - Position.x) / Velocity.x;
                float intersectionY = Position.y + Velocity.y * t;
                targetPos = new Vector2(boxSize.x - Radius, intersectionY);
                Velocity = new Vector2(-Velocity.x, Velocity.y);
                targetPos += Velocity * (1 - t);
                Position = targetPos;
                
                // Call actions
                OnBorderCollision?.Invoke();
            }
            else if (targetPos.x <= -boxSize.x + Radius) // Left boundary
            {
                float t = (Position.x + boxSize.x - Radius) / -Velocity.x;
                float intersectionY = Position.y + Velocity.y * t;
                targetPos = new Vector2(-boxSize.x + Radius, intersectionY);
                Velocity = new Vector2(-Velocity.x, Velocity.y);
                targetPos += Velocity * (1 - t);
                Position = targetPos;
                
                // Call actions
                OnBorderCollision?.Invoke();
            }
            else if (targetPos.y >= boxSize.y - Radius) // Top boundary
            {
                float t = (boxSize.y - Radius - Position.y) / Velocity.y;
                float intersectionX = Position.x + Velocity.x * t;
                targetPos = new Vector2(intersectionX, boxSize.y - Radius);
                Velocity = new Vector2(Velocity.x, -Velocity.y);
                targetPos += Velocity * (1 - t);
                Position = targetPos;
                
                // Call actions
                OnBorderCollision?.Invoke();
            }
            else if (targetPos.y <= -boxSize.y + Radius) // Bottom boundary
            {
                float t = (Position.y + boxSize.y - Radius) / -Velocity.y;
                float intersectionX = Position.x + Velocity.x * t;
                targetPos = new Vector2(intersectionX, -boxSize.y + Radius);
                Velocity = new Vector2(Velocity.x, -Velocity.y);
                targetPos += Velocity * (1 - t);
                Position = targetPos;
                
                // Call actions
                OnBorderCollision?.Invoke();
            }
            else
            {
                Position = targetPos;
            }
        }*/
    }
}