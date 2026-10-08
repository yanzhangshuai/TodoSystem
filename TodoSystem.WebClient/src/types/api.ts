/** 后端统一响应包装。code === 0 表示成功。 */
export interface ApiEnvelope<T> {
  data: T | null
  statusCode: number
  code: number
  message?: string | null
}
