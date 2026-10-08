import type { CreateTagInput, Tag, UpdateTagInput } from '@/types'
import { http } from '@/utils'

/** 标签接口。标签是用户私有的，同名标签在不同用户之间互不影响。 */

export function listTags(): Promise<Tag[]> {
  return http.get<Tag[]>('/api/v1/tags')
}

export function createTag(input: CreateTagInput): Promise<Tag> {
  return http.post<Tag>('/api/v1/tags', input)
}

/** 重命名 / 改色。后端是 PUT 整体替换语义，所以 name 必须一起传。 */
export function updateTag(id: number, input: UpdateTagInput): Promise<Tag> {
  return http.put<Tag>(`/api/v1/tags/${id}`, input)
}

/** 删除标签。与待办的关联会一并清理，待办本身不受影响。 */
export function deleteTag(id: number): Promise<void> {
  return http.delete<void>(`/api/v1/tags/${id}`)
}
