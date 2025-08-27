using System;
using SimPhys.Entities;
using System.Collections.Generic;

namespace SimPhys
{
    public class PhysicsCaster
    {
        private readonly List<Entity> entities;

        public PhysicsCaster(List<Entity> entityList)
        {
            this.entities = entityList;
        }

        /// <summary>
        /// Casts a ray against all entities in the simulation space.
        /// </summary>
        public bool Raycast(Vector2 origin, Vector2 direction, decimal maxDistance, out RaycastHit hitInfo)
        {
            hitInfo = null;
            var closestHit = new RaycastHit { Distance = maxDistance };
            bool hasHit = false;

            foreach (var entity in entities)
            {
                if (RaycastVsEntity(origin, direction, entity, out var currentHit) && currentHit.Distance < closestHit.Distance)
                {
                    closestHit = currentHit;
                    hasHit = true;
                }
            }

            if (hasHit)
            {
                hitInfo = closestHit;
            }
            
            return hasHit;
        }

        /// <summary>
        /// Sweeps a circle through the simulation space and checks for intersections.
        /// </summary>
        public bool CircleCast(Vector2 origin, decimal radius, Vector2 direction, decimal maxDistance, out CircleCastHit hitInfo)
        {
            hitInfo = null;
            var closestHit = new CircleCastHit { Distance = maxDistance };
            bool hasHit = false;

            foreach (var entity in entities)
            {
                if (CircleCastVsEntity(origin, radius, direction, entity, out var currentHit) && currentHit.Distance < closestHit.Distance)
                {
                    closestHit = currentHit;
                    hasHit = true;
                }
            }

            if (hasHit)
            {
                hitInfo = closestHit;
            }

            return hasHit;
        }

        #region Casting Helper Methods

        private bool RaycastVsEntity(Vector2 origin, Vector2 direction, Entity entity, out RaycastHit hitInfo)
        {
            hitInfo = null;
            return entity switch
            {
                Circle circle => RaycastVsCircle(origin, direction, circle, out hitInfo),
                Rectangle rect => RaycastVsRectangle(origin, direction, rect, out hitInfo),
                _ => false
            };
        }

        private bool CircleCastVsEntity(Vector2 origin, decimal radius, Vector2 direction, Entity entity, out CircleCastHit hitInfo)
        {
            hitInfo = null;
            return entity switch
            {
                Circle circle => CircleCastVsCircle(origin, radius, direction, circle, out hitInfo),
                Rectangle rect => CircleCastVsRectangle(origin, radius, direction, rect, out hitInfo),
                _ => false
            };
        }

        private bool RaycastVsCircle(Vector2 origin, Vector2 direction, Circle circle, out RaycastHit hitInfo)
        {
            hitInfo = null;
            Vector2 oc = origin - circle.Position;

            decimal a = Vector2.Dot(direction, direction);
            decimal b = 2 * Vector2.Dot(oc, direction);
            decimal c = Vector2.Dot(oc, oc) - circle.Radius * circle.Radius;
            decimal discriminant = b * b - 4 * a * c;

            if (discriminant < 0) return false;

            decimal t = (-b - discriminant.Sqrt()) / (2 * a);
            if (t < 0) t = (-b + discriminant.Sqrt()) / (2 * a);
            
            if (t >= 0 && t <= decimal.MaxValue) // Ensure t is valid
            {
                Vector2 point = origin + direction * t;
                hitInfo = new RaycastHit
                {
                    Distance = t,
                    Point = point,
                    Normal = (point - circle.Position).Normalized(),
                    HitEntity = circle
                };
                return true;
            }
            return false;
        }

        private bool RaycastVsRectangle(Vector2 origin, Vector2 direction, Rectangle rect, out RaycastHit hitInfo)
        {
            hitInfo = null;
            
            decimal cos = (decimal)Math.Cos((double)-rect.Rotation);
            decimal sin = (decimal)Math.Sin((double)-rect.Rotation);
            Vector2 delta = origin - rect.Position;

            Vector2 localOrigin = new Vector2(delta.X * cos - delta.Y * sin, delta.X * sin + delta.Y * cos);
            Vector2 localDir = new Vector2(direction.X * cos - direction.Y * sin, direction.X * sin + direction.Y * cos);

            decimal tNear = decimal.MinValue;
            decimal tFar = decimal.MaxValue;
            Vector2 normal = Vector2.Zero;

            // X-slab test
            if (localDir.X.NearlyEqual(0))
            {
                if (localOrigin.X < -rect.Width / 2 || localOrigin.X > rect.Width / 2) return false;
            }
            else
            {
                decimal t1 = (-rect.Width / 2 - localOrigin.X) / localDir.X;
                decimal t2 = (rect.Width / 2 - localOrigin.X) / localDir.X;
                Vector2 slabNormal = localDir.X < 0 ? new Vector2(1, 0) : new Vector2(-1, 0);

                if (t1 > t2) { var temp = t1; t1 = t2; t2 = temp; }
                if (t1 > tNear) { tNear = t1; normal = slabNormal; }
                if (t2 < tFar) tFar = t2;
                if (tNear > tFar) return false;
            }

            // Y-slab test
            if (localDir.Y.NearlyEqual(0))
            {
                if (localOrigin.Y < -rect.Height / 2 || localOrigin.Y > rect.Height / 2) return false;
            }
            else
            {
                decimal t1 = (-rect.Height / 2 - localOrigin.Y) / localDir.Y;
                decimal t2 = (rect.Height / 2 - localOrigin.Y) / localDir.Y;
                Vector2 slabNormal = localDir.Y < 0 ? new Vector2(0, 1) : new Vector2(0, -1);
                
                if (t1 > t2) { var temp = t1; t1 = t2; t2 = temp; }
                if (t1 > tNear) { tNear = t1; normal = slabNormal; }
                if (t2 < tFar) tFar = t2;
                if (tNear > tFar) return false;
            }

            if (tNear >= 0)
            {
                cos = (decimal)Math.Cos((double)rect.Rotation);
                sin = (decimal)Math.Sin((double)rect.Rotation);
                Vector2 worldNormal = new Vector2(normal.X * cos - normal.Y * sin, normal.X * sin + normal.Y * cos);

                hitInfo = new RaycastHit
                {
                    Distance = tNear,
                    Point = origin + direction * tNear,
                    Normal = worldNormal,
                    HitEntity = rect
                };
                return true;
            }
            return false;
        }

        private bool CircleCastVsCircle(Vector2 origin, decimal radius, Vector2 direction, Circle circle, out CircleCastHit hitInfo)
        {
            hitInfo = null;
            var enlargedCircle = new Circle { Position = circle.Position, Radius = circle.Radius + radius };
            if (RaycastVsCircle(origin, direction, enlargedCircle, out var rayHit))
            {
                Vector2 circlePosAtHit = origin + direction * rayHit.Distance;
                hitInfo = new CircleCastHit
                {
                    Distance = rayHit.Distance,
                    Point = circlePosAtHit + (-rayHit.Normal * radius),
                    Normal = rayHit.Normal,
                    HitEntity = circle,
                    CirclePositionAtHit = circlePosAtHit
                };
                return true;
            }
            return false;
        }

        private bool CircleCastVsRectangle(Vector2 origin, decimal radius, Vector2 direction, Rectangle rect, out CircleCastHit hitInfo)
        {
            hitInfo = null;
            
            decimal cosNegR = (decimal)Math.Cos((double)-rect.Rotation);
            decimal sinNegR = (decimal)Math.Sin((double)-rect.Rotation);
            Vector2 delta = origin - rect.Position;

            Vector2 localOrigin = new Vector2(delta.X * cosNegR - delta.Y * sinNegR, delta.X * sinNegR + delta.Y * cosNegR);
            Vector2 localDir = new Vector2(direction.X * cosNegR - direction.Y * sinNegR, direction.X * sinNegR + direction.Y * cosNegR);
            
            decimal halfWidth = rect.Width / 2;
            decimal halfHeight = rect.Height / 2;
            
            decimal tEnter = 0, tExit = 1;
            decimal t;

            if (localDir.X.NearlyEqual(0))
            {
                if (localOrigin.X - radius > halfWidth || localOrigin.X + radius < -halfWidth) return false;
            }
            else
            {
                t = (-halfWidth - radius - localOrigin.X) / localDir.X;
                decimal t2 = (halfWidth + radius - localOrigin.X) / localDir.X;
                if (t > t2) { var temp = t; t = t2; t2 = temp; }
                tEnter = Math.Max(tEnter, t);
                tExit = Math.Min(tExit, t2);
                if (tEnter > tExit) return false;
            }

            if (localDir.Y.NearlyEqual(0))
            {
                if (localOrigin.Y - radius > halfHeight || localOrigin.Y + radius < -halfHeight) return false;
            }
            else
            {
                t = (-halfHeight - radius - localOrigin.Y) / localDir.Y;
                decimal t2 = (halfHeight + radius - localOrigin.Y) / localDir.Y;
                if (t > t2) { var temp = t; t = t2; t2 = temp; }
                tEnter = Math.Max(tEnter, t);
                tExit = Math.Min(tExit, t2);
                if (tEnter > tExit) return false;
            }

            if (tEnter >= 0 && tEnter < 1)
            {
                Vector2 circlePosAtHit = origin + direction * tEnter;
                
                Vector2 localCirclePosAtHit = localOrigin + localDir * tEnter;
                Vector2 closestPointOnAABB = new Vector2(
                    Math.Max(-halfWidth, Math.Min(localCirclePosAtHit.X, halfWidth)),
                    Math.Max(-halfHeight, Math.Min(localCirclePosAtHit.Y, halfHeight))
                );
                Vector2 localNormal = (localCirclePosAtHit - closestPointOnAABB).Normalized();
                
                decimal cosR = (decimal)Math.Cos((double)rect.Rotation);
                decimal sinR = (decimal)Math.Sin((double)rect.Rotation);
                Vector2 worldNormal = new Vector2(localNormal.X * cosR - localNormal.Y * sinR, localNormal.X * sinR + localNormal.Y * cosR);

                hitInfo = new CircleCastHit
                {
                    Distance = tEnter,
                    Point = circlePosAtHit - worldNormal * radius,
                    Normal = worldNormal,
                    HitEntity = rect,
                    CirclePositionAtHit = circlePosAtHit
                };
                return true;
            }

            return false;
        }

        #endregion
    }
}