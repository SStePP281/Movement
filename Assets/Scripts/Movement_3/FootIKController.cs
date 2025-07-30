using UnityEngine;

public class FootIKController : MonoBehaviour
{
    [SerializeField] private float footOffsetY = 0.1f;

    Animator anim;

    Transform leftFoot;
    Transform rightFoot;

    Vector3 leftFoot_pos;
    Vector3 rightFoot_pos;

    Quaternion leftFoot_rot;
    Quaternion rightFoot_rot;

    float leftFoot_Weight;
    float rightFoot_Weight;

    private void Start()
    {
        anim = GetComponent<Animator>();
        leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // foot IK
        leftFoot_Weight = anim.GetFloat("LeftFoot");
        rightFoot_Weight = anim.GetFloat("RightFoot");

        FindFloorPositions(leftFoot, ref leftFoot_pos, ref leftFoot_rot, Vector3.up);
        FindFloorPositions(rightFoot, ref rightFoot_pos, ref rightFoot_rot, Vector3.up);

        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFoot_Weight);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFoot_Weight);

        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFoot_Weight);
        anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFoot_Weight);

        anim.SetIKPosition(AvatarIKGoal.LeftFoot, leftFoot_pos);
        anim.SetIKPosition(AvatarIKGoal.RightFoot, rightFoot_pos);

        anim.SetIKRotation(AvatarIKGoal.LeftFoot, leftFoot_rot);
        anim.SetIKRotation(AvatarIKGoal.RightFoot, rightFoot_rot);
    }

    void FindFloorPositions(Transform t, ref Vector3 targetPosition, ref Quaternion targetRotation, Vector3 direction)
    {
        RaycastHit hit;
        Vector3 rayOrigin = t.position;

        rayOrigin += direction * 0.2f;

        if (Physics.Raycast(rayOrigin, -direction, out hit, 3))
        {
            targetPosition = hit.point + footOffsetY * Vector3.up;

            Quaternion rot = Quaternion.LookRotation(transform.forward);
            targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * rot;
        }
    }

    public void Enable() => enabled = true;

    public void Disable() => enabled = false;
}