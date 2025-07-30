using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private CharacterVisualAnimator animator;
    [SerializeField] private RagdollHandler ragdollHandler;

    private void Start()
    {
        animator.Initialize(transform);
    }

    public void Kill()
    {
        animator.DisableAnimator();
        //mover.Disable();
        ragdollHandler.Enable();
    }

    public void StandUp()
    {
        ragdollHandler.Disable();
        animator.PlayStandingUp(animator.EnableAnimator, animator.StopRunning);
    }

    public void StartRun()
    {
        animator.StartRunning();
        //mover.Enable();
    }

    public void TakeDamage(Vector3 force, Vector3 hitPosition)
    {
        Kill();
        ragdollHandler.Hit(force, hitPosition);
    }
}
