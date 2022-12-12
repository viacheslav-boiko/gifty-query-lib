namespace GiftyQueryLib.Enums
{
    /// <summary>
    /// Case type represents how to form database object names<br/>
    /// <b>Camel</b> => CamelCaseText<br/>
    /// <b>Snake</b> => snake_case_text<br/>
    /// <b>Custom</b> - To define your own pattern
    /// </summary>
    public enum CaseType
    {
        Snake, Camel, Custom
    }

    public enum OrderType
    {
        Asc, Desc
    }

    public enum JoinType
    {
        Inner, Left, Right, Full, Cross
    }

    public enum SelectType
    {
        All
    }
}
