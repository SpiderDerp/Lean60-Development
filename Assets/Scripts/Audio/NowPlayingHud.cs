using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NowPlayingHud : MonoBehaviour
{
    public static NowPlayingHud Instance { get; private set; }

    [SerializeField] float holdSeconds = 4f;
    [SerializeField] float fadeSeconds = 0.45f;

    CanvasGroup _group;
    Text _title;
    Text _artist;
    Coroutine _fade;

    public static NowPlayingHud Ensure(Transform hudRoot)
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject("NowPlaying");
        go.transform.SetParent(hudRoot, false);
        return go.AddComponent<NowPlayingHud>();
    }

    void Awake()
    {
        Instance = this;
        Build();
        _group.alpha = 0f;
    }

    void OnEnable()
    {
        if (MusicDirector.Instance != null)
            MusicDirector.Instance.TrackChanged += OnTrackChanged;
    }

    void Start()
    {
        if (MusicDirector.Instance != null)
        {
            MusicDirector.Instance.TrackChanged -= OnTrackChanged;
            MusicDirector.Instance.TrackChanged += OnTrackChanged;
            if (MusicDirector.Instance.Current != null)
                OnTrackChanged();
        }
    }

    void OnDisable()
    {
        if (MusicDirector.Instance != null)
            MusicDirector.Instance.TrackChanged -= OnTrackChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnTrackChanged()
    {
        var track = MusicDirector.Instance != null ? MusicDirector.Instance.Current : null;
        if (track == null)
            return;

        _title.text = string.IsNullOrEmpty(track.title) ? (track.clip != null ? track.clip.name : "Unknown") : track.title;
        _artist.text = string.IsNullOrEmpty(track.artist) ? string.Empty : track.artist;

        if (_fade != null)
            StopCoroutine(_fade);
        _fade = StartCoroutine(ShowThenHide());
    }

    IEnumerator ShowThenHide()
    {
        yield return FadeTo(1f);
        yield return new WaitForSeconds(holdSeconds);
        yield return FadeTo(0f);
        _fade = null;
    }

    IEnumerator FadeTo(float target)
    {
        float start = _group.alpha;
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(start, target, t / fadeSeconds);
            yield return null;
        }
        _group.alpha = target;
    }

    void Build()
    {
        var rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -24f);
        rt.sizeDelta = new Vector2(420f, 92f);

        _group = gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;

        CreateText(out var heading, "Heading", "NOW PLAYING", 16, 0f);
        heading.color = new Color(1f, 1f, 1f, 0.7f);
        CreateText(out _title, "Title", string.Empty, 22, -22f);
        CreateText(out _artist, "Artist", string.Empty, 18, -48f);
        _artist.color = new Color(1f, 1f, 1f, 0.8f);
    }

    void CreateText(out Text text, string name, string value, int size, float y)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        text.fontSize = size;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.white;
        text.text = value;
        text.raycastTarget = false;
        var rt = text.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, 30f);
    }
}
