import type {
  CreateTodoItemInput,
  TodoItem,
  TodoItemDetail,
  TodoItemQuery,
  TodoStatus,
  UpdateTodoItemInput,
} from '@/types'
import { http } from '@/utils'

/**
 * 待办接口。筛选参数逐项可选 —— 只把用到的拼进去，不发送 `listId=`
 * 这样的空值：后端的可空 int 参数收到空串会绑定失败。
 */
export function listTodoItems(query: TodoItemQuery = {}): Promise<TodoItem[]> {
  const params = new URLSearchParams()

  if (query.listId != null) params.set('listId', String(query.listId))
  if (query.unassigned) params.set('unassigned', 'true')
  if (query.parentId != null) params.set('parentId', String(query.parentId))
  if (query.tagId != null) params.set('tagId', String(query.tagId))
  if (query.priority) params.set('priority', query.priority)
  if (query.dueBefore) params.set('dueBefore', query.dueBefore)
  if (query.keyword) params.set('keyword', query.keyword)

  // statuses 是集合，用重复参数表达：?statuses=TODO&statuses=IN_PROGRESS
  for (const status of query.statuses ?? []) {
    params.append('statuses', status)
  }

  const search = params.toString()

  return http.get<TodoItem[]>(`/api/v1/todo-items${search ? `?${search}` : ''}`)
}

/** 取单条待办详情，含子任务。 */
export function getTodoItem(id: number): Promise<TodoItemDetail> {
  return http.get<TodoItemDetail>(`/api/v1/todo-items/${id}`)
}

export function createTodoItem(input: CreateTodoItemInput): Promise<TodoItem> {
  return http.post<TodoItem>('/api/v1/todo-items', input)
}

export function updateTodoItem(id: number, input: UpdateTodoItemInput): Promise<TodoItem> {
  return http.put<TodoItem>(`/api/v1/todo-items/${id}`, input)
}

/**
 * 改状态。单独一个端点，不用回传整个对象 ——「勾选完成」是最高频动作，
 * 为它先拉一次完整数据再整体提交不划算。后端会同步维护 completedAt。
 */
export function changeTodoStatus(id: number, status: TodoStatus): Promise<TodoItem> {
  return http.post<TodoItem>(`/api/v1/todo-items/${id}/status`, { status })
}

/** 删除待办。子任务会一起删除。 */
export function deleteTodoItem(id: number): Promise<void> {
  return http.delete<void>(`/api/v1/todo-items/${id}`)
}

/** 给待办挂标签，幂等（已挂上的忽略）。 */
export function attachTodoTags(itemId: number, tagIds: number[]): Promise<TodoItem> {
  return http.post<TodoItem>(`/api/v1/todo-items/${itemId}/tags`, { tagIds })
}

/** 摘掉待办上的一个标签，幂等。 */
export function detachTodoTag(itemId: number, tagId: number): Promise<TodoItem> {
  return http.delete<TodoItem>(`/api/v1/todo-items/${itemId}/tags/${tagId}`)
}
