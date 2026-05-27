using UnityEngine;

public class CardSpriteDatabase : MonoBehaviour
{
    [Header("Drag all 13 Hearts sprites here (1..13)")]
    public Sprite[] hearts;

    [Header("Drag all 13 Diamonds sprites here (1..13)")]
    public Sprite[] diamonds;

    [Header("Drag all 13 Clubs sprites here (1..13)")]
    public Sprite[] clubs;

    [Header("Drag all 13 Spades sprites here (1..13)")]
    public Sprite[] spades;

    [Header("Card Back")]
    public Sprite cardBack;

    public Sprite GetSprite(Card card)
    {
        Sprite[] suitArray = GetSuitArray(card.suit);
        if (suitArray == null || card.value < 1 || card.value > suitArray.Length) return null;
        return suitArray[card.value - 1];
    }

    Sprite[] GetSuitArray(string suit)
    {
        switch (suit)
        {
            case "heart":   return hearts;
            case "diamond": return diamonds;
            case "club":    return clubs;
            case "spade":   return spades;
            default: return null;
        }
    }
}
