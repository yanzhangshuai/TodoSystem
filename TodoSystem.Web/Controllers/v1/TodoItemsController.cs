using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoSystem.Application.Services.Auth;
using TodoSystem.Application.Services.Todo;
using TodoSystem.Application.Services.Todo.Dtos;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Controllers.v1;

/// <summary>
/// 待办项。筛选、状态流转、子任务、标签关联都在这里。
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("v{Version:apiVersion}/todo-items")]
[Authorize]
public class TodoItemsController(
    ICurrentUser currentUser,
    ITodoItemService todoItems) : TodoControllerBase(currentUser)
{
    /// <summary>
    /// 按条件列出当前用户的待办。
    /// 例：<c>?statuses=TODO&amp;statuses=IN_PROGRESS&amp;listId=3</c> 取某清单下未完成的待办。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<TodoItemDto>>> ListAsync(
        [FromQuery] TodoItemQuery query,
        CancellationToken ct)
        => ApiResult<IReadOnlyList<TodoItemDto>>.Ok(
            await todoItems.ListAsync(CurrentUserId, query, ct));

    /// <summary>取单条待办详情，含子任务。</summary>
    [HttpGet("{id:int}")]
    public async Task<ApiResult<TodoItemDetailDto>> GetAsync(int id, CancellationToken ct)
        => ApiResult<TodoItemDetailDto>.Ok(await todoItems.GetAsync(CurrentUserId, id, ct));

    [HttpPost]
    public async Task<ApiResult<TodoItemDto>> CreateAsync(
        [FromBody] CreateTodoItemRequest request,
        CancellationToken ct)
        => ApiResult<TodoItemDto>.Ok(await todoItems.CreateAsync(CurrentUserId, request, ct));

    [HttpPut("{id:int}")]
    public async Task<ApiResult<TodoItemDto>> UpdateAsync(
        int id,
        [FromBody] UpdateTodoItemRequest request,
        CancellationToken ct)
        => ApiResult<TodoItemDto>.Ok(await todoItems.UpdateAsync(CurrentUserId, id, request, ct));

    /// <summary>
    /// 改状态。单独一个端点，是因为「勾选完成」是最高频的动作，
    /// 为了它回传整个待办对象（还要先把最新数据拉下来）不合理。
    /// </summary>
    [HttpPost("{id:int}/status")]
    public async Task<ApiResult<TodoItemDto>> ChangeStatusAsync(
        int id,
        [FromBody] ChangeTodoStatusRequest request,
        CancellationToken ct)
        => ApiResult<TodoItemDto>.Ok(
            await todoItems.ChangeStatusAsync(CurrentUserId, id, request.Status, ct));

    /// <summary>挂标签，幂等。</summary>
    [HttpPost("{id:int}/tags")]
    public async Task<ApiResult<TodoItemDto>> AttachTagsAsync(
        int id,
        [FromBody] AttachTagsRequest request,
        CancellationToken ct)
        => ApiResult<TodoItemDto>.Ok(
            await todoItems.AttachTagsAsync(CurrentUserId, id, request.TagIds, ct));

    /// <summary>摘掉一个标签，幂等。</summary>
    [HttpDelete("{id:int}/tags/{tagId:int}")]
    public async Task<ApiResult<TodoItemDto>> DetachTagAsync(
        int id,
        int tagId,
        CancellationToken ct)
        => ApiResult<TodoItemDto>.Ok(
            await todoItems.DetachTagAsync(CurrentUserId, id, tagId, ct));

    /// <summary>删除待办。子任务会一起删除。</summary>
    [HttpDelete("{id:int}")]
    public async Task<ApiResult> DeleteAsync(int id, CancellationToken ct)
    {
        await todoItems.DeleteAsync(CurrentUserId, id, ct);

        return ApiResult.Ok();
    }
}
