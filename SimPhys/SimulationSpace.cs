using System;
using System.Collections.Generic;
using SimPhys.Entities;

namespace SimPhys
{
    public class SimulationSpace
    {
        public List<Entity> Entities { get; } = new List<Entity>();
        public SpaceSettings SpaceSettings;
        
        private readonly PhysicsCaster _caster;

        public SimulationSpace(SpaceSettings settings)
        {
            SpaceSettings = settings;
            _caster = new PhysicsCaster(this.Entities);
            Entities = new List<Entity>();
        }

        /// <summary>
        /// Adds a new entity to the simulation space.
        /// </summary>
        /// <param name="entity">The entity to add to the simulation.</param>
        public void AddEntity(Entity entity)
        {
            if (!Entities.Contains(entity))
                Entities.Add(entity);
        }

        /// <summary>
        /// Removes an entity from the simulation space.
        /// </summary>
        /// <param name="entity">The entity to remove from the simulation.</param>
        public void RemoveEntity(Entity entity)
        {
            Entities.Remove(entity);
        }

        /// <summary>
        /// Advances the simulation by one fixed step, processing all physics calculations.
        /// This includes applying forces, handling collisions, and updating entity positions
        /// over a series of smaller, discrete substeps for improved accuracy.
        /// </summary>
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
        
        #region Public Casting API (Façade)

        /// <summary>
        /// Casts a ray against all entities in the simulation space.
        /// </summary>
        public bool Raycast(Vector2 origin, Vector2 direction, decimal maxDistance, out RaycastHit hitInfo)
        {
            return _caster.Raycast(origin, direction, maxDistance, out hitInfo);
        }

        /// <summary>
        /// Sweeps a circle through the simulation space and checks for intersections.
        /// </summary>
        public bool CircleCast(Vector2 origin, decimal radius, Vector2 direction, decimal maxDistance, out CircleCastHit hitInfo)
        {
            return _caster.CircleCast(origin, radius, direction, maxDistance, out hitInfo);
        }

        #endregion
    }
}