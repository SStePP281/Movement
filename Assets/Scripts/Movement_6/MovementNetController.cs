using Unity.Netcode;
using UnityEngine;

public class MovementNetController : NetworkBehaviour
{
    const int BUFFER_SIZE = 2048;

    [Header("Reference")]
    [SerializeField] private MovementStateController stateController;

    [Header("Settings")]
    [SerializeField] private float maxExtrapolateTime = 2.0f;
    [SerializeField] private float maxThreshold = 1.0f;

    //Input
    private MovementInputController inputController;

    //NetWorldParam
    private int currentTick;
    private float timeBetweenTick;

    //ClientParam
    private CircularBuffer<MovementStateData> clientStateBuffer;
    private int lastServerTick;

    //ServerParam
    private CircularBuffer<MovementStateData> serverStateBuffer;

    //GuestParam
    private float currentExtrapolateTime;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        timeBetweenTick = 1.0f / NetworkManager.Singleton.NetworkTickSystem.TickRate;

        stateController.Initialize(timeBetweenTick);

        if (IsOwner)
        {
            inputController = new MovementInputController();
            clientStateBuffer = new CircularBuffer<MovementStateData>(BUFFER_SIZE);
            currentTick = NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick;

            NetworkManager.Singleton.NetworkTickSystem.Tick += NetworkTickSystem_Tick;
        }

        if (IsServer)
        {
            serverStateBuffer = new CircularBuffer<MovementStateData>(BUFFER_SIZE);
        }
    }

    private void NetworkTickSystem_Tick()
    {
        MovementInputData clientInputData = inputController.GetInputData(currentTick);
        MovementStateData clientStateData = stateController.MovementProcess(clientInputData);

        clientStateBuffer.Add(clientStateData, currentTick);

        SendPredictionDataToServerRpc(clientInputData, clientStateData);

        currentTick++;
    }

    private void HandleReconciliation(MovementStateData serverStateData)
    {
        MovementStateData clientStateData = clientStateBuffer.Get(serverStateData.tick);

        if (clientStateData.Equals(default)) { return; }

        if (ShouldReconciliate(serverStateData, clientStateData))
        {
            stateController.ReconcileProcess(serverStateData);
            clientStateBuffer.Add(serverStateData, currentTick);

            for (int index = (serverStateData.tick + 1); index < currentTick; index++)
            {
                clientStateBuffer.Add(serverStateData, index);
            }
        }
    }

    private void HandleFollowing(MovementStateData serverState)
    {
        float timeSinceLastUpdate = (NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick - lastServerTick) * timeBetweenTick;

        float expectedInterval = timeBetweenTick * 1.5f;
        bool isPacketLoss = timeSinceLastUpdate > expectedInterval;

        if (isPacketLoss && currentExtrapolateTime < maxExtrapolateTime)
        {
            currentExtrapolateTime += timeBetweenTick;

            stateController.ApplyPredictedState(serverState);
        }
        else
        {
            currentExtrapolateTime = 0f;

            stateController.ApplyServerState(serverState);
        }
    }

    private bool ShouldReconciliate(MovementStateData serverStateData, MovementStateData clientStateData)
    {
        Debug.Log(Vector3.Distance(serverStateData.position, clientStateData.position));
        return Vector3.Distance(serverStateData.position, clientStateData.position) >= maxThreshold;
    }

    [ServerRpc(RequireOwnership = true)]
    private void SendPredictionDataToServerRpc(MovementInputData clientInputData, MovementStateData clientStateData)
    {
        if (clientInputData.tick == 0) { return; }

        MovementStateData privouseStateData = serverStateBuffer.Get(clientInputData.tick - 1);
        MovementStateData relativeStateData = stateController.SimulateMovementProcess(privouseStateData, clientInputData);

        if (ShouldReconciliate(relativeStateData, clientStateData))
        {
            int serverCurrentTick = NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick;
            for (int index = clientStateData.tick; index < serverCurrentTick; index++)
            {
                serverStateBuffer.Add(relativeStateData, index);
            }

            HandleFollowing(relativeStateData);

            SendRelativeDataToClientRpc(relativeStateData, NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick);
        }
        else
        {
            HandleFollowing(clientStateData);
            serverStateBuffer.Add(clientStateData, clientStateData.tick);

            SendRelativeDataToClientRpc(clientStateData, NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void SendRelativeDataToClientRpc(MovementStateData serverStateData, int serverTick)
    {
        if (IsOwner)
        {
            lastServerTick = serverTick;

            HandleReconciliation(serverStateData);
        }
        else
        {
            HandleFollowing(serverStateData);
        }
    }

    public override void OnDestroy()
    {
        if (IsOwner)
        {
            inputController.Dispose();
        }
    }
}