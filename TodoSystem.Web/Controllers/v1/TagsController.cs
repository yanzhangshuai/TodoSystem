using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoSystem.Application.Services.Auth;
using TodoSystem.Application.Services.Todo;
using TodoSystem.Application.Services.Todo.Dtos;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Controllers.v1;

/// <summary>标签。用户私有，同名标签在不同用户之间互不影响。</summary>
[ApiController]
[ApiVersion("1")]
[Route("v{Version:apiVersion}/tags")]
[Authorize]
public class TagsController(
    ICurrentUser currentUser,
    ITagService tags) : TodoControllerBase(currentUser)
{
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<TagDto>>> ListAsync(CancellationToken ct)
        => ApiResult<IReadOnlyList<TagDto>>.Ok(await tags.ListAsync(CurrentUserId, ct));

    [HttpPost]
    public async Task<ApiResult<TagDto>> CreateAsync(
        [FromBody] CreateTagRequest request,
        CancellationToken ct)
        => ApiResult<TagDto>.Ok(await tags.CreateAsync(CurrentUserId, request, ct));

    [HttpPut("{id:int}")]
    public async Task<ApiResult<TagDto>> UpdateAsync(
        int id,
        [FromBody] UpdateTagRequest request,
        CancellationToken ct)
        => ApiResult<TagDto>.Ok(await tags.UpdateAsync(CurrentUserId, id, request, ct));

    /// <summary>删除标签。与待办的关联会一并清理，待办本身不受影响。</summary>
    [HttpDelete("{id:int}")]
    public async Task<ApiResult> DeleteAsync(int id, CancellationToken ct)
    {
        await tags.DeleteAsync(CurrentUserId, id, ct);

        return ApiResult.Ok();
    }
}
