import { useCallback, useEffect, useState } from 'react'
import { createTodoList, deleteTodoList, listTodoLists, updateTodoList } from '@/apis'
import type { TodoList, UpdateTodoListInput } from '@/types'
import { ApiError } from '@/utils'
import { useAsyncAction } from './use-async-action'

/**
 * 清单的数据逻辑：加载、新建、改名 / 归档、删除。
 *
 * includeArchived 是服务端参数而不是本地过滤：归档清单可能带着几十条待办，
 * 全量拉下来再 filter 白费流量；「已归档」的判定规则也只应该在后端存在一份。
 */
export function useTodoLists() {
  const [lists, setLists] = useState<TodoList[]>([])
  const [includeArchived, setIncludeArchived] = useState(false)
  const [loading, setLoading] = useState(true)
  const { pending, error, setError, run } = useAsyncAction('加载清单失败')

  /** 拉取清单。首次进入、切换归档开关、每次增删改之后都走它。 */
  const reload = useCallback(async (): Promise<void> => {
    setLoading(true)
    setError(null)

    try {
      setLists(await listTodoLists(includeArchived))
    } catch (e) {
      setError(e instanceof ApiError ? e.message : '加载清单失败')
    } finally {
      setLoading(false)
    }
  }, [includeArchived, setError])

  /** 跑一个写操作，成功就整体重载。返回值供行内表单决定要不要收起。 */
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
    (name: string): Promise<boolean> =>
      mutate(() => createTodoList({ name }), '创建清单失败'),
    [mutate],
  )

  /**
   * 局部修改（改名 / 归档 / 改色）。后端是整体替换语义，所以要把当前值铺开再盖 patch ——
   * 只传 name 会把 isArchived 重置成 false，改名顺手把清单「取消归档」了。
   */
  const update = useCallback(
    (list: TodoList, patch: Partial<UpdateTodoListInput>): Promise<boolean> =>
      mutate(
        () =>
          updateTodoList(list.id, {
            name: list.name,
            description: list.description,
            color: list.color,
            sortOrder: list.sortOrder,
            isArchived: list.isArchived,
            ...patch,
          }),
        '更新清单失败',
      ),
    [mutate],
  )

  /** 删除清单。清单里的待办不会被删除，会退回「未归类」（由后端保证）。 */
  const remove = useCallback(
    (id: number): Promise<boolean> => mutate(() => deleteTodoList(id), '删除清单失败'),
    [mutate],
  )

  useEffect(() => {
    void reload()
  }, [reload])

  return {
    lists,
    includeArchived,
    setIncludeArchived,
    loading,
    pending,
    error,
    reload,
    create,
    update,
    remove,
  }
}
