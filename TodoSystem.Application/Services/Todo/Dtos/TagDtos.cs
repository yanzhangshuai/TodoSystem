namespace TodoSystem.Application.Services.Todo.Dtos;

/// <summary>标签读模型。</summary>
public record TagDto(
    int Id,
    string Name,
    string? Color);

public record CreateTagRequest(
    string Name,
    string? Color);

/// <summary>整体替换语义（PUT），<see cref="Color"/> 传 null 表示清空颜色。</summary>
public record UpdateTagRequest(
    string Name,
    string? Color);

/// <summary>给待办挂标签。已挂上的会被忽略，因此可以安全重试（幂等）。</summary>
public record AttachTagsRequest(IReadOnlyList<int> TagIds);
