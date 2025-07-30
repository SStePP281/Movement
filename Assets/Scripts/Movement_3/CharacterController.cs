using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [SerializeField] private MovementController movementController;
    [SerializeField] private FootIKController footIKController;
    [SerializeField] private Animator characterAnimator;

    private InputController inputController;
    private AnimationController animationController;

    private void Awake()
    {
        movementController.Initialize(Time.fixedDeltaTime);
        inputController = new InputController();
        animationController = new AnimationController(characterAnimator, movementController.IsJumpAnimation, movementController.CombatJumpCount);
    }

    private void FixedUpdate()
    {
        movementController.MovementUpdate(inputController.InputData);
        animationController.Run(movementController.MoveVelocity);
    }

    private void OnDestroy()
    {
        inputController.Dispose();
        animationController.Dispose();
    }

    private void OnDisable()
    {
        inputController.Disable();
        animationController.Disable();
        footIKController.Disable();
    }

    private void OnEnable()
    {
        inputController.Enable();
        animationController.Enable();
        footIKController.Enable();
    }
}
