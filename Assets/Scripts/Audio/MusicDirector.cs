using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MusicDirector : MonoBehaviour
{
    public static MusicDirector Instance { get; private set; }

    public event Action TrackChanged;

    [SerializeField] SoundtrackCatalog catalog;

    readonly List<SoundtrackCatalog.Track> _queue = new List<SoundtrackCatalog.Track>();
    readonly HashSet<SoundtrackCatalog.Track> _loading = new HashSet<SoundtrackCatalog.Track>();
    AudioSource _source;
    int _index = -1;
    float _retryTimer;
    float _playedTime;
    float _stoppedTimer;
    bool _heardPlay;
    bool _busy;

    public AudioSource Source => _source;
    public SoundtrackCatalog.Track Current { get; private set; }

    public static MusicDirector Ensure()
    {
        if (Instance != null)
            return Instance;
        var go = new GameObject("MusicDirector");
        return go.AddComponent<MusicDirector>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;

        if (catalog == null)
            catalog = Resources.Load<SoundtrackCatalog>("SoundtrackCatalog");

        BuildShuffledQueue();
        PlayNext();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (_queue.Count == 0 || _source == null || _busy || _source.clip == null)
            return;

        if (_source.isPlaying)
        {
            _heardPlay = true;
            _playedTime += Time.unscaledDeltaTime;
            _stoppedTimer = 0f;
            return;
        }

        if (!_heardPlay)
        {
            _retryTimer += Time.unscaledDeltaTime;
            if (_retryTimer >= 0.4f)
            {
                _retryTimer = 0f;
                _source.Play();
            }
            return;
        }

        _stoppedTimer += Time.unscaledDeltaTime;
        if (_stoppedTimer < 0.2f)
            return;

        if (_playedTime < 0.5f)
        {
            _stoppedTimer = 0f;
            _source.Play();
            return;
        }

        PlayNext();
    }

    void BuildShuffledQueue()
    {
        _queue.Clear();
        if (catalog == null)
            return;

        var source = catalog.TracksFor(GameSession.Selected);
        if (source == null)
            return;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null && !string.IsNullOrEmpty(source[i].path))
                _queue.Add(source[i]);
        }

        var rng = new System.Random();
        for (int i = _queue.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var tmp = _queue[i];
            _queue[i] = _queue[j];
            _queue[j] = tmp;
        }
    }

    void PlayNext()
    {
        if (_queue.Count == 0 || _source == null)
            return;

        _index = (_index + 1) % _queue.Count;
        Current = _queue[_index];
        _heardPlay = false;
        _retryTimer = 0f;
        _playedTime = 0f;
        _stoppedTimer = 0f;
        StartCoroutine(PlayCurrent());
    }

    IEnumerator PlayCurrent()
    {
        _busy = true;
        var track = Current;
        yield return LoadClip(track);
        if (_source == null || track == null || track.clip == null || Current != track)
        {
            _busy = false;
            yield break;
        }

        _source.clip = track.clip;
        _heardPlay = false;
        _retryTimer = 0f;
        _playedTime = 0f;
        _stoppedTimer = 0f;
        _source.Play();
        TrackChanged?.Invoke();
        _busy = false;
        StartCoroutine(PrefetchRest());
    }

    IEnumerator PrefetchRest()
    {
        for (int i = 0; i < _queue.Count; i++)
        {
            var track = _queue[i];
            if (track == null || track.clip != null || string.IsNullOrEmpty(track.path))
                continue;
            yield return LoadClip(track);
        }
    }

    IEnumerator LoadClip(SoundtrackCatalog.Track track)
    {
        if (track == null || track.clip != null || string.IsNullOrEmpty(track.path))
            yield break;

        while (_loading.Contains(track))
            yield return null;

        if (track.clip != null)
            yield break;

        _loading.Add(track);
        string url = StreamingUrl(track.path);
        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                track.clip = DownloadHandlerAudioClip.GetContent(req);
            else
                Debug.LogWarning("Failed to load track " + track.path + ": " + req.error);
        }
        _loading.Remove(track);
    }

    static string StreamingUrl(string relativePath)
    {
        string url = Application.streamingAssetsPath + "/" + relativePath.Replace(" ", "%20");
        if (url.IndexOf("://", StringComparison.Ordinal) < 0)
            url = "file://" + url;
        return url;
    }
}
