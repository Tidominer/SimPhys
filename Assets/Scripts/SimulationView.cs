using System.Collections.Generic;
using SimPhys;
using SimPhys.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimulationView : MonoBehaviour
{
    [SerializeField] private GameObject circlePrefab, squarePrefab;
    private List<Transform> _entityViews;

    private SimulationSpace[] _simulators;
    private int _currentSim;

    private void Start()
    {
        //World settings
        SpaceSettings.Friction = new System.Numerics.Vector2(0.9f, 0.9f);
        SpaceSettings.SpaceSize = new System.Numerics.Vector2(20, 20);

        _entityViews = new List<Transform>();
        
        _simulators = new SimulationSpace[100];
        for (int i = 0; i < 100; i++)
        {
            _simulators[i] = new SimulationSpace();
        }
        
        for (int i = 0; i < 10; i++)
        {
            var entityView = Instantiate(circlePrefab);
            _entityViews.Add(entityView.transform);

            var pos = new System.Numerics.Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f));
            var rot = new System.Numerics.Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            for (int s = 0; s < 100; s++)
            {
                var entity = new Circle
                {
                    Position = pos,
                    Velocity = rot,
                    Radius = .5f,
                    Mass = 1,
                    Bounciness = 1
                };
                
                _simulators[s].AddEntity(entity);
            }
        }
        
        //add box
        var sq1 = Instantiate(squarePrefab);
        _entityViews.Add(sq1.transform);
        var poss = new System.Numerics.Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f)); 
        for (int s = 0; s < 100; s++)
        {
            var sq1E = new Rectangle
            {
                Position = poss,
                Velocity = System.Numerics.Vector2.Zero,
                Width = 2,
                Height = 1,
                Mass = 1,
                Bounciness = 1
            };
            _simulators[s].AddEntity(sq1E);
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < _simulators.Length; i++)
        {
            _simulators[i].SimulateStep();
        }
        for (int j = 0; j < _simulators[_currentSim].Entities.Count; j++)
        {
            _entityViews[j].position = new Vector3(_simulators[_currentSim].Entities[j].Position.X, _simulators[_currentSim].Entities[j].Position.Y);

            if (_simulators[_currentSim].Entities[j] is Rectangle r)
            {
                _entityViews[j].localScale = new Vector3(r.Width, r.Height, 1);
            }
        }
    }

    public void AddVelocity()
    {
        var velocities = new System.Numerics.Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            velocities[i] = new System.Numerics.Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
        }
        foreach (var simulator in _simulators)
        {
            for (var i = 0; i < simulator.Entities.Count; i++)
            {
                simulator.Entities[i].Velocity = velocities[i];
            }
        }
    }

    private int _selectedEntity = -1;
    private Vector3 _selectionMousePos;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddVelocity();
        }else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _currentSim++;
            if (_currentSim >= _simulators.Length)
                _currentSim = 0;
        }else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _currentSim--;
            if (_currentSim < 0)
                _currentSim = _simulators.Length-1;
        }

        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var hit = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit)
            {
                var index = _entityViews.IndexOf(hit.transform);
                _selectedEntity = index;
                _selectionMousePos = Input.mousePosition;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_selectedEntity != -1)
            {
                var mouse = Input.mousePosition;
                for (int i = 0; i < _simulators.Length; i++)
                {
                    var velocity = (_selectionMousePos - mouse) * 0.01f;
                    _simulators[i].Entities[_selectedEntity].Velocity = new System.Numerics.Vector2(velocity.x, velocity.y);
                }
            }
        }
    }
}