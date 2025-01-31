using System;
using SimPhys.Unity;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimulationTest : MonoBehaviour
{
    [SerializeField] private SimPhysEntity[] entities;

    private void Start()
    {
        entities[0].Entity.OnCollisionEnter += (other) => { Debug.Log("In"); };
        entities[0].Entity.OnCollisionExit += (other) => { Debug.Log("Out"); };
    }

    public void AddForce()
    {
        foreach (var entity in entities)
        {
            entity.Entity.Velocity = new System.Numerics.Vector2(Random.Range(-2f,2f), Random.Range(-2f,2f));
        }
    }
}