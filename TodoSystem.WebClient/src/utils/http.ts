import type { ApiEnvelope, LoginResult } from '@/types'
import { tokenStore } from './token'

/**
 * HTTP 层：统一请求、统一错误、统一在 401 时刷新令牌。
 * 这一层不认识业务，业务语义在 apis/ 里。
 */

/** 后端返回非 2xx，或响应体里的 code 非 0 时抛出。 */
export class ApiError extends Error {
  readonly status: number
  readonly code: number

  constructor(status: number, code: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

/**
 * 并发 401 时只发一次刷新请求，其余请求复用同一个 Promise。
 * 模块级变量而不是状态 —— 它必须跨组件、跨渲染存活。
 */
let refreshInFlight: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = tokenStore.getRefresh()

  if (!refreshToken) {
    return null
  }

  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      try {
        // 这里用裸 fetch 而不是 http.post —— 否则刷新失败会再次触发刷新，无限递归。
        // v2 = JWT 凭证方案。
        const response = await fetch('/api/v2/auth/refresh', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken }),
          credentials: 'include',
        })

        if (!response.ok) {
          return null
        }

        const body = (await response.json()) as ApiEnvelope<LoginResult>

        if (!body.data) {
          return null
        }

        tokenStore.saveFrom(body.data)
        return body.data.accessToken
      } catch {
        return null
      } finally {
        refreshInFlight = null
      }
    })()
  }

  return refreshInFlight
}

/** 容错解析响应体：空 body 或非 JSON 都返回 null，不抛错。 */
async function parseEnvelope<T>(response: Response): Promise<ApiEnvelope<T> | null> {
  const text = await response.text()

  if (!text) {
    return null
  }

  try {
    return JSON.parse(text) as ApiEnvelope<T>
  } catch {
    return null
  }
}

async function request<T>(path: string, init: RequestInit = {}, allowRetry = true): Promise<T> {
  const headers = new Headers(init.headers)

  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const accessToken = tokenStore.getAccess()

  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  const response = await fetch(path, { ...init, headers, credentials: 'include' })

  // Access Token 过期：刷新一次后重放原请求（只重放一次，避免死循环）
  if (response.status === 401 && allowRetry) {
    const renewed = await refreshAccessToken()

    if (renewed) {
      return request<T>(path, init, false)
    }

    tokenStore.clear()
    throw new ApiError(401, 401, '登录已过期，请重新登录')
  }

  const body = await parseEnvelope<T>(response)

  if (!response.ok) {
    throw new ApiError(
      response.status,
      body?.code ?? response.status,
      body?.message?.trim() || `请求失败（HTTP ${response.status}）`,
    )
  }

  // 无返回值的接口（ApiResult）data 为 null，这里按 T 透传
  return (body?.data ?? null) as T
}

export const http = {
  get<T>(path: string): Promise<T> {
    return request<T>(path)
  },

  post<T>(path: string, payload?: unknown): Promise<T> {
    return request<T>(path, {
      method: 'POST',
      body: payload === undefined ? undefined : JSON.stringify(payload),
    })
  },

  put<T>(path: string, payload?: unknown): Promise<T> {
    return request<T>(path, {
      method: 'PUT',
      body: payload === undefined ? undefined : JSON.stringify(payload),
    })
  },

  delete<T>(path: string): Promise<T> {
    return request<T>(path, { method: 'DELETE' })
  },
}
