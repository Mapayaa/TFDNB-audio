using UnityEngine;

/// <summary>
/// Script om de positie van een TextBox (TMP UI object) aan te passen bij activatie.
/// Sleep de RectTransform van je TMP-object in het 'textBox' veld in de Inspector.
/// Stel de gewenste positie in via het 'newPosition' veld.
/// </summary>
public class EndUI : MonoBehaviour
{
    [Tooltip("RectTransform van de TextBox (TMP UI object)")]
    public RectTransform textBox;

    [Tooltip("Nieuwe positie voor de TextBox (anchoredPosition)")]
    public Vector2 newPosition;

    void Start()
    {
        if (textBox != null)
        {
            textBox.anchoredPosition = newPosition;
        }
    }
}
