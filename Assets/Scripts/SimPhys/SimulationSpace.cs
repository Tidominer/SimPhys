using System;
using System.Collections.Generic;
using SimPhys.Entities;
using UnityEngine;

namespace SimPhys
{
    public class SimulationSpace
    {
        public List<Entity> Entities { get; } = new List<Entity>();
        public float Friction { get; set; } = 0.1f;

        public void AddEntity(Entity entity) => Entities.Add(entity);

        public void SimulateStep(float deltaTime)
        {
            const int maxIterations = 5;
            float timeRemaining = deltaTime;
    
            for (int i = 0; i < maxIterations && timeRemaining > 0; i++)
            {
                CollisionManifold earliest = FindEarliestCollision(timeRemaining);
                float stepTime = earliest?.Time ?? timeRemaining;

                MoveEntities(stepTime);
                ApplyFriction(stepTime);
                timeRemaining -= stepTime;

                if (earliest != null)
                {
                    CollisionResolver.Resolve(earliest);
                    // Re-check collisions after resolution
                    timeRemaining += stepTime * 0.1f; // Add some time back for micro-collisions
                }
            }

            // Clamp positions to valid numbers
            foreach (var entity in Entities)
            {
                if (float.IsNaN(entity.Position.X)) entity.Position = new Vector2(0, 0);
                if (float.IsNaN(entity.Position.Y)) entity.Position = new Vector2(0, 0);
            }
        }

        private CollisionManifold FindEarliestCollision(float deltaTime)
        {
            CollisionManifold earliest = null;
            float minTime = deltaTime;

            foreach (var pair in GetEntityPairs())
            {
                if (pair.Item1.CheckCollision(pair.Item2, deltaTime, out var manifold) &&
                    manifold.Time < minTime)
                {
                    minTime = manifold.Time;
                    earliest = manifold;
                }
            }
            return earliest;
        }

        private IEnumerable<Tuple<Entity, Entity>> GetEntityPairs()
        {
            for (int i = 0; i < Entities.Count; i++)
            for (int j = i + 1; j < Entities.Count; j++)
                yield return Tuple.Create(Entities[i], Entities[j]);
        }

        private void MoveEntities(float deltaTime)
        {
            foreach (var entity in Entities)
                entity.Position += entity.Velocity * deltaTime;
        }

        private void ApplyFriction(float deltaTime)
        {
            float frictionFactor = (float)Math.Pow(1 - Friction, deltaTime);
            foreach (var entity in Entities)
                entity.Velocity *= frictionFactor;
        }
    }
}