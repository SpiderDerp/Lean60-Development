using System;
using System.Collections.Generic;
using UnityEngine;

public class MusicDirector : MonoBehaviour
{
    public static MusicDirector Instance { get; private set; }

    public event Action TrackChanged;

    [SerializeField] SoundtrackCatalog catalog;

    readonly List<SoundtrackCatalog.Track> _queue = new List<SoundtrackCatalog.Track>();
    AudioSource _source;
    int _index = -1;
    float _retryTimer;

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
        if (_queue.Count == 0 || _source == null || _source.clip == null)
            return;

        if (_source.isPlaying)
            return;

        if (_source.time >= Mathf.Max(0.05f, _source.clip.length - 0.08f))
        {
            PlayNext();
            return;
        }

        _retryTimer += Time.unscaledDeltaTime;
        if (_retryTimer >= 0.4f)
        {
            _retryTimer = 0f;
            _source.Play();
        }
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
            if (source[i] != null && source[i].clip != null)
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
        _source.clip = Current.clip;
        _source.Play();
        TrackChanged?.Invoke();
    }
}
