using TodoSystem.Domain.Constants;
using TodoSystem.Infrastructure.Exceptions;

namespace TodoSystem.Application.Services.Todo;

/// <summary>
/// 待办领域共用的入参校验。集中一处，保证清单 / 待办 / 标签对「必填、空白、超长」
/// 的处理规则一致 —— 三个服务各写一遍迟早会分叉。
/// </summary>
internal static class TodoValidation
{
    /// <summary>必填文本：去空格后不能为空，且不超长。</summary>
    public static string RequireText(string? value, string fieldName, int maxLength)
    {
        var text = value?.Trim();

        if (string.IsNullOrEmpty(text))
        {
            throw new CustomException(400, $"{fieldName}不能为空");
        }

        if (text.Length > maxLength)
        {
            throw new CustomException(400, $"{fieldName}不能超过 {maxLength} 个字符");
        }

        return text;
    }

    /// <summary>
    /// 可选文本：纯空白视为「没填」，统一存 null。
    /// 否则库里会同时出现 null 和 "" 两种「空」，前端每次判断都要写两遍。
    /// </summary>
    public static string? OptionalText(string? value, string fieldName, int maxLength)
    {
        var text = value?.Trim();

        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (text.Length > maxLength)
        {
            throw new CustomException(400, $"{fieldName}不能超过 {maxLength} 个字符");
        }

        return text;
    }

    /// <summary>校验状态取值，返回规范化后的大写形式。</summary>
    public static string RequireStatus(string? value)
    {
        var status = value?.Trim().ToUpperInvariant();

        if (!TodoStatuses.IsValid(status))
        {
            throw new CustomException(
                400,
                $"状态取值必须是 {string.Join(" / ", TodoStatuses.All)} 之一");
        }

        return status!;
    }

    /// <summary>校验优先级取值，返回规范化后的大写形式。</summary>
    public static string RequirePriority(string? value)
    {
        var priority = value?.Trim().ToUpperInvariant();

        if (!TodoPriorities.IsValid(priority))
        {
            throw new CustomException(
                400,
                $"优先级取值必须是 {string.Join(" / ", TodoPriorities.All)} 之一");
        }

        return priority!;
    }
}
