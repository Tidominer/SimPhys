using System;
using UnityEngine;

namespace SimPhys.Unity
{
    public class SimPhysEntity : MonoBehaviour
    {
        public Entities.Entity Entity;
        public float mass = 1;
        public float bounciness = 1;
        public bool frozen;
        public bool trigger;
        
        protected Transform Transform;

        private void Awake()
        {
            Transform = transform;
        }

        private void Start()
        {
            SimPhysSpace.Instance.AddEntity(Entity);
        }
    }
}