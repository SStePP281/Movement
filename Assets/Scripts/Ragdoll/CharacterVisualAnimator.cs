using System;
using System.Linq;
using UnityEngine;

public class CharacterVisualAnimator : MonoBehaviour
{
    private const string RUN_BOOL_NAME = "IsRunning";
    private const string BackStandUpName = "BackStandUp";
    private const string FrontStandUpName = "FrontStandUp";

    [SerializeField] private LayerMask ground;

    private Animator animator;
    private Transform parent;
    private Transform hipsBone;

    private RigAdjusterForAnimation rigAdjustForBackStandUp;
    private RigAdjusterForAnimation rigAdjustForFrontStandUp;

    private Action standingUpEndCallback;
    private int runHashName;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        runHashName = Animator.StringToHash(RUN_BOOL_NAME);
    }

    public void Initialize(Transform parent)
    {
        this.parent = parent;
        hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);

        AnimationClip[] currentClips = animator.runtimeAnimatorController.animationClips;
        Transform[] bones = hipsBone.GetComponentsInChildren<Transform>();

        rigAdjustForBackStandUp = new RigAdjusterForAnimation(currentClips.First(clip => clip.name == BackStandUpName), this, bones);
        rigAdjustForFrontStandUp = new RigAdjusterForAnimation(currentClips.First(clip => clip.name == FrontStandUpName), this, bones);
    }

    public void PlayStandingUp(Action adjustAnimationEnded = null, Action animationEndedCallback = null)
    {
        AdjustParentRotationToHipsBone();
        AdjustParentPositionToHipsBone();

        standingUpEndCallback = animationEndedCallback;

        if (IsFrontUp)
        {
            rigAdjustForFrontStandUp.Adjust(() => CallbackForAdjustStand(FrontStandUpName, adjustAnimationEnded));
        }
        else
        {
            rigAdjustForBackStandUp.Adjust(() => CallbackForAdjustStand(BackStandUpName, adjustAnimationEnded));
        }
    }
    
    public void OnStandUpAnimationEnded() => standingUpEndCallback?.Invoke();

    public void StartRunning() => animator.SetBool(runHashName, true);

    public void StopRunning() => animator.SetBool(runHashName, false);

    public void DisableAnimator() => animator.enabled = false;
    
    public void EnableAnimator() => animator.enabled = true;

    private void CallbackForAdjustStand(string clipName, Action callback)
    {
        callback?.Invoke();
        animator.Play(clipName, -1, 0.0f);
    }

    private bool IsFrontUp => Vector3.Dot(hipsBone.up, Vector3.up) > 0;

    private void AdjustParentPositionToHipsBone()
    {
        Vector3 initHipsPositon = hipsBone.position;
        parent.position = initHipsPositon;

        AdjustParentPositionRelativeGround();

        hipsBone.position = initHipsPositon;
    }

    private void AdjustParentPositionRelativeGround()
    {
        if (Physics.Raycast(parent.position, Vector3.down, out RaycastHit hit, 5, ground))
        {
            parent.position = new Vector3(parent.position.x, hit.point.y, parent.position.z);
        }
    }    

    private void AdjustParentRotationToHipsBone()
    {
        Vector3 initHipsPositon = hipsBone.position;
        Quaternion initHipsRotation = hipsBone.rotation;

        Vector3 directionForRotate = hipsBone.up;
        if (!IsFrontUp)
        {
            directionForRotate *= -1;
        }

        directionForRotate.y = 0;

        Quaternion correctionRotation = Quaternion.FromToRotation(parent.forward, directionForRotate.normalized);
        parent.rotation *= correctionRotation;

        hipsBone.position = initHipsPositon;
        hipsBone.rotation = initHipsRotation;
    }
}
