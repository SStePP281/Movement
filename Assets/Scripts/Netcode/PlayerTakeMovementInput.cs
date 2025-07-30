using System;
using Unity.Netcode;
using UnityEngine;

public struct PlayerInput : INetworkSerializable
{
    public int tick;
    public Vector2 keyboardInput;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref tick);
        serializer.SerializeValue(ref keyboardInput);
    }
}

public class PlayerTakeMovementInput : MonoBehaviour
{
    private PlayerInput input;

    private void Update()
    {
        input.keyboardInput.x = Input.GetAxis("Horizontal");
        input.keyboardInput.y = Input.GetAxis("Vertical");
    }

    public PlayerInput GetPlayerInput(int tick)
    {
        input.tick = tick;
        return input;
    }
}
