using System.Collections.Generic;
using SimPhys.Entities;

namespace SimPhys
{
    public class SimulationSpace
    {
        public List<Entity> Entities { get; } = new List<Entity>();
        public CollisionSystem CollisionSystem { get; set; } = new();

        public void AddEntity(Entity entity)
        {
            Entities.Add(entity);
        }

        public void SimulateStep()
        {
            // Apply Velocity (implement)
                
            // Apply Friction from SpaceSettings.Friction
            
            // Check and Resolve collision
            for (var i = 0; i < Entities.Count; i++)
            {
                
                Entities[i].Velocity *= SpaceSettings.Friction;
            }
            
            CollisionSystem.CheckCollisions(Entities);
        }
    }
}