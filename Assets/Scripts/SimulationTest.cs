using System;
using System.Collections.Generic;
using SimPhys;
using SimPhys.Entities;
using SimPhys.Unity;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = System.Numerics.Vector2;

public class SimulationTest : MonoBehaviour
{
    [SerializeField] private SimPhysEntity[] entities;
    public SimulationSpace SimulationSpace;
    public Color color;
    [SerializeField] private List<Transform> myTransforms;
    public KeyCode KeyCode;

    private void Awake()
    {
        var forces = new List<Vector2>();
        for (int i = 0; i < 10; i++)
        {
            forces.Add(new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f)));
        }

        Forces = forces.ToArray();
    }

    private void Start()
    {
        entities[0].Entity.OnCollisionEnter += (other) => { Debug.Log("In"); };
        entities[0].Entity.OnCollisionExit += (other) => { Debug.Log("Out"); };

        var settings = new SpaceSettings()
        {
            Friction = 0.98f,
            SpaceSize = new Vector2(10, 10),
            SubSteppingSpeed = 0.1f
        };

        SimulationSpace = new SimulationSpace(settings);
        foreach (var entity in entities)
        {
            SimulationSpace.AddEntity(new Circle
            {
                Position = entity.Entity.Position,
                Velocity = entity.Entity.Velocity,
                Bounciness = entity.Entity.Bounciness,
                Mass = entity.Entity.Mass,
                Radius = ((Circle)entity.Entity).Radius,
                IsTrigger = entity.Entity.IsTrigger,
                IsFrozen = entity.Entity.IsFrozen
            });
        }
    }

    private int _currentSim = 0;

    public void FixedUpdate()
    {
        SimulationSpace.SimulateStep();
        for (var i = 0; i < myTransforms.Count; i++)
        {
            myTransforms[i].position = SimulationSpace.Entities[i].Position.ToUnityVector();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode))
            for (int i = 0; i < 1000; i++)
            {
                SimulationSpace.SimulateStep();
            }
    }

    private void OnDrawGizmos()
    {
        if (SimulationSpace == null) return;
        Gizmos.color = color;
        for (int i = 0; i < SimulationSpace.Entities.Count; i++)
        {
            if (SimulationSpace.Entities[i] is Circle c)
                Gizmos.DrawWireSphere(c.Position.ToUnityVector(), c.Radius*transform.localScale.x);
        }
    }

    public static Vector2[] Forces;

    public void AddForce()
    {
        for (var i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            var velocity = Forces[i];
            entity.Entity.Velocity = velocity;
            SimulationSpace.Entities[i].Velocity = velocity;
        }
    }
}