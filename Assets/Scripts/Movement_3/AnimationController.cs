using System;
using UnityEngine;

public class AnimationController : IDisposable
{
    private const string VELOCITY_FLOAT_NAME = "Velocity";
    private const string JUMPING_BOOL_NAME = "Jumping";
    private const string JUMPING_COMBAT_INT_NAME = "CombatJump";

    private int velocityNameHash;
    private int jumpingNameHash;
    private int jumpingCombatNameHash;

    private Animator animator;
    private ReactiveVariable<bool> isJumping;
    private ReactiveVariable<int> jumpCombatCount;

    private bool isEnable = true;

    public AnimationController(Animator _animator, ReactiveVariable<bool> _isJumping, ReactiveVariable<int> _jumpCombatCount)
    {
        animator = _animator;
        isJumping = _isJumping;
        jumpCombatCount = _jumpCombatCount;

        velocityNameHash = Animator.StringToHash(VELOCITY_FLOAT_NAME);
        jumpingNameHash = Animator.StringToHash(JUMPING_BOOL_NAME);
        jumpingCombatNameHash = Animator.StringToHash(JUMPING_COMBAT_INT_NAME);

        isJumping.OnValueChanged += Jump;
        jumpCombatCount.OnValueChanged += JumpCombat;
    }

    public void Run(float currentVelocity)
    {
        if (!isEnable) { return; }

        animator.SetFloat(velocityNameHash, currentVelocity, 0.05f, Time.fixedDeltaTime);
    }

    private void Jump(bool priviouse, bool current)
    {
        if (!isEnable) { return; }

        animator.SetBool(jumpingNameHash, current);
    }

    private void JumpCombat(int priviouse, int current)
    {
        if (!isEnable) { return; }

        animator.SetInteger(jumpingCombatNameHash, current);
    }

    public void Disable() => isEnable = false;

    public void Enable() => isEnable = true;

    public void Dispose()
    {
        isJumping.OnValueChanged -= Jump;
        jumpCombatCount.OnValueChanged -= JumpCombat;
    }
}
