using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AlphaTargetGameObject : MonoBehaviour, IAlphaTarget
{
    private new Renderer renderer;

    private void Awake()
    {
        renderer = GetComponent<Renderer>();
    }

    public void SetAlpha(float alpha)
    {
        Color c = renderer.material.color;
        renderer.material.color = new Color(c.r, c.g, c.b, alpha);
    }
}
