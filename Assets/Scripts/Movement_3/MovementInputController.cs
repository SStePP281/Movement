using System;
using UnityEngine;
using UnityEngine.InputSystem;

public struct MovementInputData
{
    public Vector2 movementInput;
    public bool isJumpPressed;
}

public class MovementInputController : IDisposable
{
    public MovementInputData MovementInputData { get => data; }

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

    private void OnMove(InputAction.CallbackContext cntx)
    {
        data.movementInput = cntx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext cntx)
    {
        data.isJumpPressed = cntx.ReadValueAsButton();
    }
}
