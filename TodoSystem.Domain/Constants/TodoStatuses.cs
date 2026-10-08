namespace TodoSystem.Domain.Constants;

/// <summary>
/// 待办状态取值。与 ck_todo_items_status 数据库约束保持一致。
/// </summary>
/// <remarks>
/// 用字符串常量而不是 enum：这个值同时出现在数据库、API 响应和前端联合类型里，
/// 用 enum 就要额外配 HasConversion（落库）和 JsonStringEnumConverter（出参）两套转换，
/// 两处命名规则独立配置，不一致就会出现「库里是 TODO、接口返回 Todo」这种错位。
/// </remarks>
public static class TodoStatuses
{
    public const string Todo = "TODO";

    public const string InProgress = "IN_PROGRESS";

    public const string Done = "DONE";

    /// <summary>全部合法取值，供 CHECK 约束和入参校验共用。</summary>
    public static readonly IReadOnlyList<string> All = [Todo, InProgress, Done];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
