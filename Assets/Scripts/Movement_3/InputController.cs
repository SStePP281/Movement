using System;
using UnityEngine;
using UnityEngine.InputSystem;

public struct InputData
{
    public Vector2 movementInput;
    public bool isJumpPressed;
}

public class InputController : IDisposable
{
    public InputData InputData { get => data; }

    private InputData data;
    private PlayerInputAction inputActions;

    public InputController()
    {
        data = new InputData();

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
