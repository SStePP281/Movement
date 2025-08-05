using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;

public class MovementController : NetworkBehaviour
{
    private const int MOVEMENT_BUFFER_SIZE = 1024;

    [SerializeField] private MovementProcessController movementProcess;
    [SerializeField] private float reconciliationThreshold = 2.0f;
    [SerializeField] private float maxExtrapolateTime = 1.0f;

    /*Client params*/
    private CircualBuffer<MovementInputData> clientInputBuffer;
    private CircualBuffer<MovementProcessData> clientProcessBuffer;
    private MovementProcessData lastServerProcess;
    private MovementProcessData lastPredictionProcess;
    private int currentTick = 0;
    private float timeBettwenTick;

    private ClientNetworkTransform clientNetworkTransform;
    private MovementProcessData guestLastProcess;
    private float currentExtrapolateTime = 0.0f;
    private float lastServerTime = 0.0f;

    /*Server params*/
    private CircualBuffer<MovementProcessData> serverProcessBuffer;
    private Queue<MovementInputData> serverInputQueue;

    /*Input params*/
    private RagdollController ragdollController;
    private AnimationController animationController;
    private MovementInputController movementInput;

    private void Awake()
    {
        clientNetworkTransform = GetComponent<ClientNetworkTransform>();

        clientInputBuffer = new CircualBuffer<MovementInputData>(MOVEMENT_BUFFER_SIZE);
        clientProcessBuffer = new CircualBuffer<MovementProcessData>(MOVEMENT_BUFFER_SIZE);
        serverProcessBuffer = new CircualBuffer<MovementProcessData>(MOVEMENT_BUFFER_SIZE);
        serverInputQueue = new Queue<MovementInputData>();

        timeBettwenTick = 1.0f / NetworkManager.NetworkTickSystem.TickRate;
        movementProcess.Initialize(timeBettwenTick);
    }

    public void Initialize(AnimationController _animationController, RagdollController _ragdollController)
    {
        animationController = _animationController;
        ragdollController = _ragdollController;

        movementProcess.OnFalling += (Vector3 vel) => { ragdollController.Enable(true, vel); };
        animationController.SetJumpingVariable(movementProcess.IsJumpAnimation, movementProcess.CombatJumpCount);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        NetworkManager.Singleton.NetworkTickSystem.Tick += NetworkManager_OnTick;

        if (IsOwner)
        {
            movementInput = new MovementInputController();
        }

        if (!IsOwner && !IsServer)
        {
            clientNetworkTransform.SyncPositionX = false;
            clientNetworkTransform.SyncPositionY = false;
            clientNetworkTransform.SyncPositionZ = false;
            clientNetworkTransform.SyncRotAngleX = false;
            clientNetworkTransform.SyncRotAngleY = false;
            clientNetworkTransform.SyncRotAngleZ = false;
        }
    }

    private void NetworkManager_OnTick()
    {
        currentTick++;

        HandleClientTick();

        HandleServerTick();

        HandleGuestTick();
    }

    private void HandleGuestTick()
    {
        if (IsOwner) { return; }

        if (lastServerProcess.position == guestLastProcess.position || lastServerProcess.Equals(default)) { return; }

        float ping = (NetworkManager.LocalTime.TimeAsFloat - lastServerTime) * 1000;

        if (ping >= 200f && currentExtrapolateTime < maxExtrapolateTime)
        {
            currentExtrapolateTime += timeBettwenTick;

            guestLastProcess = movementProcess.ExtrapolateClient(lastServerProcess);
        }
        else
        {
            currentExtrapolateTime = 0.0f;

            guestLastProcess = movementProcess.InterpolateClient(lastServerProcess);
        }
    }

    private void HandleClientTick()
    {
        if (!IsClient || !IsOwner) { return; }

        int bufferIndex = currentTick % MOVEMENT_BUFFER_SIZE;

        MovementInputData input = movementInput.GetInputData(currentTick);
        clientInputBuffer.Add(input, bufferIndex);

        SendClientInputToServerRpc(input);

        MovementProcessData process = movementProcess.MovementProcess(input);
        clientProcessBuffer.Add(process, bufferIndex);

        HandleServerReconciliation();

        animationController.Run(movementProcess.MoveVelocity);
    }

    [ServerRpc]
    private void SendClientInputToServerRpc(MovementInputData input)
    {
        serverInputQueue.Enqueue(input);
    }

    private void HandleServerReconciliation()
    {
        if (!ShouldReconcile()) { return; }

        int bufferIndex = lastServerProcess.tick % MOVEMENT_BUFFER_SIZE;

        if (bufferIndex - 1 < 0) { return; }

        MovementProcessData rewindProcess = IsHost ? serverProcessBuffer.Get(bufferIndex - 1) : lastServerProcess;
        MovementProcessData clientProcess = IsHost ? clientProcessBuffer.Get(bufferIndex) : clientProcessBuffer.Get(bufferIndex);

        float positionError = Vector3.Distance(rewindProcess.position, clientProcess.position);

        if (positionError > reconciliationThreshold)
        {
            ReconcileProcess(rewindProcess);
        }

        lastPredictionProcess = rewindProcess;
    }

    private bool ShouldReconcile()
    {
        bool isNewServerProcess = !lastServerProcess.Equals(default);
        bool isLastProcessUndefinedOrDifferent = lastPredictionProcess.Equals(default) ||
                                                !lastPredictionProcess.Equals(lastServerProcess);

        return isNewServerProcess && isLastProcessUndefinedOrDifferent && !ragdollController.IsActive && !animationController.IsStandUp;
    }

    private void ReconcileProcess(MovementProcessData rewindProcess)
    {
        movementProcess.ReconcileProcess(rewindProcess);

        if (!rewindProcess.Equals(lastServerProcess)) { return; }

        clientProcessBuffer.Add(rewindProcess, rewindProcess.tick % MOVEMENT_BUFFER_SIZE);

        int tickToReplay = lastServerProcess.tick;
        MovementInputData input = default;
        MovementProcessData process = default;

        while (tickToReplay < currentTick)
        {
            int bufferIndex = tickToReplay % MOVEMENT_BUFFER_SIZE;

            input = clientInputBuffer.Get(bufferIndex);

            process = movementProcess.MovementProcess(input);

            clientProcessBuffer.Add(process, bufferIndex);

            tickToReplay++;
        }
    }

    private void HandleServerTick()
    {
        if (!IsServer) { return; }

        int bufferIndex = -1;
        MovementInputData playerInput = default;
        MovementProcessData process = default;

        while (serverInputQueue.Count > 0)
        {
            playerInput = serverInputQueue.Dequeue();

            bufferIndex = playerInput.tick % MOVEMENT_BUFFER_SIZE;

            process = movementProcess.SimulateMovementProcess(playerInput);
            serverProcessBuffer.Add(process, bufferIndex);
        }

        if (bufferIndex == -1) { return; }

        SendLastServerProcessToClientRpc(serverProcessBuffer.Get(bufferIndex), NetworkManager.ServerTime.TimeAsFloat);
    }

    [ClientRpc]
    private void SendLastServerProcessToClientRpc(MovementProcessData process, float serverTime)
    {
        lastServerProcess = process;
        lastServerTime = serverTime;
    }

    public void OnRagdolDisable() => movementProcess.OnRagdolDisable();

    public void OnRagdolEnable() => movementProcess.OnRagdolEnable();

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (IsOwner)
        {
            movementInput.Dispose();
        }
    }
}