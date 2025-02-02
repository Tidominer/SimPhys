using System;
using SimPhys.Entities;
using UnityEngine;

namespace SimPhys.Unity
{
    public class SimPhysRectangle : SimPhysEntity
    {
        public Rectangle Rectangle;
        public Vector2 size = Vector2.one;

        private void Awake()
        {
            Transform = transform;
            Rectangle = new Rectangle
            {
                Position = new System.Numerics.Vector2(Transform.position.x, Transform.position.y),
                Bounciness = bounciness,
                Mass = mass,
                Velocity = System.Numerics.Vector2.Zero,
                Width = size.x,
                Height = size.y,
                IsFrozen = frozen,
                IsTrigger = trigger
            };
            Entity = Rectangle;
        }

        private void Update()
        {
            Transform.position = new Vector3(Rectangle.Position.X, Rectangle.Position.Y);
            Rectangle.Bounciness = bounciness;
            Rectangle.Mass = mass;
            Rectangle.Width = size.x;
            Rectangle.Height = size.y;
            Rectangle.IsFrozen = frozen;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 1));
        }
    }
}