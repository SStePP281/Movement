using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public struct BoneSyncData : INetworkSerializable
{
    public Vector3 position;
    public Quaternion rotation;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
    }
}

public class RagdollController : NetworkBehaviour
{
    [SerializeField] private Transform playerVisual;
    [SerializeField] private Rigidbody hipsBone;

    [SerializeField] private float stilTime = 2.0f;

    public Action OnStill;
    public Action OnRagdolEnable;

    public bool IsActive { get; private set; }

    private List<Rigidbody> rigidbodyList;
    private List<Collider> colliderList;

    private Coroutine syncBoneCorotine;
    private Coroutine stillCorotine;

    private void Awake()
    {
        rigidbodyList = new List<Rigidbody>(playerVisual.GetComponentsInChildren<Rigidbody>());
        colliderList = new List<Collider>(playerVisual.GetComponentsInChildren<Collider>());
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        RagdolDisable();
    }

    public void Hit(Vector3 force, Vector3 hitPosition)
    {
        Rigidbody rigidbody = rigidbodyList.OrderBy(rigidbody => Vector3.Distance(rigidbody.position, hitPosition)).First();
        rigidbody.AddForceAtPosition(force, hitPosition, ForceMode.Impulse);
    }

    public void Enable(bool canStandUp = false, Vector3 startVelocity = default)
    {
        RagdolEnable(canStandUp, startVelocity);

        RagdolEnableServerRpc(canStandUp, startVelocity);
    }

    public void Disable()
    {
        RagdolDisable();

        RagdolDisableServerRpc();
    }

    [ServerRpc]
    private void RagdolEnableServerRpc(bool canStandUp = false, Vector3 startVelocity = default)
    {
        if (!IsHost)
        {
            RagdolEnable(canStandUp, startVelocity);
        }

        RagdolEnableClientRpc(canStandUp, startVelocity);
    }

    [ServerRpc]
    private void RagdolDisableServerRpc()
    {
        if (!IsHost)
        {
            RagdolDisable();
        }

        RagdolDisableClientRpc();
    }

    [ClientRpc]
    private void RagdolEnableClientRpc(bool canStandUp = false, Vector3 startVelocity = default)
    {
        if (IsOwner) { return; }

        RagdolEnable(canStandUp, startVelocity);
    }

    [ClientRpc]
    private void RagdolDisableClientRpc()
    {
        if (IsOwner) { return; }
        
        RagdolDisable();
    }

    private void RagdolEnable(bool canStandUp = false, Vector3 startVelocity = default)
    {
        IsActive = true;

        foreach (Rigidbody rigidbody in rigidbodyList)
        {
            rigidbody.isKinematic = false;
            rigidbody.velocity = startVelocity;
        }

        foreach (Collider collider in colliderList)
        {
            collider.enabled = true;
        }

        OnRagdolEnable?.Invoke();

        if (!IsOwner) { return; }

        if (stillCorotine == null && canStandUp)
        {
            stillCorotine = StartCoroutine(CheckStil());
        }

        if (syncBoneCorotine == null)
        {
            syncBoneCorotine = StartCoroutine(SyncHipsBone());
        }
    }

    private void RagdolDisable()
    {
        IsActive = false;

        foreach (Rigidbody rigidbody in rigidbodyList)
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.isKinematic = true;
        }

        foreach (Collider collider in colliderList)
        {
            collider.enabled = false;
        }

        if (!IsOwner) { return; }

        if (stillCorotine != null)
        {
            StopCoroutine(stillCorotine);
            stillCorotine = null;
        }

        if (syncBoneCorotine != null)
        {
            StopCoroutine(syncBoneCorotine);
            syncBoneCorotine = null;
        }
    }

    private IEnumerator CheckStil()
    {
        float currentStilTime = 0.0f;

        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            float totalMagnitude = rigidbodyList.Sum(r => r.velocity.magnitude) / rigidbodyList.Count;

            if (totalMagnitude < 0.1f)
            {
                if (currentStilTime >= stilTime)
                {
                    OnStill?.Invoke();
                    yield break;
                }
            }
            else
            {
                currentStilTime = 0.0f;
            }

            currentStilTime += 0.1f;
        }
    }

    private IEnumerator SyncHipsBone()
    {
        BoneSyncData boneSyncData = new BoneSyncData();

        while (true)
        {
            boneSyncData.position = hipsBone.position;
            boneSyncData.rotation = hipsBone.rotation;

            SyncHipsBoneServerRpc(boneSyncData);

            yield return new WaitForSeconds(0.1f);
        }
    }

    [ServerRpc]
    private void SyncHipsBoneServerRpc(BoneSyncData data)
    {
        SyncHipsBoneClientRpc(data);
    }

    [ClientRpc]
    private void SyncHipsBoneClientRpc(BoneSyncData data)
    {
        if (IsOwner) { return; }

        if (Vector3.Distance(hipsBone.position, data.position) >= 0.1f)
        {
            hipsBone.MovePosition(Vector3.Lerp(hipsBone.position, data.position, 0.1f));
        }

        if (Quaternion.Angle(hipsBone.rotation, data.rotation) >= 0.1f)
        {
            hipsBone.MoveRotation(Quaternion.Lerp(hipsBone.rotation, data.rotation, 0.1f));
        }
    }
}
