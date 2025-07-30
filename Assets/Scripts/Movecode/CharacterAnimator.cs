using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    private const string VELOCITY_FLOAT_NAME = "Velocity";
    private const string JUMPING_BOOL_NAME = "Jumping";
    private const string JUMPING_COMBAT_INT_NAME = "CombatJump";

    [SerializeField] private Animator characterAnimator;

    private int velocityNameHash;
    private int jumpingNameHash;
    private int jumpingCombatNameHash;

    private void Awake()
    {
        velocityNameHash = Animator.StringToHash(VELOCITY_FLOAT_NAME);
        jumpingNameHash = Animator.StringToHash(JUMPING_BOOL_NAME);
        jumpingCombatNameHash = Animator.StringToHash(JUMPING_COMBAT_INT_NAME);
    }

    public void Run(float currentVelocity)
    {
        characterAnimator.SetFloat(velocityNameHash, currentVelocity, 0.2f, Time.deltaTime);
    }

    public void Jump(bool priviouse, bool current)
    {
        characterAnimator.SetBool(jumpingNameHash, current);
    }

    public void JumpCombat(int priviouse, int current)
    {
        characterAnimator.SetInteger(jumpingCombatNameHash, current);
    }
}