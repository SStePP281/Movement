using Unity.Netcode;
using UnityEngine;

public class NewMovementController : NetworkBehaviour
{
    private const int MOVEMENT_BUFFER_SIZE = 1024;

    [SerializeField] private NewMovementProcessController movementProcess;
    [SerializeField] private float reconciliationThreshold = 2.0f;
    [SerializeField] private float maxExtrapolateTime = 1.0f;

    /*Client params*/
    private CircularBuffer<MovementInputData> clientInputBuffer;
    private CircularBuffer<MovementStateData> clientProcessBuffer;
    private MovementStateData lastServerProcess;
    private MovementStateData lastPredictionProcess;
    private int currentTick = 0;
    private float timeBetweenTick;

    private bool hasReceivedServerState = false;
    private float currentExtrapolateTime = 0.0f;
    private float lastServerTime = 0.0f;

    /*Server params*/
    private CircularBuffer<MovementStateData> serverProcessBuffer;
    private MovementInputData[] serverInputBuffer;
    private int lastProcessedServerTick = -1;
    private MovementInputData lastReceivedInput;

    private NewMovementInputController movementInput;

    private void Awake()
    {
        clientInputBuffer = new CircularBuffer<MovementInputData>(MOVEMENT_BUFFER_SIZE);
        clientProcessBuffer = new CircularBuffer<MovementStateData>(MOVEMENT_BUFFER_SIZE);
        serverProcessBuffer = new CircularBuffer<MovementStateData>(MOVEMENT_BUFFER_SIZE);
        serverInputBuffer = new MovementInputData[MOVEMENT_BUFFER_SIZE];
        lastPredictionProcess = new MovementStateData();

        timeBetweenTick = 1.0f / NetworkManager.NetworkTickSystem.TickRate;
        movementProcess.Initialize(timeBetweenTick);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        NetworkManager.Singleton.NetworkTickSystem.Tick += NetworkManager_OnTick;

        if (IsOwner)
        {
            movementInput = new NewMovementInputController();
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
        if (IsServer)
        {
            Debug.Log(IsOwner || !hasReceivedServerState);
        }

        if (IsOwner || !hasReceivedServerState) { return; }

        float timeSinceLastUpdate = NetworkManager.LocalTime.TimeAsFloat - lastServerTime;

        float expectedInterval = timeBetweenTick * 1.5f;
        bool isPacketLoss = timeSinceLastUpdate > expectedInterval;

        if (isPacketLoss && currentExtrapolateTime < maxExtrapolateTime)
        {
            currentExtrapolateTime += timeBetweenTick;
            movementProcess.ApplyPredictedState(lastServerProcess);
        }
        else
        {
            currentExtrapolateTime = 0f;
            movementProcess.ApplyServerState(lastServerProcess);
        }
    }

    private void HandleClientTick()
    {
        if (!IsClient || !IsOwner) return;

        int bufferIndex = currentTick % MOVEMENT_BUFFER_SIZE;

        MovementInputData input = movementInput.GetInputData(currentTick);
        clientInputBuffer.Add(input, bufferIndex);

        SendClientInputToServerRpc(input);

        MovementStateData process = movementProcess.MovementProcess(input);
        clientProcessBuffer.Add(process, bufferIndex);
        lastPredictionProcess = process;

        HandleServerReconciliation();
    }

    [ServerRpc]
    private void SendClientInputToServerRpc(MovementInputData input)
    {
        if (input.tick <= 0) return;

        int index = input.tick % MOVEMENT_BUFFER_SIZE;
        serverInputBuffer[index] = input;
        lastReceivedInput = input;
    }

    private void HandleServerReconciliation()
    {
        if (!ShouldReconcile()) return;

        MovementStateData clientProcessAtServerTick = clientProcessBuffer.Get(lastServerProcess.tick);

        float positionError = Vector3.Distance(clientProcessAtServerTick.position, lastServerProcess.position);

        if (positionError > reconciliationThreshold)
        {
            ReconcileProcess(lastServerProcess);
        }
    }

    private bool ShouldReconcile()
    {
        if (!hasReceivedServerState)
            return false;

        if (lastServerProcess.tick > currentTick + 3) { return false; }

        MovementStateData clientProcessAtServerTick = clientProcessBuffer.Get(lastServerProcess.tick);

        if (clientProcessAtServerTick.tick != lastServerProcess.tick)
            return false;

        float positionError = Vector3.Distance(clientProcessAtServerTick.position, lastServerProcess.position);
        float rotationError = Quaternion.Angle(clientProcessAtServerTick.rotation, lastServerProcess.rotation);

        bool isDifferent = positionError > 0.01f || rotationError > 1f;

        return isDifferent;
    }

    private void ReconcileProcess(MovementStateData serverProcess)
    {
        movementProcess.ReconcileProcess(serverProcess);

        int bufferIndex = serverProcess.tick % MOVEMENT_BUFFER_SIZE;
        clientProcessBuffer.Add(serverProcess, bufferIndex);

        int tickToReplay = serverProcess.tick + 1;

        while (tickToReplay <= currentTick)
        {
            int idx = tickToReplay % MOVEMENT_BUFFER_SIZE;
            MovementInputData input = clientInputBuffer.Get(idx);
            MovementStateData process = movementProcess.MovementProcess(input);
            clientProcessBuffer.Add(process, idx);
            tickToReplay++;
        }

        lastPredictionProcess = clientProcessBuffer.Get(currentTick);
    }

    private void HandleServerTick()
    {
        if (!IsServer) return;
        if (lastReceivedInput.tick <= 0) return;

        if (lastProcessedServerTick == -1)
        {
            lastProcessedServerTick = lastReceivedInput.tick - 1;
        }

        int maxTickToProcess = Mathf.Min(currentTick, lastReceivedInput.tick);

        for (int tick = lastProcessedServerTick + 1; tick <= maxTickToProcess; tick++)
        {
            int bufferIndex = tick % MOVEMENT_BUFFER_SIZE;
            MovementInputData input;

            if (serverInputBuffer[bufferIndex].tick == tick)
            {
                input = serverInputBuffer[bufferIndex];
            }
            else
            {
                continue;
            }

            MovementStateData process = movementProcess.SimulateMovementProcess(input);
            serverProcessBuffer.Add(process, bufferIndex);
            lastProcessedServerTick = tick;
        }

        if (lastProcessedServerTick >= 0)
        {
            SendLastServerProcessToClientRpc(serverProcessBuffer.Get(lastProcessedServerTick), NetworkManager.ServerTime.TimeAsFloat);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void SendLastServerProcessToClientRpc(MovementStateData process, float serverTime)
    {
        if (process.tick < lastServerProcess.tick) { return; }

        lastServerProcess = process;

        lastServerTime = serverTime;
        hasReceivedServerState = true;
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