using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class AlphaTargetUI : MonoBehaviour, IAlphaTarget
{
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
    }
}
