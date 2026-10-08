import type { UserProfile } from '@/types'
import { http } from '@/utils'

/** 账号接口：已登录之后的账号操作。与凭证方案无关，后端只有 v1。 */

export function fetchProfile(): Promise<UserProfile> {
  return http.get<UserProfile>('/api/v1/account/me')
}

/** 把 Google 账号绑定到当前登录用户。本地注册的账号想改用 Google 登录时走这里。 */
export function linkGoogle(idToken: string): Promise<UserProfile> {
  return http.post<UserProfile>('/api/v1/account/google', { idToken })
}
