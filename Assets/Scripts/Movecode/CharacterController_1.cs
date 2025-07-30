using UnityEngine;

public class CharacterController_1 : MonoBehaviour
{
    [SerializeField] private CharacterMovement movement;
    [SerializeField] private CharacterAnimator animatior;
    [SerializeField] private CharacterDeformation deformation;
    [SerializeField] private CharacterInput input;

    private void Awake()
    {
        input = new CharacterInput();
        movement.Initialize();

        movement.jumpCombatCount.OnValueChanged += animatior.JumpCombat;
        movement.isJumpAnimation.OnValueChanged += animatior.Jump;
    }

    private void FixedUpdate()
    {
        movement.MovementProcess(input.InputData, Time.fixedDeltaTime);

        Vector2 velocity = new Vector2(movement.Velocity.x, movement.Velocity.z);
        animatior.Run(velocity.normalized.magnitude);

        deformation.SquashAndStretch(movement.IsGrounded, movement.Velocity);
    }

    private void OnDestroy()
    {
        input.Dispose();

        movement.jumpCombatCount.OnValueChanged -= animatior.JumpCombat;
        movement.isJumpAnimation.OnValueChanged -= animatior.Jump;
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void OnEnable()
    {
        input.Enable();
    }
}
