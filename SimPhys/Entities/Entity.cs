using System;
using System.Collections.Generic;
using System.Numerics;

namespace SimPhys.Entities
{
    public abstract class Entity
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Bounciness { get; set; }
        public float Mass { get; set; }
        public float InverseMass => Mass <= 0 ? 0 : 1f / Mass;
        public bool IsFrozen { get; set; }
        public bool IsTrigger { get; set; }
        

        public Action<Entity> OnCollisionEnter = delegate { };
        public Action<Entity> OnCollisionStep = delegate { };
        public Action<Entity> OnCollisionExit = delegate { };

        public abstract bool Intersects(Entity other, out CollisionData collisionData);
        public abstract void ResolveCollision(Entity other, CollisionData collisionData);
        public abstract void ResolveBorderCollision(float minX, float maxX, float minY, float maxY);

        public void Step()
        {
            foreach (var collision in currentStepCollisions)
            {
                if (!enteredCollisions.Contains(collision))
                {
                    enteredCollisions.Add(collision);
                    OnCollisionEnter?.Invoke(collision);
                }
                else
                {
                    OnCollisionStep?.Invoke(collision);
                }
            }
            
            currentStepCollisions.Clear();
        }
        
        public readonly List<Entity> currentStepCollisions = new List<Entity>();
        public readonly List<Entity> enteredCollisions = new List<Entity>();
    }
}
