namespace TodoSystem.Web.Utils.Route;

/// <summary>
/// router 转小写
/// </summary>
public class KebabCaseParameterTransformer: IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        return value?.ToString()?.ToLowerInvariant();
    }
}