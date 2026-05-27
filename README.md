# BCI Blackjack

A hands-free blackjack game played with an EEG headset. Three flashing
circles act as buttons — focus your gaze on one and the BCI classifier
triggers the action.

| Circle | Action    |
| ------ | --------- |
| Red    | Hit       |
| Yellow | Stand     |
| Blue   | New Game  |

Standard blackjack rules apply: dealer stands on 17, blackjack pays 3-to-2,
aces count as 1 or 11, busts and pushes are detected automatically.

## How to start

1. Open the project in **Unity** (2D, Unity 2022+).
2. Plug in the **g.tec Unicorn BCI** headset.
3. Open the scene `Assets/Game/card_game.unity`.
4. Press **Play**.
5. Click the **START** button to begin BCI training.
6. When training completes, focus on the **BLUE** circle to deal the first hand.
7. Use **RED** to hit, **YELLOW** to stand, **BLUE** to start a new round.

**Keyboard fallback** (for testing without the headset):
`H` = Hit, `S` = Stand, `N` = New Game.

## Tech

- Unity 2D + C#
- g.tec Unicorn BCI + Unity Interface SDK (Visual ERP 2D paradigm)
- TextMeshPro
- Procedurally rendered casino table

## Team

- Catalin Babos
- Raul Peres
- Ionut Trofin
- Rares Tomoiaga
- Andrei Cojerean
- Ioana Cirja
