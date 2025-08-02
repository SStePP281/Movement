using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Properties;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    private const int MOVEMENT_BUFFER_SIZE = 1024;

    [SerializeField] private MovementProcess movementProcess;
    [SerializeField] private float reconciliationThreshold = 2.0f;

    /*Client params*/
    private CircualBuffer<PlayerInput> clientInputBuffer;
    private CircualBuffer<PlayerProcess> clientProcessBuffer;
    private PlayerProcess lastServerProcess;
    private PlayerProcess lastPredictionProcess;
    private int currentTick = 0;
    private float timeBettwenTick;

    private ClientNetworkTransform clientNetworkTransform;
    private PlayerProcess guestLastProcess;
    [SerializeField] private float maxExtrapolateTime = 1.0f;
    private float currentExtrapolateTime = 0.0f;
    private float lastServerTime = 0.0f;

    /*Server params*/
    private CircualBuffer<PlayerProcess> serverProcessBuffer;
    private Queue<PlayerInput> serverInputQueue;

    /*Input params*/
    private MovementInputController movementInput;

    private void Awake()
    {
        movementInput = new MovementInputController();
        clientInputBuffer = new CircualBuffer<PlayerInput>(MOVEMENT_BUFFER_SIZE);
        clientProcessBuffer = new CircualBuffer<PlayerProcess>(MOVEMENT_BUFFER_SIZE);
        serverProcessBuffer = new CircualBuffer<PlayerProcess>(MOVEMENT_BUFFER_SIZE);
        serverInputQueue = new Queue<PlayerInput>();

        clientNetworkTransform = GetComponent<ClientNetworkTransform>();
    }

    public void Initialize()
    {
        movementProcess.Initialize(Time.fixedDeltaTime);
    }

    private void FixedUpdate()
    {
        movementProcess.MovementUpdate(movementInput.MovementInputData);
    }

    private void OnDestroy()
    {
        movementInput.Dispose();
    }
}