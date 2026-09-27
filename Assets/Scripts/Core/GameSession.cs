public static class GameSession
{
    public enum Difficulty
    {
        Normal,
        Hard
    }

    public static Difficulty Selected { get; set; } = Difficulty.Normal;
    public static bool HasChosen { get; set; }
}
