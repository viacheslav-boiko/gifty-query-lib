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

    /// <summary>
    /// Order dirction for sorting<br/>
    /// <b>Asc</b> - Ascending<br/>
    /// <b>Desc</b> - Descending<br/>
    /// </summary>
    public enum OrderType
    {
        Asc, Desc
    }
}
