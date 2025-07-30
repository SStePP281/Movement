using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BoneTransfromData
{
    public Vector3 position;
    public Quaternion rotation;
}

public class RigAdjusterForAnimation
{
    private const float timeToShiftBonesToStartAnimation = 0.5f;

    private AnimationClip clip;
    private MonoBehaviour animatonScript;
    private List<Transform> bonesList;

    private BoneTransfromData[] bonesBefore;
    private BoneTransfromData[] bonesAfter;

    private Coroutine shiftBonesCoroutine;

    public RigAdjusterForAnimation(AnimationClip clip, MonoBehaviour animatonScript, IEnumerable<Transform> bones)
    {
        this.clip = clip;
        this.animatonScript = animatonScript;
        bonesList = new List<Transform>(bones);

        bonesBefore = new BoneTransfromData[bonesList.Count];
        bonesAfter = new BoneTransfromData[bonesList.Count];

        SaveBonesDataFromStartAnimation();
    }

    public void Adjust(Action callback)
    {
        SaveCurrentBonesDataTo(bonesBefore);

        if (shiftBonesCoroutine != null)
        {
            animatonScript.StopCoroutine(shiftBonesCoroutine);
        }

        shiftBonesCoroutine = animatonScript.StartCoroutine(ShiftBonesToAnimation(callback));
    }

    private IEnumerator ShiftBonesToAnimation(Action callback)
    {
        float progress = 0.0f;

        while (progress < timeToShiftBonesToStartAnimation)
        {
            progress += Time.deltaTime;

            float progressInterpolate = progress / timeToShiftBonesToStartAnimation;

            for (int i = 0; i < bonesBefore.Length; i++)
            {
                bonesList[i].localPosition = Vector3.Lerp(bonesBefore[i].position, bonesAfter[i].position, progressInterpolate);
                bonesList[i].localRotation = Quaternion.Lerp(bonesBefore[i].rotation, bonesAfter[i].rotation, progressInterpolate);
            }

            yield return null;
        }

        callback?.Invoke();
    }

    private void SaveBonesDataFromStartAnimation()
    {
        Vector3 initPosition = animatonScript.transform.position;
        Quaternion initRotation = animatonScript.transform.rotation;

        clip.SampleAnimation(animatonScript.gameObject, 0);
        SaveCurrentBonesDataTo(bonesAfter);

        animatonScript.transform.position = initPosition;
        animatonScript.transform.rotation = initRotation;
    }

    private void SaveCurrentBonesDataTo(BoneTransfromData[] bones)
    {
        for (int i = 0; i < bones.Length; i++)
        {
            bones[i].position = bonesList[i].localPosition;
            bones[i].rotation = bonesList[i].localRotation;
        }
    }
}
