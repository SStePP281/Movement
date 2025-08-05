using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public struct MovementInputData : INetworkSerializable
{
    public Vector2 movementInput;
    public bool isJumpPressed;
    public int tick;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref movementInput);
        serializer.SerializeValue(ref isJumpPressed);
        serializer.SerializeValue(ref tick);
    }
}

public class MovementInputController : IDisposable
{
    private MovementInputData data;
    private PlayerInputAction inputActions;

    public MovementInputController()
    {
        data = new MovementInputData();

        inputActions = new PlayerInputAction();
        inputActions.Enable();

        inputActions.PlayerController.Move.started += OnMove;
        inputActions.PlayerController.Move.performed += OnMove;
        inputActions.PlayerController.Move.canceled += OnMove;

        inputActions.PlayerController.Jump.started += OnJump;
        inputActions.PlayerController.Jump.canceled += OnJump;
    }

    public void Dispose()
    {
        inputActions.PlayerController.Move.started -= OnMove;
        inputActions.PlayerController.Move.performed -= OnMove;
        inputActions.PlayerController.Move.canceled -= OnMove;

        inputActions.PlayerController.Jump.started -= OnJump;
        inputActions.PlayerController.Jump.canceled -= OnJump;
    }

    public void Disable() => inputActions.PlayerController.Disable();

    public void Enable() => inputActions.PlayerController.Enable();

    public MovementInputData GetInputData(int tick)
    {
        data.tick = tick;
        return data;
    }

    private void OnMove(InputAction.CallbackContext cntx)
    {
        data.movementInput = cntx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext cntx)
    {
        data.isJumpPressed = cntx.ReadValueAsButton();
    }
}
