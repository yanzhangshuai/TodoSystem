namespace TodoSystem.Domain.Constants;

/// <summary>
/// 待办优先级取值。与 <c>ck_todo_items_priority</c> 数据库约束保持一致。
/// </summary>
/// <remarks>用字符串常量而非 enum 的理由同 <see cref="TodoStatuses"/>。</remarks>
public static class TodoPriorities
{
    public const string Low = "LOW";

    public const string Normal = "NORMAL";

    public const string High = "HIGH";

    public const string Urgent = "URGENT";

    /// <summary>全部合法取值。</summary>
    public static readonly IReadOnlyList<string> All = [Low, Normal, High, Urgent];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
