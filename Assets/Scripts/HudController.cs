using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates the on-screen texts (remaining amount in the corner, ready items, delivery info, messages).
/// </summary>
public class HudController : MonoBehaviour
{
    [SerializeField] private Text amountText;
    [SerializeField] private Text floorText;
    [SerializeField] private Text readyText;
    [SerializeField] private Text deliveryText;
    [SerializeField] private Text messageText;

    private float messageTimeLeft;

    private void Update()
    {
        if (messageTimeLeft > 0f)
        {
            messageTimeLeft -= Time.deltaTime;
            if (messageTimeLeft <= 0f)
            {
                messageText.text = "";
            }
        }
    }

    public void SetAmountText(string text)
    {
        amountText.text = text;
    }

    public void SetFloorText(string text)
    {
        floorText.text = text;
    }

    public void SetReadyText(string text)
    {
        readyText.text = text;
    }

    public void SetDeliveryText(string text)
    {
        deliveryText.text = text;
    }

    public void ShowMessage(string text, float duration)
    {
        messageText.text = text;
        messageTimeLeft = duration;
    }
}
