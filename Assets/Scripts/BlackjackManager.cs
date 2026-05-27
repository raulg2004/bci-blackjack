using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BlackjackManager : MonoBehaviour
{
    public Deck deck;
    public CardSpriteDatabase spriteDatabase;
    public GameObject cardPrefab;
    public Transform dealerZone;
    public Transform playerZone;

    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI dealerScoreText;
    public TextMeshProUGUI resultText;

    [Tooltip("Horizontal offset between cards (world units).")]
    public float cardSpacing = 1.1f;

    [Tooltip("Sorting order assigned to the first card. Each subsequent card +1. Set higher than the table/background sprite.")]
    public int cardBaseSortingOrder = 10;

    [Tooltip("Sorting layer name to use for cards. Leave 'Default' if you have no custom layer.")]
    public string cardSortingLayer = "Default";

    private readonly List<Card> playerHand = new List<Card>();
    private readonly List<Card> dealerHand = new List<Card>();
    private readonly List<GameObject> playerCardObjects = new List<GameObject>();
    private readonly List<GameObject> dealerCardObjects = new List<GameObject>();
    private bool gameOver = false;
    private bool holeCardHidden = false;

    void Start()
    {
        // Don't auto-deal. The game waits for the player to look at the
        // BLUE (New Game) circle after BCI training completes.
        if (resultText != null) resultText.text = "";
        if (playerScoreText != null) playerScoreText.gameObject.SetActive(false);
        if (dealerScoreText != null) dealerScoreText.gameObject.SetActive(false);
        gameOver = true; // block Hit/Stand until first NewGame
    }

    public void NewGame()
    {
        ClearTable();
        gameOver = false;
        holeCardHidden = true;
        if (resultText != null) resultText.text = "";
        if (playerScoreText != null) playerScoreText.gameObject.SetActive(true);
        if (dealerScoreText != null) dealerScoreText.gameObject.SetActive(true);

        DealCardToPlayer(deck.DrawCard());
        DealCardToDealer(deck.DrawCard(), faceDown: false);
        DealCardToPlayer(deck.DrawCard());
        DealCardToDealer(deck.DrawCard(), faceDown: true); // hole card

        UpdateScoreUI();

        // Natural blackjack check.
        if (GetHandValue(playerHand) == 21)
        {
            RevealHoleCard();
            if (GetHandValue(dealerHand) == 21)
                EndGame("Push - both have Blackjack!");
            else
                EndGame("BLACKJACK! You win!");
        }
    }

    public void Hit()
    {
        if (gameOver) return;
        DealCardToPlayer(deck.DrawCard());
        UpdateScoreUI();

        int total = GetHandValue(playerHand);
        if (total > 21)
            EndGame("BUST! Dealer wins!");
        else if (total == 21)
            Stand();
    }

    public void Stand()
    {
        if (gameOver) return;

        RevealHoleCard();

        while (GetHandValue(dealerHand) < 17)
            DealCardToDealer(deck.DrawCard(), faceDown: false);

        UpdateScoreUI();
        CheckWinner();
    }

    void ClearTable()
    {
        foreach (GameObject c in playerCardObjects) if (c != null) Destroy(c);
        foreach (GameObject c in dealerCardObjects) if (c != null) Destroy(c);
        playerCardObjects.Clear();
        dealerCardObjects.Clear();
        playerHand.Clear();
        dealerHand.Clear();
    }

    void DealCardToPlayer(Card card)
    {
        playerHand.Add(card);
        GameObject cardObj = SpawnCard(playerZone, playerCardObjects.Count);
        cardObj.GetComponent<CardDisplay>().ShowCard(card, spriteDatabase);
        playerCardObjects.Add(cardObj);
    }

    void DealCardToDealer(Card card, bool faceDown)
    {
        dealerHand.Add(card);
        GameObject cardObj = SpawnCard(dealerZone, dealerCardObjects.Count);
        var display = cardObj.GetComponent<CardDisplay>();
        if (faceDown) display.ShowBack(spriteDatabase);
        else display.ShowCard(card, spriteDatabase);
        dealerCardObjects.Add(cardObj);
    }

    GameObject SpawnCard(Transform zone, int indexInZone)
    {
        GameObject obj = Instantiate(cardPrefab, zone);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        // Place the card in world space, but FORCE world Z = 0 so it never
        // ends up at/behind the camera's near clip plane (the bug we just
        // fixed: zones were at z=-10, same as the camera).
        Vector3 world = zone.position + new Vector3(indexInZone * cardSpacing, 0f, 0f);
        world.z = 0f;
        obj.transform.position = world;

        // Force cards to render in front of the table / background.
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = cardSortingLayer;
            sr.sortingOrder = cardBaseSortingOrder + indexInZone;
        }

        // If the prefab is UI-based, make sure it's on a Canvas; nothing to
        // sort here - sibling order in the Canvas controls draw order.
        return obj;
    }

    void RevealHoleCard()
    {
        if (!holeCardHidden) return;
        holeCardHidden = false;
        // Hole card is the second dealer card (index 1).
        if (dealerCardObjects.Count >= 2 && dealerHand.Count >= 2)
        {
            dealerCardObjects[1].GetComponent<CardDisplay>()
                .ShowCard(dealerHand[1], spriteDatabase);
        }
        UpdateScoreUI();
    }

    int GetHandValue(List<Card> hand)
    {
        int total = 0;
        int aces = 0;

        foreach (Card c in hand)
        {
            total += c.GetBlackjackValue();
            if (c.value == 1) aces++;
        }

        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }

        return total;
    }

    void CheckWinner()
    {
        int player = GetHandValue(playerHand);
        int dealer = GetHandValue(dealerHand);

        if (dealer > 21) EndGame("Dealer busted! You win!");
        else if (player > dealer) EndGame("You win!");
        else if (dealer > player) EndGame("Dealer wins!");
        else EndGame("Push - it's a tie!");
    }

    void EndGame(string message)
    {
        if (resultText != null) resultText.text = message;
        gameOver = true;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (playerScoreText != null)
            playerScoreText.text = "Player: " + GetHandValue(playerHand);

        if (dealerScoreText != null)
        {
            if (holeCardHidden && dealerHand.Count >= 1)
                dealerScoreText.text = "Dealer: " + dealerHand[0].GetBlackjackValue() + " + ?";
            else
                dealerScoreText.text = "Dealer: " + GetHandValue(dealerHand);
        }
    }
}
