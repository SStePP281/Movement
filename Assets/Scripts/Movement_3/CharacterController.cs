using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [SerializeField] private MovementController movementController;
    [SerializeField] private AnimationController animationController;
    [SerializeField] private FootIKController footIKController;
    [SerializeField] private RagdollController ragdollHandler;

    private InputController inputController;

    private void Awake()
    {
        inputController = new InputController();
    }

    private void Start()
    {
        movementController.Initialize(Time.fixedDeltaTime);
        animationController.Initialize(transform, movementController.IsJumpAnimation, movementController.CombatJumpCount);

        movementController.OnFalling += (Vector3 vel) => { ActiveRagdol(true, vel); };
        ragdollHandler.OnStill += StandUp;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ActiveRagdol();
        }

        if (Input.GetKeyUp(KeyCode.X))
        {
            StandUp();
        }
    }

    private void FixedUpdate()
    {
        movementController.MovementUpdate(inputController.InputData);
        animationController.Run(movementController.MoveVelocity);
    }

    private void ActiveRagdol(bool isCanStand = false, Vector3 startVelocity = default)
    {
        animationController.Disable();
        footIKController.Disable();
        movementController.OnRagdolEnable();
        ragdollHandler.Enable(isCanStand, startVelocity);
    }

    private void StandUp()
    {
        ragdollHandler.Disable();
        animationController.PlayStandingUp(animationController.Enable, () => { movementController.OnRagdolDisable(); animationController.UpdateParams(); footIKController.Enable(); });
    }

    private void OnDestroy()
    {
        inputController.Dispose();
        animationController.Dispose();
    }
}
