using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RagdollHandler : MonoBehaviour
{
    private List<Rigidbody> rigidbodyList;

    private void Awake()
    {
        rigidbodyList = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());
        Disable();
    }

    public void Hit(Vector3 force, Vector3 hitPosition)
    {
        Rigidbody rigidbody = rigidbodyList.OrderBy(rigidbody => Vector3.Distance(rigidbody.position, hitPosition)).First();
        rigidbody.AddForceAtPosition(force, hitPosition, ForceMode.Impulse);
    }

    public void Enable()
    {
        foreach (Rigidbody rigidbody in rigidbodyList)
        {
            rigidbody.isKinematic = false;
        }
    }

    public void Disable()
    {
        foreach (Rigidbody rigidbody in rigidbodyList)
        {
            rigidbody.isKinematic = true;
        }
    }
}
