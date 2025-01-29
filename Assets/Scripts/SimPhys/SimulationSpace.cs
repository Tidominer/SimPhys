using System;
using System.Collections.Generic;
using System.Linq;
using SimPhys.Entities;

namespace SimPhys
{
    public class SimulationSpace
    {
        public List<Entity> Entities { get; } = new List<Entity>();

        public void AddEntity(Entity entity) => Entities.Add(entity);

        public void SimulateStep()
        {
            float maxSpeed = 10.0f;
            int subSteps = (int)Math.Ceiling(Entities.Max(e => e.Velocity.Length()) / maxSpeed);
            subSteps = Math.Max(1, subSteps);
    
            float subStepFriction = (float)Math.Pow(SpaceSettings.Friction, 1.0f / subSteps);

            for (int step = 0; step < subSteps; step++)
            {
                foreach (var entity in Entities)
                {
                    entity.Velocity *= subStepFriction;  // Apply friction per substep
                    entity.Position += entity.Velocity / subSteps;
                }

                for (int i = 0; i < Entities.Count; i++)
                {
                    for (int j = i + 1; j < Entities.Count; j++)
                    {
                        if (Entities[i].Intersects(Entities[j], out var data))
                            Entities[i].ResolveCollision(Entities[j], data);
                    }
                }
            }
        }
    }
}