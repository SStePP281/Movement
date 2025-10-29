using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;

public class MovementController : NetworkBehaviour
{
    private const int MOVEMENT_BUFFER_SIZE = 1024;

    [SerializeField] private MovementStateController movementProcess;
    [SerializeField] private float reconciliationThreshold = 2.0f;
    [SerializeField] private float maxExtrapolateTime = 1.0f;

    /*Client params*/
    private CircularBuffer<MovementInputData> clientInputBuffer;
    private CircularBuffer<MovementStateData> clientProcessBuffer;
    private MovementStateData lastServerProcess;
    private MovementStateData lastPredictionProcess;
    private int currentTick = 0;
    private float timeBettwenTick;

    private ClientNetworkTransform clientNetworkTransform;
    private MovementStateData guestLastProcess;
    private float currentExtrapolateTime = 0.0f;
    private float lastServerTime = 0.0f;

    /*Server params*/
    private CircularBuffer<MovementStateData> serverProcessBuffer;
    private MovementInputData[] serverInputBuffer;
    private int lastProcessedServerTick = -1;
    private MovementInputData lastReceivedInput;

    /*Input params*/
    private RagdollController ragdollController;
    private AnimationController animationController;
    private MovementInputController movementInput;

    private void Awake()
    {
        clientNetworkTransform = GetComponent<ClientNetworkTransform>();

        clientInputBuffer = new CircularBuffer<MovementInputData>(MOVEMENT_BUFFER_SIZE);
        clientProcessBuffer = new CircularBuffer<MovementStateData>(MOVEMENT_BUFFER_SIZE);
        serverProcessBuffer = new CircularBuffer<MovementStateData>(MOVEMENT_BUFFER_SIZE);
        serverInputBuffer = new MovementInputData[MOVEMENT_BUFFER_SIZE];

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

        if (lastServerProcess.position == guestLastProcess.position) { return; }

        Debug.Log("LLL");

        float ping = (NetworkManager.LocalTime.TimeAsFloat - lastServerTime) * 1000;
        if (ping >= 200f && currentExtrapolateTime < maxExtrapolateTime)
        {
            currentExtrapolateTime += timeBettwenTick;

            movementProcess.ApplyPredictedState(lastServerProcess);
        }
        else
        {
            currentExtrapolateTime = 0.0f;

            movementProcess.ApplyPredictedState(lastServerProcess);
        }

        guestLastProcess = lastServerProcess;
    }

    private void HandleClientTick()
    {
        if (!IsClient || !IsOwner) { return; }

        int bufferIndex = currentTick % MOVEMENT_BUFFER_SIZE;

        MovementInputData input = movementInput.GetInputData(currentTick);
        clientInputBuffer.Add(input, bufferIndex);

        SendClientInputToServerRpc(input);

        MovementStateData process = movementProcess.MovementProcess(input);
        clientProcessBuffer.Add(process, bufferIndex);
        lastPredictionProcess = process;

        HandleServerReconciliation();

        animationController.Run(movementProcess.MoveVelocity);
    }

    [ServerRpc]
    private void SendClientInputToServerRpc(MovementInputData input)
    {
        int index = input.tick % MOVEMENT_BUFFER_SIZE;
        serverInputBuffer[index] = input;
        lastReceivedInput = input;
    }

    private void HandleServerReconciliation()
    {
        if (!ShouldReconcile()) { return; }

        int serverTick = lastServerProcess.tick;
        int bufferIndex = serverTick % MOVEMENT_BUFFER_SIZE;

        MovementStateData rewindProcess = IsHost
            ? serverProcessBuffer.Get(bufferIndex)
            : lastServerProcess;
        MovementStateData clientProcess = IsHost ? clientProcessBuffer.Get(bufferIndex) : clientProcessBuffer.Get(bufferIndex);

        float positionError = Vector3.Distance(rewindProcess.position, clientProcess.position);

        Debug.Log(positionError - reconciliationThreshold);

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

    private void ReconcileProcess(MovementStateData rewindProcess)
    {
        movementProcess.ReconcileProcess(rewindProcess);

        if (!rewindProcess.Equals(lastServerProcess)) { return; }

        clientProcessBuffer.Add(rewindProcess, rewindProcess.tick % MOVEMENT_BUFFER_SIZE);

        int tickToReplay = lastServerProcess.tick;
        MovementInputData input = default;
        MovementStateData process = default;

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
        if (!IsServer || lastReceivedInput.tick == 0) return;

        if (lastProcessedServerTick == -1)
            lastProcessedServerTick = lastReceivedInput.tick - 1;

        int maxTickToProcess = Mathf.Min(currentTick, lastReceivedInput.tick);

        for (int tick = lastProcessedServerTick + 1; tick <= maxTickToProcess; tick++)
        {
            int index = tick % MOVEMENT_BUFFER_SIZE;
            MovementInputData input;

            if (serverInputBuffer[index].tick == tick)
                input = serverInputBuffer[index];
            else
                input = lastReceivedInput;

            input.tick = tick;

            //MovementStateData process = movementProcess.SimulateMovementProcess(input);
            //serverProcessBuffer.Add(process, index);
            lastProcessedServerTick = tick;
        }

        if (lastProcessedServerTick >= 0)
        {
            int lastIndex = lastProcessedServerTick % MOVEMENT_BUFFER_SIZE;
            SendLastServerProcessToClientRpc(serverProcessBuffer.Get(lastIndex), NetworkManager.ServerTime.TimeAsFloat);
        }
    }

    [ClientRpc]
    private void SendLastServerProcessToClientRpc(MovementStateData process, float serverTime)
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