using System;
using System.Collections.Generic;

namespace SimPhys.Entities
{
    public abstract class Entity
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public decimal Bounciness { get; set; } = 0.5m;
        public decimal Mass { get; set; } = 1;
        public decimal InverseMass => Mass <= 0 ? 0 : 1 / Mass;
        public bool IsFrozen { get; set; }
        public bool IsTrigger { get; set; }
        
        public const decimal Epsilon = 0.000000001m;

        public Action<Entity> OnCollisionEnter = delegate { };
        public Action<Entity> OnCollisionStep = delegate { };
        public Action<Entity> OnCollisionExit = delegate { };

        public abstract bool Intersects(Entity other, out CollisionData? collisionData);
        public abstract void ResolveCollision(Entity other, CollisionData collisionData);
        public abstract void ForceResolveCollision(Entity other, CollisionData collisionData);
        
        internal int substepCount;
        internal const decimal MinVelocityThreshold = 0.000001m;
        internal bool hasCollidedThisStep;

        public void Step(int newSubstepCount, decimal subStepFriction)
        {
            substepCount = newSubstepCount;
            
            //Freeze system
            if (IsFrozen)
                Velocity = Vector2.Zero;
            
            // If velocity is too small to be visually meaningful, just zero it out
            if (Velocity.LengthSquared() < MinVelocityThreshold)
                Velocity = Vector2.Zero;
            
            //Apply velocity if there has been no collisions
            if (!hasCollidedThisStep)
                Position += Velocity / substepCount;
            
            //Friction
            Velocity *= subStepFriction;
            
            //Handle Collisions
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

            hasCollidedThisStep = false;
        }
        
        public readonly List<Entity> currentStepCollisions = new List<Entity>();
        public readonly List<Entity> enteredCollisions = new List<Entity>();
    }
}
