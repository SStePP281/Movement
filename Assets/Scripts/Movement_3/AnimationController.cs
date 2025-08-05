using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class AnimationController : NetworkBehaviour, IDisposable
{
    private const string VELOCITY_FLOAT_NAME = "Velocity";
    private const string JUMPING_BOOL_NAME = "Jumping";
    private const string JUMPING_COMBAT_INT_NAME = "CombatJump";
    private const string BACK_STAND_UP_NAME = "BackStandUp";
    private const string FRONT_STAND_UP_NAME = "FrontStandUp";

    private Animator animator;

    /*Base Anim params*/
    private int velocityNameHash;
    private int jumpingNameHash;
    private int jumpingCombatNameHash;

    /*Movement params*/
    private ReactiveVariable<bool> isJumping;
    private ReactiveVariable<int> jumpCombatCount;

    /*Ragdol params*/
    private Transform parent;
    private Transform hipsBone;
    private RigAdjusterForAnimation rigAdjustForBackStandUp;
    private RigAdjusterForAnimation rigAdjustForFrontStandUp;

    public Action animationEndedCallback;

    public bool IsStandUp { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();

        velocityNameHash = Animator.StringToHash(VELOCITY_FLOAT_NAME);
        jumpingNameHash = Animator.StringToHash(JUMPING_BOOL_NAME);
        jumpingCombatNameHash = Animator.StringToHash(JUMPING_COMBAT_INT_NAME);
    }

    public void Initialize(Transform _parent)
    {
        parent = _parent;

        hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);

        AnimationClip[] currentClips = animator.runtimeAnimatorController.animationClips;
        Transform[] bones = hipsBone.GetComponentsInChildren<Transform>();

        rigAdjustForBackStandUp = new RigAdjusterForAnimation(currentClips.First(clip => clip.name == BACK_STAND_UP_NAME), this, bones);
        rigAdjustForFrontStandUp = new RigAdjusterForAnimation(currentClips.First(clip => clip.name == FRONT_STAND_UP_NAME), this, bones);
    }

    public void SetJumpingVariable(ReactiveVariable<bool> _isJumping, ReactiveVariable<int> _jumpCombatCount)
    {
        isJumping = _isJumping;
        jumpCombatCount = _jumpCombatCount;

        isJumping.OnValueChanged += Jump;
        jumpCombatCount.OnValueChanged += JumpCombat;
    }

    public void Run(float currentVelocity)
    {
        animator.SetFloat(velocityNameHash, currentVelocity, 0.05f, Time.fixedDeltaTime);
    }

    private void Jump(bool priviouse, bool current)
    {
        animator.SetBool(jumpingNameHash, current);
    }

    private void JumpCombat(int priviouse, int current)
    {
        animator.SetInteger(jumpingCombatNameHash, current);
    }

    public void PlayStandingUp()
    {
        bool isFront = IsFrontUp;
        IsStandUp = true;

        StandUp(isFront);

        SendStandUpServerRpc(isFront);
    }

    public void OnStandUpAnimationEnded()
    {
        IsStandUp = false;
        animationEndedCallback?.Invoke();
    }

    private void CallbackForAdjustStand(string clipName, Action callback)
    {
        callback?.Invoke();
        animator.Play(clipName, -1, 0.0f);
    }

    private void AdjustParentPositionToHipsBone()
    {
        Vector3 initHipsPositon = hipsBone.position;
        parent.position = initHipsPositon;

        AdjustParentPositionRelativeGround();

        hipsBone.position = initHipsPositon;
    }

    private void AdjustParentPositionRelativeGround()
    {
        if (Physics.Raycast(parent.position, Vector3.down, out RaycastHit hit, 5))
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

    private bool IsFrontUp => Vector3.Dot(hipsBone.up, Vector3.up) > 0;

    public void Disable() => animator.enabled = false;

    public void Enable() => animator.enabled = true;

    public void UpdateParams()
    {
        animator.SetInteger(jumpingCombatNameHash, jumpCombatCount.Value);
        animator.SetBool(jumpingNameHash, isJumping.Value);
    }

    public void Dispose()
    {
        isJumping.OnValueChanged -= Jump;
        jumpCombatCount.OnValueChanged -= JumpCombat;
    }

    [ServerRpc]
    private void SendStandUpServerRpc(bool isFront)
    {
        if (!IsHost)
        {
            StandUp(isFront);
        }

        SendStandUpClientRpc(isFront);
    }

    [ClientRpc]
    private void SendStandUpClientRpc(bool isFront)
    {
        if (IsOwner) { return; }

        StandUp(isFront);
    }

    private void StandUp(bool isFront)
    {
        AdjustParentRotationToHipsBone();
        AdjustParentPositionToHipsBone();

        if (isFront)
        {
            rigAdjustForFrontStandUp.Adjust(() => CallbackForAdjustStand(FRONT_STAND_UP_NAME, Enable));
        }
        else
        {
            rigAdjustForBackStandUp.Adjust(() => CallbackForAdjustStand(BACK_STAND_UP_NAME, Enable));
        }
    }
}
