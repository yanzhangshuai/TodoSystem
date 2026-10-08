import type { CreateTodoListInput, TodoList, UpdateTodoListInput } from '@/types'
import { http } from '@/utils'

/**
 * 清单接口。路由写死 todo-lists 而不是 [controller] —— 后端注册的
 * KebabCaseParameterTransformer 只做小写化、不插连字符，用 [controller] 会得到 todolists。
 * 全部需要登录，且只能操作自己的数据；userId 从 token 取，前端不传。
 */

/** 列出当前用户的清单。默认不含已归档的。 */
export function listTodoLists(includeArchived = false): Promise<TodoList[]> {
  return http.get<TodoList[]>(`/api/v1/todo-lists?includeArchived=${includeArchived}`)
}

export function createTodoList(input: CreateTodoListInput): Promise<TodoList> {
  return http.post<TodoList>('/api/v1/todo-lists', input)
}

export function updateTodoList(id: number, input: UpdateTodoListInput): Promise<TodoList> {
  return http.put<TodoList>(`/api/v1/todo-lists/${id}`, input)
}

/** 删除清单。清单里的待办不会被删除，会退回「未归类」。 */
export function deleteTodoList(id: number): Promise<void> {
  return http.delete<void>(`/api/v1/todo-lists/${id}`)
}
