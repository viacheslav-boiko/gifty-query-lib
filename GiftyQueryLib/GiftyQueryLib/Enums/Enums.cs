namespace GiftyQueryLib.Enums
{
    /// <summary>
    /// Case type represents how to form database object names<br/>
    /// <b>Camel</b> => CamelCaseText<br/>
    /// <b>Snake</b> => snake_case_text<br/>
    /// <b>None</b> - If custom formatter func is not defined, names will be taken as it is
    /// </summary>
    public enum CaseType
    {
        Snake, Camel, None
    }

    /// <summary>
    /// Order dirction for sorting<br/>
    /// <b>Asc</b> - Ascending<br/>
    /// <b>Desc</b> - Descending<br/>
    /// <b>None</b> - No ordering
    /// </summary>
    public enum OrderType
    {
        Asc, Desc, None
    }
}
