using UnityEngine;

public class MovePlatform : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private Vector3 moveDir = Vector3.zero;
    [SerializeField] private float rotationSpeed = 15.0f;
    [SerializeField] private Vector3 rotationDir = Vector3.zero;

    private Vector3 baseY;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        baseY = transform.position.y * Vector3.one;
    }

    private void FixedUpdate()
    {
        body.velocity = moveDir * moveSpeed * Time.fixedDeltaTime;

        transform.position = new Vector3(transform.position.x, baseY.y, transform.position.z);
    }
}
