using UnityEngine;
using System;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Character/InputReader")]
public class InputReader : ScriptableObject
{
    
    public event Action OnJumpStarted;
    public event Action OnJumpCancelled;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    private PlayerInputActions _actions;

    private void OnEnable()
    {
        _actions = new PlayerInputActions();

        _actions.Player.Jump.performed += OnJump;
        _actions.Player.Jump.canceled += OnJumpCancel;

        _actions.Player.Move.performed += OnMove;
        _actions.Player.Move.canceled += OnMove;
        _actions.Player.Look.performed += OnLook;
        _actions.Player.Look.canceled += OnLook;
        _actions.Player.Enable();
    }

    private void OnDisable()
    {

        _actions.Player.Jump.performed -= OnJump;
        _actions.Player.Jump.canceled -= OnJumpCancel;
        _actions.Player.Move.performed -= OnMove;
        _actions.Player.Move.canceled -= OnMove;
        _actions.Player.Look.performed -= OnLook;
        _actions.Player.Look.canceled -= OnLook;
        _actions.Player.Disable();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        OnJumpStarted?.Invoke();
    }

    private void OnJumpCancel(InputAction.CallbackContext context)
    {
        OnJumpCancelled?.Invoke();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }


}
