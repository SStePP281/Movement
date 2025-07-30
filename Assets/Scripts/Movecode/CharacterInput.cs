using System;
using UnityEngine;
using UnityEngine.InputSystem;

public struct CharacterInputData
{
    public Vector2 movement;
    public bool isJumpPressed;
}

public class CharacterInput : IDisposable
{
    public CharacterInputData InputData { get => data; }

    private CharacterInputData data;
    private PlayerInputAction inputActions;

    public CharacterInput()
    {
        data = new CharacterInputData();

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
        data.movement = cntx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext cntx)
    {
        data.isJumpPressed = cntx.ReadValueAsButton();
    }
}
