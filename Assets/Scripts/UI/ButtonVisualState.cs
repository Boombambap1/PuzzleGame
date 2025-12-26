using UnityEngine;
using UnityEngine.UI;

public class ButtonVisualState : MonoBehaviour
{
    private RectTransform rect;
    private Shadow shadow;
    private Vector2 originalPos;
    [SerializeField] private Vector2 anchorOffset = new Vector2(-6f, 6f);
    [SerializeField] private Vector2 shadowDistance = new Vector2(6f, -6f);

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        shadow = GetComponent<Shadow>();
        originalPos = rect.anchoredPosition;
        shadow.effectDistance = shadowDistance;
    }

    public void SetSelected(bool selected)
    {
        rect.anchoredPosition = originalPos + (selected ? anchorOffset : Vector2.zero);
    }
}
