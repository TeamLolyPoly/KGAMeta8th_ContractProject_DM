#region Note Related
public enum NoteType
{
    None,
    Short,
    Long,
}

public enum NoteDirection
{
    None,
    East,
    West,
    South,
    North,
    NorthEast,
    NorthWest,
    SouthEast,
    SouthWest,
}

public enum NoteAxis
{
    PZ,
    MZ,
    PX,
    MX,
}

public enum NoteColor
{
    None,
    Red,
    Blue,
    Yellow,
}

public enum NoteRatings
{
    None,
    Miss,
    Perfect,
    Great,
    Good,
    Success,
}

#endregion

public enum BandType
{
    Drums,
    Saxophone,
    Guitar,
    Bass_Guitar,
}

public enum BeatsPerBar
{
    Four,
    Eight,
    Six,
    Twelve,
    Sixteen,
    ThirtyTwo,
}

public enum Engagement
{
    First = 0,
    Second,
    Third,
    Fourth,
    Fifth,
    Sixth,
    Seventh,
    Eighth,
    Ninth,
    Tenth,
}

public enum PanelType
{
    Title,
    Mode,
    Album,
    Music,
    DifficultySelect,
    ResultDetail,
    Result,
    Option,
    Scoreboard,
    Loading,
    PopUp,

    // 에디터 전용
    NoteEditor,
    EditorStart,
    NewTrack,
    LoadTrack,
    EditorSettings,
}

public enum Difficulty
{
    None,
    Easy,
    Normal,
    Hard,
}
