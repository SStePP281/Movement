using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewMovementInputController : IDisposable
{
    private MovementInputData data;
    private PlayerInputAction inputActions;

    public NewMovementInputController()
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
