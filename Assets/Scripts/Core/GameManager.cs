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
    [SerializeField] SpawnPoint spawnPoint;
    [SerializeField] PlayerController player;
    [SerializeField] Text timerText;
    [SerializeField] Text statusText;

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

        TimeRemaining -= Time.deltaTime;
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

        if (player != null && spawnPoint != null)
            player.RespawnAt(spawnPoint.Position);

        UpdateUi();
    }

    void UpdateUi()
    {
        if (timerText != null)
            timerText.text = $"Time: {Mathf.CeilToInt(TimeRemaining)}";

        if (statusText == null)
            return;

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

    public void BindReferences(PlayerController playerController, SpawnPoint spawn, Text timer, Text status)
    {
        player = playerController;
        spawnPoint = spawn;
        timerText = timer;
        statusText = status;
    }
}
