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
    #region 스테이지 전용

    #region 공용
    Title,
    Loading,
    Option,
    Mode,
    AlbumSelect,
    #endregion

    #region 싱글전용
    Single_TrackSelect,
    Single_Result,
    #endregion

    #region 멀티전용
    Multi_Waiting,
    Multi_TrackSelect,
    Multi_Room,
    Multi_Result,
    Multi_Status,
    Multi_TrackDecision,
    #endregion

    #endregion

    #region 에디터 전용
    NoteEditor,
    EditorStart,
    NewTrack,
    LoadTrack,
    EditorSettings,
    #endregion
}

public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
}

public enum ResultRank
{
    Splus,
    S,
    A,
    B,
    C,
}
