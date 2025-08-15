using System;
using System.Collections.Generic;
using SimPhys.Entities;

namespace SimPhys
{
    public class SimulationSpace
    {
        public List<Entity> Entities { get; } = new List<Entity>();
        public SpaceSettings SpaceSettings;

        public SimulationSpace(SpaceSettings settings)
        {
            SpaceSettings = settings;
        }

        public void AddEntity(Entity entity)
        {
            if (!Entities.Contains(entity))
                Entities.Add(entity);
        }

        public void RemoveEntity(Entity entity) => Entities.Remove(entity);

        public void SimulateStep()
        {
            if (Entities.Count == 0) return;
            
            var subSteps = Math.Max(1, SpaceSettings.SubStepsCount);

            decimal subStepFriction = (decimal)Math.Pow(SpaceSettings.Friction, 1.0f / subSteps);

            var entities = Entities.ToArray();
            for (int step = 0; step < subSteps; step++)
            {
                foreach (var entity in entities)
                {
                    entity.Step();
                    //Freeze system
                    if (entity.IsFrozen) entity.Velocity = Vector2.Zero;
                    //if (entity.Velocity.Length() < 0.000001f) entity.Velocity = Vector2.Zero;

                    entity.Velocity *= subStepFriction; // Apply friction per substep
                    entity.Position += entity.Velocity / subSteps;

                    //Border Collision Resolution
                    if (!entity.IsFrozen && SpaceSettings.SpaceSize != null)
                        entity.ResolveBorderCollision(-SpaceSettings.SpaceSize.Value.X, SpaceSettings.SpaceSize.Value.X,
                            -SpaceSettings.SpaceSize.Value.Y, SpaceSettings.SpaceSize.Value.Y);
                }

                for (int i = 0; i < entities.Length; i++)
                {
                    for (int j = i + 1; j < entities.Length; j++)
                    {
                        if (entities[i].Intersects(entities[j], out var data))
                        {
                            entities[i].ResolveCollision(entities[j], data);
                            entities[i].ForceResolveCollision(entities[j], data);
                        }
                    }
                }
            }
        }
    }
}