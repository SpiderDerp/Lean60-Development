using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum RunState
    {
        Playing,
        Won,
        Lost
    }

    [SerializeField] float roundSeconds = 60f;
    [SerializeField] float countdownSlowdown = 1.5f;
    [SerializeField] SpawnPoint spawnPoint;
    [SerializeField] PlayerController player;
    [SerializeField] Text timerText;
    [SerializeField] Image timerRing;
    [SerializeField] Text statusText;

    int _deaths;
    int _shownSeconds = int.MinValue;
    RunState _shownState = (RunState)(-1);

    public RunState State { get; private set; } = RunState.Playing;
    public float TimeRemaining { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        TimeRemaining = roundSeconds;
    }

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        if (spawnPoint == null)
            spawnPoint = FindFirstObjectByType<SpawnPoint>();

        if (player != null && spawnPoint != null)
            player.TeleportTo(spawnPoint.Position);

        UpdateUi();
    }

    void Update()
    {
        if (State != RunState.Playing)
            return;

        float slowdown = countdownSlowdown > 0f ? countdownSlowdown : 1f;
        TimeRemaining -= Time.deltaTime / slowdown;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            OnTimerExpired();
        }

        UpdateUi();
    }

    public void NotifyPlayerDied()
    {
        if (State != RunState.Playing)
            return;

        _deaths++;
        if (_deaths % 5 == 0)
            MusicVisualizer.RandomizePreset();

        RespawnPlayer();
    }

    public void Win()
    {
        if (State != RunState.Playing)
            return;

        State = RunState.Won;
        Time.timeScale = 0f;
        UpdateUi();
    }

    void OnTimerExpired()
    {
        if (player != null)
            player.KillFromTimer();
        else
            RespawnPlayer();
    }

    void RespawnPlayer()
    {
        TimeRemaining = roundSeconds;
        State = RunState.Playing;
        Time.timeScale = 1f;

        LevelResetter.ResetLevel();

        if (player != null && spawnPoint != null)
            player.RespawnAt(spawnPoint.Position);

        UpdateUi();
    }

    void UpdateUi()
    {
        int seconds = Mathf.CeilToInt(TimeRemaining);
        if (timerText != null && seconds != _shownSeconds)
        {
            _shownSeconds = seconds;
            timerText.text = seconds.ToString();
        }

        if (timerRing != null && roundSeconds > 0.01f)
            timerRing.fillAmount = Mathf.Clamp01(TimeRemaining / roundSeconds);

        if (statusText == null || State == _shownState)
            return;

        _shownState = State;
        switch (State)
        {
            case RunState.Won:
                statusText.text = "YOU WIN";
                break;
            case RunState.Lost:
                statusText.text = "TIME UP";
                break;
            default:
                statusText.text = string.Empty;
                break;
        }
    }

    public void BindReferences(PlayerController playerController, SpawnPoint spawn, Text timer, Text status, Image ring = null)
    {
        player = playerController;
        spawnPoint = spawn;
        timerText = timer;
        statusText = status;
        if (ring != null)
            timerRing = ring;
    }

    public void SetTimerRing(Image ring)
    {
        timerRing = ring;
    }
}
