using SimPhys.Unity;
using UnityEngine;

public class ShootableEntities : MonoBehaviour
{
    public SimPhysEntity selectedEntity;
    public Vector3 selectedMousePos;
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (!_camera)
            return;
            
        if (Input.GetMouseButtonDown(0))
        {

            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            var hit = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit && hit.transform.TryGetComponent(out SimPhysEntity entity))
            {
                selectedEntity = entity;
                selectedMousePos = Input.mousePosition;
            }

        }

        if (Input.GetMouseButtonUp(0))
        {
            if (selectedEntity)
            {
                var mouse = Input.mousePosition;
                var velocity = (selectedMousePos - mouse) * 0.01f;
                selectedEntity.Entity.Velocity = new System.Numerics.Vector2(velocity.x, velocity.y);
                selectedEntity = null;
            }
        }
    }
}