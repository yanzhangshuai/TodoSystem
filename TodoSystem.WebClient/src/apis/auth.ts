import type { LoginResult } from '@/types'
import { http, tokenStore } from '@/utils'

/**
 * 认证接口，走 v2（JWT 凭证）。v1 是给同站浏览器页面的 Cookie 方案，本前端不用。
 * 这里只负责发请求和存凭证，React 状态归 stores/auth。
 */

/** 邮箱密码登录。 */
export async function login(userName: string, password: string): Promise<LoginResult> {
  const result = await http.post<LoginResult>('/api/v2/auth/login', { userName, password })
  tokenStore.saveFrom(result)
  return result
}

/** 本地账号注册；后端注册成功后直接签发令牌。 */
export async function register(
  userName: string,
  email: string,
  password: string,
): Promise<LoginResult> {
  const result = await http.post<LoginResult>('/api/v2/auth/register', {
    userName,
    email,
    password,
  })
  tokenStore.saveFrom(result)
  return result
}

/** Google 登录，首次登录即注册。 */
export async function loginWithGoogle(idToken: string): Promise<LoginResult> {
  const result = await http.post<LoginResult>('/api/v2/auth/google', { idToken })
  tokenStore.saveFrom(result)
  return result
}

/**
 * 注销 Refresh Token。本地凭证的清理由 stores/auth 的 signOut 统一做 ——
 * 「接口失败也必须清本地」这条规则只需要写一处。
 */
export function logout(refreshToken: string | null): Promise<void> {
  return http.post<void>('/api/v2/auth/logout', { refreshToken })
}
