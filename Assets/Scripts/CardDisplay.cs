using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a card sprite. Works with either a SpriteRenderer (world-space
/// card prefab) or a UI Image (canvas-based card prefab) - whichever is
/// present on the prefab.
/// </summary>
public class CardDisplay : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Image uiImage;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
    }

    public void ShowCard(Card card, CardSpriteDatabase db)
    {
        Sprite sprite = db.GetSprite(card);
        if (sprite == null)
        {
            Debug.LogError($"Card sprite missing: {card.value}_{card.suit}");
            return;
        }
        SetSprite(sprite);
    }

    public void ShowBack(CardSpriteDatabase db)
    {
        if (db.cardBack == null) { Debug.LogError("Card back sprite missing"); return; }
        SetSprite(db.cardBack);
    }

    private void SetSprite(Sprite s)
    {
        if (spriteRenderer != null) spriteRenderer.sprite = s;
        if (uiImage != null)        uiImage.sprite = s;
    }
}
