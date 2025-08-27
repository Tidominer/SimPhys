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
                    if (entity.IsFrozen)
                    {
                        entity.Velocity = Vector2.Zero;
                        continue;
                    }
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
                    if (entities[i].IsFrozen) continue;
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
        /// <param name="origin">The starting point of the ray in world space.</param>
        /// <param name="direction">The normalized direction of the ray.</param>
        /// <param name="maxDistance">The maximum distance the ray should travel.</param>
        /// <param name="hitInfo">If a hit occurs, this contains detailed information about the closest hit.</param>
        /// <param name="ignoreCondition">A function to filter which entities should be ignored by the cast. Return true to ignore.</param>
        public bool Raycast(Vector2 origin, Vector2 direction, decimal maxDistance, out RaycastHit hitInfo, Func<Entity, bool> ignoreCondition = null)
        {
            return _caster.Raycast(origin, direction, maxDistance, out hitInfo, ignoreCondition);
        }

        /// <summary>
        /// Sweeps a circle through the simulation space and checks for intersections.
        /// </summary>
        /// <param name="origin">The starting center position of the circle in world space.</param>
        /// <param name="radius">The radius of the circle to sweep.</param>
        /// <param name="direction">The normalized direction of the sweep.</param>
        /// <param name="maxDistance">The maximum distance the circle should travel.</param>
        /// <param name="hitInfo">If a hit occurs, this contains detailed information about the closest hit.</param>
        /// <param name="ignoreCondition">A function to filter which entities should be ignored by the cast. Return true to ignore.</param>
        public bool CircleCast(Vector2 origin, decimal radius, Vector2 direction, decimal maxDistance, out CircleCastHit hitInfo, Func<Entity, bool> ignoreCondition = null)
        {
            return _caster.CircleCast(origin, radius, direction, maxDistance, out hitInfo, ignoreCondition);
        }

        #endregion
    }
}