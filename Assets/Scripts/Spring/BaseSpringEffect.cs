using UnityEngine;

public abstract class BaseSpringEffect : MonoBehaviour
{
    public abstract void OnSpringUpdate(float springValue, float springVelocity);
}
