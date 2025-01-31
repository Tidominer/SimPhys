using System;
using System.Collections.Generic;
using System.Linq;
using SimPhys.Entities;
using System.Numerics;

namespace SimPhys
{
    public class SimulationSpace
    {
        public List<Entity> Entities { get; } = new List<Entity>();

        public void AddEntity(Entity entity) => Entities.Add(entity);

        public void SimulateStep()
        {
            if (Entities.Count == 0) return;
            
            float maxSpeed = SpaceSettings.SubSteppingSpeed;
            int subSteps = (int)Math.Ceiling(Entities.Max(e => e.Velocity.Length()) / maxSpeed);
            subSteps = Math.Max(1, subSteps);
    
            float subStepFriction = (float)Math.Pow(SpaceSettings.Friction, 1.0f / subSteps);

            for (int step = 0; step < subSteps; step++)
            {
                foreach (var entity in Entities)
                {
                    entity.Step();
                    //Freeze system
                    if (entity.IsFrozen) entity.Velocity = Vector2.Zero;
                    if (entity.Velocity.Length() < 0.000001f) entity.Velocity = Vector2.Zero;
                    
                    entity.Velocity *= subStepFriction;  // Apply friction per substep
                    entity.Position += entity.Velocity / subSteps;
                    
                    entity.ResolveBorderCollision(-SpaceSettings.SpaceSize.X, SpaceSettings.SpaceSize.X, -SpaceSettings.SpaceSize.Y, SpaceSettings.SpaceSize.Y);
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