using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Lean60/Soundtrack Catalog", fileName = "SoundtrackCatalog")]
public class SoundtrackCatalog : ScriptableObject
{
    [Serializable]
    public class Track
    {
        public string path;
        public string title;
        public string artist;
        public Color accent = new Color(0.55f, 0.35f, 0.85f, 1f);

        [NonSerialized] public AudioClip clip;
    }

    public Track[] normal = Array.Empty<Track>();
    public Track[] hard = Array.Empty<Track>();

    public Track[] TracksFor(GameSession.Difficulty difficulty)
    {
        return difficulty == GameSession.Difficulty.Hard ? hard : normal;
    }
}
