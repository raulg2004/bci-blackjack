using UnityEngine;
using UnityEngine.UI;
using Gtec.UnityInterface;
using ClassSelection = Gtec.Chain.Common.Templates.Utilities.ClassSelection;

/// <summary>
/// Bridges g.tec BCI Visual ERP 2D selections to the blackjack game.
///
/// Auto-subscribes to ERPPipeline.OnClassSelection at startup, so the
/// flashing circles act as buttons - focus on one for ~1s and the
/// classifier fires its classId here. We map classIds to game actions:
///
///   classId == hitClassId      → HIT       (red circle)
///   classId == standClassId    → STAND     (yellow circle)
///   classId == newGameClassId  → NEW GAME  (blue circle)
///
/// g.tec class IDs are configured per-flash via the ERPFlashTag2D
/// component on each circle in the BCI Visual ERP 2D prefab. They are
/// usually 1, 2, 3 (1-indexed). If your prefab uses different IDs, edit
/// the fields below in the Inspector. Every selection is logged so you
/// can verify the mapping at runtime.
/// </summary>
public class BCIGameController : MonoBehaviour
{
    public BlackjackManager blackjackManager;
    [Tooltip("Optional: notified when a hand is dealt so it can hide its status text.")]
    public GameFlowController gameFlow;

    [Header("Optional fallback UI buttons (mouse / keyboard for testing)")]
    public Button hitButton;
    public Button standButton;
    public Button newGameButton;

    [Header("g.tec class ID mapping (match ERPFlashTag2D.ClassId on each circle)")]
    [Tooltip("Class ID of the RED circle.")]
    public int hitClassId = 1;
    [Tooltip("Class ID of the YELLOW circle.")]
    public int standClassId = 2;
    [Tooltip("Class ID of the BLUE circle.")]
    public int newGameClassId = 3;

    [Header("State")]
    [Tooltip("If true, BCI selections are dropped on the floor until EnableInput() is called.")]
    public bool requireEnable = true;
    [SerializeField] private bool inputEnabled = false;

    [Header("Cooldown")]
    [Tooltip("Seconds to ignore further BCI selections after one is accepted. Prevents the headset from spamming the same circle.")]
    public float selectionCooldownSeconds = 5f;
    private float _lastAcceptedAt = -999f;

    public bool InputEnabled => inputEnabled || !requireEnable;

    // Backwards-compat aliases (older inspector references).
    public int hitIndex     { get => hitClassId;     set => hitClassId = value; }
    public int standIndex   { get => standClassId;   set => standClassId = value; }
    public int newGameIndex { get => newGameClassId; set => newGameClassId = value; }

    private ERPPipeline _pipeline;

    void Start()
    {
        if (hitButton != null)     hitButton.onClick.AddListener(Hit);
        if (standButton != null)   standButton.onClick.AddListener(Stand);
        if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);

        // Auto-wire the BCI class-selection event. The pipeline lives on a
        // child of the BCI Visual ERP 2D prefab in the scene.
        _pipeline = FindObjectOfType<ERPPipeline>();
        if (_pipeline != null)
        {
            _pipeline.OnClassSelection.AddListener(HandlePipelineClassSelection);
            Debug.Log("BCIGameController: subscribed to ERPPipeline.OnClassSelection.");
        }
        else
        {
            Debug.LogWarning("BCIGameController: no ERPPipeline found in scene. " +
                             "BCI selections will not auto-trigger.");
        }
    }

    void OnDestroy()
    {
        if (_pipeline != null)
            _pipeline.OnClassSelection.RemoveListener(HandlePipelineClassSelection);
    }

    // Bridges the pipeline's two-arg event into our int-based handler.
    // ClassSelection exposes the focused class via its 'Class' property.
    private void HandlePipelineClassSelection(ERPPipeline pipeline, ClassSelection selection)
    {
        if (selection == null) return;
        int classId = (int)selection.Class;
        OnBCISelection(classId);
    }

    void Update()
    {
        // Keyboard fallback for testing without the headset.
        if (Input.GetKeyDown(KeyCode.H)) Hit();
        else if (Input.GetKeyDown(KeyCode.S)) Stand();
        else if (Input.GetKeyDown(KeyCode.N)) NewGame();
    }

    public void EnableInput()
    {
        inputEnabled = true;
        Debug.Log("BCIGameController: input ENABLED - circles now control the game.");
    }

    public void DisableInput()
    {
        inputEnabled = false;
        Debug.Log("BCIGameController: input DISABLED.");
    }

    /// Receives the classId of the flash the user just selected with their gaze.
    /// Fires automatically (via OnClassSelection) and can also be invoked manually.
    public void OnBCISelection(int classId)
    {
        Debug.Log($"BCI selection received: classId = {classId} (enabled={InputEnabled})");

        if (!InputEnabled) return;

        float now = Time.unscaledTime;
        float remaining = (_lastAcceptedAt + selectionCooldownSeconds) - now;
        if (remaining > 0f)
        {
            Debug.Log($"BCI selection ignored (cooldown: {remaining:F1}s left).");
            return;
        }

        bool accepted = true;
        if (classId == hitClassId)          { Debug.Log("→ HIT");      Hit(); }
        else if (classId == standClassId)   { Debug.Log("→ STAND");    Stand(); }
        else if (classId == newGameClassId) { Debug.Log("→ NEW GAME"); NewGame(); }
        else
        {
            Debug.LogWarning($"Unmapped classId: {classId}. " +
                             "Edit hit/stand/newGame ClassId on BCIGameController.");
            accepted = false;
        }

        if (accepted) _lastAcceptedAt = now;
    }

    public void Hit()     { if (blackjackManager != null) blackjackManager.Hit(); }
    public void Stand()   { if (blackjackManager != null) blackjackManager.Stand(); }
    public void NewGame()
    {
        if (blackjackManager != null) blackjackManager.NewGame();
        if (gameFlow != null) gameFlow.MarkGameStarted();
    }
}
