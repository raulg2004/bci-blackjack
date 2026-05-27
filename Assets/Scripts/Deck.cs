using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    private List<Card> cards = new List<Card>();
    private static readonly string[] Suits = { "heart", "diamond", "club", "spade" };

    void Awake()
    {
        BuildAndShuffle();
    }

    public void BuildAndShuffle()
    {
        cards.Clear();
        foreach (string suit in Suits)
            for (int i = 1; i <= 13; i++)
                cards.Add(new Card { suit = suit, value = i });

        for (int i = 0; i < cards.Count; i++)
        {
            int rand = Random.Range(i, cards.Count);
            Card temp = cards[i];
            cards[i] = cards[rand];
            cards[rand] = temp;
        }
    }

    public Card DrawCard()
    {
        if (cards.Count < 10) BuildAndShuffle();
        Card drawn = cards[0];
        cards.RemoveAt(0);
        return drawn;
    }
}
