namespace TodoSystem.Web.Utils.Route;

/// <summary>
/// 把出站路由参数转小写。名字里的 Kebab 有误导 —— 只转小写，不插连字符。
/// </summary>
public class KebabCaseParameterTransformer: IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        return value?.ToString()?.ToLowerInvariant();
    }
}