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
            SpaceSettings.SubSteppingSpeed = 0.1f;
            SpaceSettings.SpaceSize = new Vector2(spaceSize.x, spaceSize.y);

            SimulationSpace = new SimulationSpace();
        }

        public void AddEntity(Entity entity)
        {
            SimulationSpace.AddEntity(entity);
        }

        private void Update()
        {
            SpaceSettings.Friction = spaceFriction;
        }

        private void FixedUpdate()
        {
            SimulationSpace.SimulateStep();
        }
    }
}