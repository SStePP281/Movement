using Unity.Netcode;
using UnityEngine;

public struct PlayerProcess : INetworkSerializable
{
    public int tick;
    public Vector3 position;
    public Vector3 velocity;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref tick);
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref velocity);
    }
}

public class PlayerMovementProcess : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10.0f;

    [SerializeField] private float baseInterpolateSpeed = 10.0f;
    [SerializeField] private float maxExtrapolateDistance = 2.0f;
    [SerializeField] private Transform extr;
    private Vector3 smoothInterpolateVelocity = Vector3.zero;
    private Vector3 smoothExtrapolateVelocity = Vector3.zero;

    private Rigidbody rb;

    private PlayerProcess process;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public PlayerProcess MovementProcess(PlayerInput input, float deltaTime, int tick)
    {
        Vector3 velocity = (Vector3.right * input.keyboardInput.x + Vector3.forward * input.keyboardInput.y) * moveSpeed;

        Vector3 movement = velocity * deltaTime;

        Vector3 newPosition = rb.position + movement;

        rb.MovePosition(newPosition);

        process.position = newPosition;
        process.velocity = velocity;
        process.tick = tick;

        return process;
    }

    public PlayerProcess GetCurrenrProcessData() => process;

    public PlayerProcess SimulateMovement(PlayerInput input, float deltaTime, int tick)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Vector3 velocity = (Vector3.right * input.keyboardInput.x + Vector3.forward * input.keyboardInput.y) * moveSpeed;

            Vector3 position = rb.position;

            return new PlayerProcess
            {
                tick = tick,
                position = position,
                velocity = velocity
            };
        }

        Physics.simulationMode = SimulationMode.Script;

        PlayerProcess process = MovementProcess(input, deltaTime, tick);

        Physics.Simulate(deltaTime);

        Physics.simulationMode = SimulationMode.FixedUpdate;

        return process;
    }

    public void ReconcileProcess(PlayerProcess rewindProcess)
    {
        transform.position = rewindProcess.position;
        rb.velocity = rewindProcess.velocity;
    }

    public PlayerProcess InterpolateClientPosition(PlayerProcess process, float deltaTime)
    {
        float smoothTime = 1.0f / (baseInterpolateSpeed + process.velocity.magnitude);

        Vector3 targetPosition = Vector3.SmoothDamp(transform.position, process.position, ref smoothInterpolateVelocity, smoothTime, Mathf.Infinity, deltaTime);

        rb.MovePosition(targetPosition);

        return new PlayerProcess { position = targetPosition, tick = process.tick, velocity = rb.velocity };
    }

    public PlayerProcess Extrapolate(PlayerProcess process, float deltaTime)
    {
        Vector3 displacement = process.velocity * deltaTime;

        if (displacement.magnitude > maxExtrapolateDistance)
        {
            displacement = displacement.normalized * maxExtrapolateDistance;

            Debug.Log("dist");
        }

        Vector3 extrapolatePosition = process.position + displacement;

        float smoothTime = 1.0f / (baseInterpolateSpeed + 1 + process.velocity.magnitude);

        Vector3 targetPosition = Vector3.SmoothDamp(transform.position, extrapolatePosition, ref smoothExtrapolateVelocity, smoothTime, Mathf.Infinity, deltaTime);

        rb.MovePosition(targetPosition);

        return new PlayerProcess { position = targetPosition, tick = process.tick, velocity = process.velocity };
    }
}
