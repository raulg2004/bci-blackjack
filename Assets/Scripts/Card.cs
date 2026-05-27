using UnityEngine;

public class Card
{
    public string suit;
    public int value;

    public int GetBlackjackValue()
    {
        if (value >= 10) return 10;
        if (value == 1) return 11;
        return value;
    }
}