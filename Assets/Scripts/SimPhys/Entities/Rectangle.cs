using System;

namespace SimPhys.Entities
{
    public class Rectangle : Entity
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public override bool CheckCollision(Entity other, float deltaTime, out CollisionManifold manifold)
        {
            manifold = new CollisionManifold();
            return other switch
            {
                Circle circle => circle.CheckRectangleCollision(this, deltaTime, ref manifold),
                Rectangle rect => CheckRectangleCollision(rect, deltaTime, ref manifold),
                _ => false
            };
        }

        private bool CheckRectangleCollision(Rectangle other, float deltaTime, ref CollisionManifold manifold)
        {
            Vector2 relVelocity = Velocity - other.Velocity;
            float tFirst = 0;
            float tLast = deltaTime;

            Vector2 aMin = Position;
            Vector2 aMax = Position + new Vector2(Width, Height);
            Vector2 bMin = other.Position;
            Vector2 bMax = other.Position + new Vector2(other.Width, other.Height);

            for (int axis = 0; axis < 2; axis++)
            {
                if (relVelocity.GetAxis(axis) == 0)
                {
                    if (aMax.GetAxis(axis) < bMin.GetAxis(axis) || aMin.GetAxis(axis) > bMax.GetAxis(axis))
                        return false;
                    continue;
                }

                float t0 = (bMin.GetAxis(axis) - aMax.GetAxis(axis)) / relVelocity.GetAxis(axis);
                float t1 = (bMax.GetAxis(axis) - aMin.GetAxis(axis)) / relVelocity.GetAxis(axis);
            
                if (relVelocity.GetAxis(axis) < 0)
                    (t0, t1) = (t1, t0);
            
                tFirst = Math.Max(tFirst, t0);
                tLast = Math.Min(tLast, t1);
            
                if (tFirst > tLast) return false;
            }

            if (tFirst > deltaTime) return false;

            Vector2 normal = new Vector2(
                aMax.X < bMin.X ? 1 : aMin.X > bMax.X ? -1 : 0,
                aMax.Y < bMin.Y ? 1 : aMin.Y > bMax.Y ? -1 : 0
            ).Normalized();

            manifold.EntityA = this;
            manifold.EntityB = other;
            manifold.Normal = normal;
            manifold.Time = tFirst;
            return true;
        }
    }
}