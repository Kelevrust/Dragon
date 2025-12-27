using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CardDisplay cardDisplay;

    void Start()
    {
        cardDisplay = GetComponent<CardDisplay>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cardDisplay != null && TooltipManager.instance != null)
        {
            // Pass the entire card component so the manager can clone the visuals
            TooltipManager.instance.Show(cardDisplay);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.instance != null)
        {
            TooltipManager.instance.Hide();
        }
    }
}