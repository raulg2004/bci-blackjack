using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Owns the boot → train → play state machine.
///
///   IDLE     : "START" button is shown. Game inputs disabled.
///   TRAINING : button hidden, status shows training instructions.
///   READY    : status tells the player to look at the BLUE circle to deal.
///   PLAYING  : (the game proceeds normally; BCI input is enabled).
///
/// Wire-up:
///   - OnStartTraining UnityEvent fires when the user clicks START.
///     Wire it to your g.tec ERPParadigmUI / ERPParadigm to begin training.
///   - When training finishes (g.tec OnClassifierAvailable / OnStartParadigm
///     with ParadigmMode.Application), call <see cref="MarkTrainingComplete"/>.
///   - For testing without a headset, press the START button - this
///     auto-completes after <see cref="autoCompleteTrainingSeconds"/>.
/// </summary>
public class GameFlowController : MonoBehaviour
{
    public BCIGameController bciController;

    [Header("Hooks")]
    [Tooltip("Fired when the user presses the START button. Wire to your BCI training trigger.")]
    public UnityEvent OnStartTraining;

    [Header("Testing without a headset")]
    [Tooltip("If > 0, training auto-completes this many seconds after START is pressed. Set to 0 to require a manual call to MarkTrainingComplete().")]
    public float autoCompleteTrainingSeconds = 3f;

    [Header("UI styling")]
    public Color buttonColor    = new Color(0.85f, 0.20f, 0.20f, 1f);
    public Color buttonTextColor = Color.white;
    public Color statusColor    = new Color(0.95f, 0.85f, 0.40f, 1f);

    private enum State { Idle, Training, Ready, Playing }
    private State _state = State.Idle;

    private Canvas _canvas;
    private Button _startButton;
    private TMP_Text _buttonLabel;
    private float _trainingStartedAt;

    void Awake()
    {
        BuildUI();
        SetState(State.Idle);
    }

    void Update()
    {
        if (_state == State.Training && autoCompleteTrainingSeconds > 0f)
        {
            if (Time.unscaledTime - _trainingStartedAt >= autoCompleteTrainingSeconds)
                MarkTrainingComplete();
        }
    }

    // ----- public API -----

    /// Call when BCI training finishes (e.g. from g.tec OnClassifierAvailable).
    public void MarkTrainingComplete()
    {
        if (_state != State.Training) return;
        SetState(State.Ready);
    }

    /// Call when the player has dealt a hand. Hides the status overlay.
    public void MarkGameStarted()
    {
        SetState(State.Playing);
    }

    // ----- internal -----

    void OnStartClicked()
    {
        if (_state != State.Idle) return;
        _trainingStartedAt = Time.unscaledTime;
        SetState(State.Training);
        OnStartTraining?.Invoke();
    }

    void SetState(State s)
    {
        _state = s;
        switch (s)
        {
            case State.Idle:
                _startButton.gameObject.SetActive(true);
                _buttonLabel.text = "START";
                if (bciController != null) bciController.DisableInput();
                break;

            case State.Training:
                _startButton.gameObject.SetActive(false);
                if (bciController != null) bciController.DisableInput();
                break;

            case State.Ready:
            case State.Playing:
                _startButton.gameObject.SetActive(false);
                if (bciController != null) bciController.EnableInput();
                break;
        }
    }

    void BuildUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("GameFlowCanvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Start button - centered
        GameObject btnGO = new GameObject("StartButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        Image bg = btnGO.AddComponent<Image>();
        bg.color = buttonColor;
        _startButton = btnGO.AddComponent<Button>();
        _startButton.targetGraphic = bg;
        _startButton.onClick.AddListener(OnStartClicked);

        var brt = btnGO.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(420, 140);

        // Button label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        _buttonLabel = labelGO.AddComponent<TextMeshProUGUI>();
        _buttonLabel.alignment = TextAlignmentOptions.Center;
        _buttonLabel.color = buttonTextColor;
        _buttonLabel.fontSize = 64;
        _buttonLabel.fontStyle = FontStyles.Bold;
        _buttonLabel.text = "START";
        var lrt = _buttonLabel.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }
}
