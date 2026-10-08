import { useCallback, useEffect, useState } from 'react'
import { createTag, deleteTag, listTags, updateTag } from '@/apis'
import type { Tag, UpdateTagInput } from '@/types'
import { ApiError } from '@/utils'
import { useAsyncAction } from './use-async-action'

/** 标签的数据逻辑：加载、新建（带颜色）、改名 / 改色、删除。 */
export function useTags() {
  const [tags, setTags] = useState<Tag[]>([])
  const [loading, setLoading] = useState(true)
  const { pending, error, setError, run } = useAsyncAction('加载标签失败')

  const reload = useCallback(async (): Promise<void> => {
    setLoading(true)
    setError(null)

    try {
      setTags(await listTags())
    } catch (e) {
      setError(e instanceof ApiError ? e.message : '加载标签失败')
    } finally {
      setLoading(false)
    }
  }, [setError])

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
    (name: string, color: string | null): Promise<boolean> =>
      mutate(() => createTag({ name, color }), '创建标签失败'),
    [mutate],
  )

  /**
   * 改名 / 改色。后端是整体替换语义，所以 name 必须一起传 ——
   * 只传 color 会把标签名清空。
   */
  const update = useCallback(
    (tag: Tag, patch: Partial<UpdateTagInput>): Promise<boolean> =>
      mutate(
        () => updateTag(tag.id, { name: tag.name, color: tag.color, ...patch }),
        '更新标签失败',
      ),
    [mutate],
  )

  /** 删除标签。与待办的关联会一并清理，待办本身不受影响。 */
  const remove = useCallback(
    (id: number): Promise<boolean> => mutate(() => deleteTag(id), '删除标签失败'),
    [mutate],
  )

  useEffect(() => {
    void reload()
  }, [reload])

  return { tags, loading, pending, error, reload, create, update, remove }
}
