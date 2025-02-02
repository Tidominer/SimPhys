using System;
using SimPhys.Entities;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace SimPhys.Unity
{
    public class SimPhysSpace : MonoBehaviour
    {
        public static SimPhysSpace Instance { get; private set; }

        public SimulationSpace SimulationSpace { get; private set; }
        public float spaceFriction = 0.98f;
        public UnityEngine.Vector2 spaceSize; 

        private void Awake()
        {
            Instance = this;
            var settings = new SpaceSettings()
            {
                SubSteppingSpeed = 0.1f,
                SpaceSize = new Vector2(spaceSize.x, spaceSize.y),
                Friction = spaceFriction
            };

            SimulationSpace = new SimulationSpace(settings);
        }

        public void AddEntity(Entity entity)
        {
            SimulationSpace.AddEntity(entity);
        }

        public void RemoveEntity(Entity entity)
        {
            SimulationSpace.RemoveEntity(entity);
        }

        private void FixedUpdate()
        {
            SimulationSpace.SimulateStep();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(Vector3.zero, spaceSize*2);
        }
    }
}