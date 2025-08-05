using Unity.Netcode;
using UnityEngine;

public class CharacterController : NetworkBehaviour
{
    [SerializeField] private MovementController movementController;
    [SerializeField] private AnimationController animationController;
    [SerializeField] private FootIKController footIKController;
    [SerializeField] private RagdollController ragdolController;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        animationController.Initialize(transform);

        animationController.animationEndedCallback += OnStandUpEnded;
        ragdolController.OnRagdolEnable += OnRagdolEnable;

        if (IsOwner)
        {
            movementController.Initialize(animationController, ragdolController);

            animationController.animationEndedCallback += animationController.UpdateParams;
            ragdolController.OnStill += OnStill;
        }
    }

    private void OnRagdolEnable()
    {
        animationController.Disable();
        footIKController.Disable();
        movementController.OnRagdolEnable();
    }

    private void OnStill()
    {
        ragdolController.Disable();
        animationController.PlayStandingUp();
    }

    private void OnStandUpEnded()
    {
        footIKController.Enable();
        movementController.OnRagdolDisable();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (IsOwner)
        {
            animationController.Dispose();
        }
    }
}
