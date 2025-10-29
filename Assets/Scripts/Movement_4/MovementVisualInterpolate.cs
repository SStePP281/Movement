using UnityEngine;

public class MovementVisualInterpolate : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("Ссылка на объект с физикой (NetSimulate)")]
    [SerializeField] private Transform physicsTarget;
    [Tooltip("Скорость следования за позицией")]
    [SerializeField] private float positionSmoothSpeed = 12.0f;
    [Tooltip("Скорость следования за поворотом")]
    [SerializeField] private float rotationSmoothSpeed = 10.0f;

    private void Awake()
    {
        if (physicsTarget == null)
        {
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, physicsTarget.position, Time.deltaTime * positionSmoothSpeed);

        transform.rotation = Quaternion.Slerp(transform.rotation, physicsTarget.rotation, Time.deltaTime * rotationSmoothSpeed);
    }
}
