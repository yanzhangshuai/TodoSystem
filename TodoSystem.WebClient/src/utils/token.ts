import type { LoginResult } from '@/types'

const ACCESS_TOKEN_KEY = 'todosystem.accessToken'
const REFRESH_TOKEN_KEY = 'todosystem.refreshToken'

/**
 * 凭证的本地存储。用 localStorage 而不是 cookie，是因为 v2 的令牌由 JS 显式
 * 放进 Authorization 头，不依赖浏览器自动携带。
 */
export const tokenStore = {
  getAccess(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY)
  },

  getRefresh(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY)
  },

  save(accessToken: string, refreshToken: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken)
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
  },

  /** 直接存后端下发的凭证对象。 */
  saveFrom(result: LoginResult): void {
    tokenStore.save(result.accessToken, result.refreshToken)
  },

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
  },
}
