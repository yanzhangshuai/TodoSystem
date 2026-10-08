using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoSystem.Application.Services.Auth;
using TodoSystem.Application.Services.Todo;
using TodoSystem.Application.Services.Todo.Dtos;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Controllers.v1;

/// <summary>
/// 待办清单。
///
/// 路由写死 <c>todo-lists</c> 而不用 <c>[controller]</c>：注册的
/// <see cref="Utils.Route.KebabCaseParameterTransformer"/> 只做小写化、
/// 不插连字符，用 <c>[controller]</c> 会得到 <c>todolists</c>。
///
/// 权限边界是「归属」而不是「角色」：清单是用户私有数据，
/// 任何登录用户都只能操作自己的那一份。
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("v{Version:apiVersion}/todo-lists")]
[Authorize]
public class TodoListsController(
    ICurrentUser currentUser,
    ITodoListService todoLists) : TodoControllerBase(currentUser)
{
    /// <summary>列出当前用户的清单。默认不含已归档的。</summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<TodoListDto>>> ListAsync(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default)
        => ApiResult<IReadOnlyList<TodoListDto>>.Ok(
            await todoLists.ListAsync(CurrentUserId, includeArchived, ct));

    [HttpGet("{id:int}")]
    public async Task<ApiResult<TodoListDto>> GetAsync(int id, CancellationToken ct)
        => ApiResult<TodoListDto>.Ok(await todoLists.GetAsync(CurrentUserId, id, ct));

    [HttpPost]
    public async Task<ApiResult<TodoListDto>> CreateAsync(
        [FromBody] CreateTodoListRequest request,
        CancellationToken ct)
        => ApiResult<TodoListDto>.Ok(await todoLists.CreateAsync(CurrentUserId, request, ct));

    [HttpPut("{id:int}")]
    public async Task<ApiResult<TodoListDto>> UpdateAsync(
        int id,
        [FromBody] UpdateTodoListRequest request,
        CancellationToken ct)
        => ApiResult<TodoListDto>.Ok(await todoLists.UpdateAsync(CurrentUserId, id, request, ct));

    /// <summary>删除清单。清单里的待办不会被删掉，会退回到「未归类」。</summary>
    [HttpDelete("{id:int}")]
    public async Task<ApiResult> DeleteAsync(int id, CancellationToken ct)
    {
        await todoLists.DeleteAsync(CurrentUserId, id, ct);

        return ApiResult.Ok();
    }
}
