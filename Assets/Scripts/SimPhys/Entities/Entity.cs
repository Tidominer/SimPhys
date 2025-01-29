using System;
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

        public abstract bool Intersects(Entity other); 
        
        public Action OnBorderCollision { get; set; }
        public Action<Entity> OnEntityCollision { get; set; }
    }
}
