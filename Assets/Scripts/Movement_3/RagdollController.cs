using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    public Action OnStill;

    private List<Rigidbody> rigidbodyList;
    private List<Collider> colliderList;
    private Coroutine stillCorotine;

    private void Awake()
    {
        rigidbodyList = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());
        colliderList = new List<Collider>(GetComponentsInChildren<Collider>());
        Disable();
    }

    public void Hit(Vector3 force, Vector3 hitPosition)
    {
        Rigidbody rigidbody = rigidbodyList.OrderBy(rigidbody => Vector3.Distance(rigidbody.position, hitPosition)).First();
        rigidbody.AddForceAtPosition(force, hitPosition, ForceMode.Impulse);
    }

    public void Enable(bool canStandUp = false, Vector3 startVelocity = default)
    {
        foreach (Rigidbody rigidbody in rigidbodyList)
        {
            rigidbody.isKinematic = false;
            rigidbody.velocity = startVelocity;
        }

        foreach (Collider collider in colliderList)
        {
            collider.enabled = true;
        }

        if (stillCorotine == null && canStandUp)
        {
            stillCorotine = StartCoroutine(CheckStil());
        }
    }

    public void Disable()
    {
        foreach (Rigidbody rigidbody in rigidbodyList)
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.isKinematic = true;
        }

        foreach (Collider collider in colliderList)
        {
            collider.enabled = false;
        }

        if (stillCorotine != null)
        {
            StopCoroutine(stillCorotine);
            stillCorotine = null;
        }
    }

    private IEnumerator CheckStil()
    {
        while (true)
        {
            float totalMagnitude = rigidbodyList.Sum(r => r.velocity.magnitude) / rigidbodyList.Count;

            if (totalMagnitude < 0.1f)
            {
                OnStill?.Invoke();
                yield break;
            }

            yield return new WaitForSeconds(0.1f);

        }
    }
}
