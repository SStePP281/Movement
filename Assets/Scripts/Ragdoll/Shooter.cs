using UnityEngine;

public class Shooter : MonoBehaviour
{
    private const int LeftMouseButton = 0;

    [SerializeField, Range(0, 10000)] private float force;

    private new Camera camera;

    private void Awake()
    {
        camera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(LeftMouseButton))
        {
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Enemy[] enemys = hit.collider.GetComponentsInParent<Enemy>();

                if (enemys.Length > 0)
                {
                    Enemy enemy = enemys[0];

                    Vector3 forceDirection = (hit.point - camera.transform.position).normalized;
                    forceDirection.y = 0;

                    enemy.TakeDamage(forceDirection * force, hit.point);
                }
            }
        }
    }
}
