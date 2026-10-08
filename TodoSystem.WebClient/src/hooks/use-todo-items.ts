import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  attachTodoTags,
  changeTodoStatus,
  createTodoItem,
  deleteTodoItem,
  detachTodoTag,
  listTodoItems,
  updateTodoItem,
} from '@/apis'
import type {
  TodoFilters,
  TodoItem,
  TodoItemQuery,
  TodoPriority,
  TodoStatus,
  UpdateTodoItemInput,
} from '@/types'
import { ApiError } from '@/utils'
import { useAsyncAction } from './use-async-action'

/**
 * 当前视图的范围，由左侧导航决定。
 * listId 为 null 表示不限清单；unassigned 表示只看没有清单的（listId IS NULL）。
 * 两者互斥，由页面保证不会同时为真。
 */
export interface TodoScope {
  listId: number | null
  unassigned: boolean
}

/**
 * 把「当前待办 + 局部改动」拼成后端要的完整 PUT 请求体。
 *
 * 后端是整体替换语义：没传的字段会被当成 null 写回去，所以任何一处局部修改
 * 都必须把其它字段一起带上。patch 里不要出现显式的 undefined —— 展开会把它
 * 盖到基础值上，序列化时该字段直接消失，等于把这一列清空了。
 */
function toUpdateInput(
  item: TodoItem,
  patch: Partial<UpdateTodoItemInput> = {},
): UpdateTodoItemInput {
  return {
    title: item.title,
    description: item.description,
    listId: item.listId,
    status: item.status,
    priority: item.priority,
    dueAt: item.dueAt,
    sortOrder: item.sortOrder,
    ...patch,
  }
}

/**
 * 待办列表的数据逻辑。
 *
 * 每次写操作之后整体 reload，不在本地拼状态：状态的真相在后端
 * （completedAt 由后端维护、isOverdue 由后端算、删父任务会牵连子任务），
 * 本地维护一份副本迟早和服务端不一致。
 */
export function useTodoItems(scope: TodoScope, filters: TodoFilters) {
  const [items, setItems] = useState<TodoItem[]>([])
  const [loading, setLoading] = useState(true)
  const { pending, error, setError, run } = useAsyncAction('加载待办失败')

  const { listId, unassigned } = scope
  const { keyword, priority, tagId } = filters

  /**
   * 查询条件 memo 在基本类型上，而不是 scope / filters 这两个对象上。
   * 页面每次渲染都会新建这两个对象，直接把对象放进依赖数组会让 query 每帧都是
   * 新引用 → reload 每帧都变 → useEffect 跟着重跑 → 请求无限循环。
   */
  const query = useMemo<TodoItemQuery>(
    () => ({
      listId,
      unassigned,
      // 空串不当成筛选条件，免得白白多一个 WHERE 子句
      keyword: keyword.trim() || null,
      priority,
      tagId,
    }),
    [listId, unassigned, keyword, priority, tagId],
  )

  const reload = useCallback(async (): Promise<void> => {
    setLoading(true)
    setError(null)

    try {
      setItems(await listTodoItems(query))
    } catch (e) {
      setError(e instanceof ApiError ? e.message : '加载待办失败')
    } finally {
      setLoading(false)
    }
  }, [query, setError])

  /** 跑一个写操作，成功就整体重载。返回值供表单决定提交完要不要收起。 */
  const mutate = useCallback(
    async (action: () => Promise<unknown>, message: string): Promise<boolean> => {
      const ok = await run(async () => {
        await action()
      }, message)

      if (ok) {
        await reload()
      }

      return ok
    },
    [run, reload],
  )

  const create = useCallback(
    (title: string, itemPriority: TodoPriority): Promise<boolean> =>
      mutate(
        () =>
          createTodoItem({
            title,
            priority: itemPriority,
            // 「未归类」视图下新建的待办本来就不该挂清单
            listId: unassigned ? null : listId,
          }),
        '创建待办失败',
      ),
    [mutate, listId, unassigned],
  )

  /**
   * 新建子任务。子任务继承父任务的清单和优先级 —— 它本来就是因父任务才存在的。
   * 「最多两层」由后端校验，前端不二次拦截，否则用户只会看到按钮点了没反应。
   */
  const createSubtask = useCallback(
    (parent: TodoItem, title: string): Promise<boolean> =>
      mutate(
        () =>
          createTodoItem({
            title,
            listId: parent.listId,
            parentId: parent.id,
            priority: parent.priority,
          }),
        '创建子任务失败',
      ),
    [mutate],
  )

  /** 勾选 / 取消勾选。只调状态端点，不动其它字段 —— 这是最高频的动作。 */
  const toggle = useCallback(
    (item: TodoItem): Promise<boolean> =>
      mutate(
        () => changeTodoStatus(item.id, item.status === 'DONE' ? 'TODO' : 'DONE'),
        '更新状态失败',
      ),
    [mutate],
  )

  /** 切到指定状态。行内的「开始 / 回到待开始」走它。 */
  const setStatus = useCallback(
    (item: TodoItem, status: TodoStatus): Promise<boolean> =>
      mutate(() => changeTodoStatus(item.id, status), '更新状态失败'),
    [mutate],
  )

  /** 整体保存（编辑表单提交）。input 由调用方拼成完整的替换请求体。 */
  const saveItem = useCallback(
    (itemId: number, input: UpdateTodoItemInput): Promise<boolean> =>
      mutate(() => updateTodoItem(itemId, input), '保存待办失败'),
    [mutate],
  )

  const setDueAt = useCallback(
    (item: TodoItem, dueAt: string | null): Promise<boolean> =>
      mutate(() => updateTodoItem(item.id, toUpdateInput(item, { dueAt })), '更新截止时间失败'),
    [mutate],
  )

  const moveToList = useCallback(
    (item: TodoItem, targetListId: number | null): Promise<boolean> =>
      mutate(
        () => updateTodoItem(item.id, toUpdateInput(item, { listId: targetListId })),
        '移动待办失败',
      ),
    [mutate],
  )

  /** 删除待办。后端会连带软删除它的子任务。 */
  const remove = useCallback(
    (id: number): Promise<boolean> => mutate(() => deleteTodoItem(id), '删除待办失败'),
    [mutate],
  )

  const attachTag = useCallback(
    (itemId: number, tagIdToAttach: number): Promise<boolean> =>
      mutate(() => attachTodoTags(itemId, [tagIdToAttach]), '挂标签失败'),
    [mutate],
  )

  const detachTag = useCallback(
    (itemId: number, tagIdToDetach: number): Promise<boolean> =>
      mutate(() => detachTodoTag(itemId, tagIdToDetach), '摘标签失败'),
    [mutate],
  )

  useEffect(() => {
    void reload()
  }, [reload])

  return {
    items,
    loading,
    pending,
    error,
    reload,
    create,
    createSubtask,
    toggle,
    setStatus,
    saveItem,
    setDueAt,
    moveToList,
    remove,
    attachTag,
    detachTag,
  }
}
