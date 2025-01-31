using System;
using SimPhys.Entities;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace SimPhys.Unity
{
    public class SimPhysCircle : SimPhysEntity
    {
        public Circle Circle;
        public float radius = 1;

        private void Awake()
        {
            Transform = transform;
            Circle = new Circle
            {
                Position = new Vector2(Transform.position.x, Transform.position.y),
                Bounciness = bounciness,
                Mass = mass,
                Velocity = Vector2.Zero,
                Radius = radius,
                IsFrozen = frozen,
                IsTrigger = trigger
            };
            Entity = Circle;
        }

        private void Update()
        {
            Transform.position = new Vector3(Circle.Position.X, Circle.Position.Y);
            Circle.Bounciness = bounciness;
            Circle.Mass = mass;
            Circle.Radius = radius;
            Circle.IsFrozen = frozen;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}