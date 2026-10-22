using UnityEngine;
using UnityEngine.EventSystems;

public class ModernButtonStyle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.03f;
    [SerializeField] private float animationSpeed = 12f;
    private Vector3 startScale;
    private Vector3 targetScale;

    private void Awake()
    {
        startScale = transform.localScale;
        targetScale = startScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = startScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = startScale;
    }

    private void OnDisable()
    {
        transform.localScale = startScale;
        targetScale = startScale;
    }
}
