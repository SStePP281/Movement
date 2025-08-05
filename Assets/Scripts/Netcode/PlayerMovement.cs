using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private const int MOVEMENT_BUFFER_SIZE = 1024;

    [SerializeField] private float reconciliationThreshold = 2.0f;

    //Client Data
    private CircualBuffer<PlayerInput> clientInputBuffer;
    private CircualBuffer<PlayerProcess> clientProcessBuffer;
    private PlayerProcess lastServerProcess;
    private PlayerProcess lastPredictionProcess;
    private int currentTick = 0;
    private float timeBettwenTick;

    //private ClientNetworkTransform clientNetworkTransform;
    private PlayerProcess guestLastProcess;
    [SerializeField] private float maxExtrapolateTime = 1.0f;
    private float currentExtrapolateTime = 0.0f;
    private float lastServerTime = 0.0f;

    //Server Data
    private CircualBuffer<PlayerProcess> serverProcessBuffer;
    private Queue<PlayerInput> serverInputQueue;

    private PlayerTakeMovementInput movementInput;
    private PlayerMovementProcess movementProcess;

    private void Awake()
    {
        movementProcess = GetComponent<PlayerMovementProcess>();
        movementInput = GetComponent<PlayerTakeMovementInput>();
        //clientNetworkTransform = GetComponent<ClientNetworkTransform>();

        clientInputBuffer = new CircualBuffer<PlayerInput>(MOVEMENT_BUFFER_SIZE);
        clientProcessBuffer = new CircualBuffer<PlayerProcess>(MOVEMENT_BUFFER_SIZE);
        serverProcessBuffer = new CircualBuffer<PlayerProcess>(MOVEMENT_BUFFER_SIZE);
        serverInputQueue = new Queue<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        timeBettwenTick = 1.0f / NetworkManager.NetworkTickSystem.TickRate;

        GetComponent<MeshRenderer>().material.color = IsOwner ? Color.green : Color.red;

        NetworkManager.Singleton.NetworkTickSystem.Tick += NetworkManager_OnTick;

        if (!IsOwner && !IsServer)
        {
            //clientNetworkTransform.SyncPositionX = false;
            //clientNetworkTransform.SyncPositionY = false;
            //clientNetworkTransform.SyncPositionZ = false;
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

        float ping = NetworkManager.LocalTime.TimeAsFloat - lastServerTime;

        Debug.Log(ping);

        if (ping >= 0.3f && currentExtrapolateTime < maxExtrapolateTime)
        {
            currentExtrapolateTime += timeBettwenTick;

            guestLastProcess = movementProcess.Extrapolate(lastServerProcess, timeBettwenTick);
        }
        else
        {
            currentExtrapolateTime = 0.0f;

            guestLastProcess = movementProcess.InterpolateClientPosition(lastServerProcess, timeBettwenTick);
        }
    }

    private void HandleClientTick()
    {
        if (!IsClient || !IsOwner) { return; }

        int bufferIndex = currentTick % MOVEMENT_BUFFER_SIZE;

        PlayerInput input = movementInput.GetPlayerInput(currentTick);
        clientInputBuffer.Add(input, bufferIndex);

        SendClientInputToServerRpc(input);

        PlayerProcess process = movementProcess.MovementProcess(input, timeBettwenTick, input.tick);
        clientProcessBuffer.Add(process, bufferIndex);

        HandleServerReconciliation();
    }

    [ServerRpc]
    private void SendClientInputToServerRpc(PlayerInput input)
    {    
        serverInputQueue.Enqueue(input);
    }

    private void HandleServerReconciliation()
    {
        if (!ShouldReconcile()) { return; }

        int bufferIndex = lastServerProcess.tick % MOVEMENT_BUFFER_SIZE;

        if (bufferIndex - 1 < 0) { return; }

        PlayerProcess rewindProcess = IsHost ? serverProcessBuffer.Get(bufferIndex - 1) : lastServerProcess;
        PlayerProcess clientProcess = IsHost ? clientProcessBuffer.Get(bufferIndex) : clientProcessBuffer.Get(bufferIndex);

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

        return isNewServerProcess && isLastProcessUndefinedOrDifferent;
    }

    private void ReconcileProcess(PlayerProcess rewindProcess)
    {
        movementProcess.ReconcileProcess(rewindProcess);

        if (!rewindProcess.Equals(lastServerProcess)) { return; }

        clientProcessBuffer.Add(rewindProcess, rewindProcess.tick % MOVEMENT_BUFFER_SIZE);

        int tickToReplay = lastServerProcess.tick;

        while (tickToReplay < currentTick)
        {
            int bufferIndex = tickToReplay % MOVEMENT_BUFFER_SIZE;

            PlayerInput input = clientInputBuffer.Get(bufferIndex);

            PlayerProcess process = movementProcess.MovementProcess(input, timeBettwenTick, input.tick);

            clientProcessBuffer.Add(process, bufferIndex);

            tickToReplay++;
        }
    }

    private void HandleServerTick()
    {
        if (!IsServer) { return; }

        int bufferIndex = -1;
        PlayerInput playerInput = default;

        while (serverInputQueue.Count > 0)
        {
            playerInput = serverInputQueue.Dequeue();

            bufferIndex = playerInput.tick % MOVEMENT_BUFFER_SIZE;

            PlayerProcess process = movementProcess.SimulateMovement(playerInput, timeBettwenTick, playerInput.tick);
            serverProcessBuffer.Add(process, bufferIndex);
        }

        if (bufferIndex == -1) { return; }

        SendLastServerProcessToClientRpc(serverProcessBuffer.Get(bufferIndex), NetworkManager.ServerTime.TimeAsFloat);
    }

    [ClientRpc]
    private void SendLastServerProcessToClientRpc(PlayerProcess process, float serverTime)
    {
        lastServerProcess = process;
        lastServerTime = serverTime;
    }
}
