/** 登录 / 注册 / 刷新成功后后端下发的凭证。 */
export interface LoginResult {
  userId: number
  userName: string
  role: string
  accessToken: string
  refreshToken: string
  /** Access Token 剩余有效秒数 */
  expiresIn: number
  refreshTokenExpiresAt: string
}

/** 当前登录用户的资料。 */
export interface UserProfile {
  userId: number
  userName: string
  email: string
  role: string
  avatarUrl: string | null
  /** false 表示该账号没有本地密码，只能走 Google 登录 */
  hasLocalPassword: boolean
  hasGoogleBound: boolean
  /**
   * 未验证的邮箱不会被第三方登录自动合并到本账号，
   * 需要先登录再主动绑定 Google。
   */
  emailVerified: boolean
}

/** 后端下发的公开配置（无需登录即可获取）。 */
export interface AuthConfig {
  googleClientId: string
}

/** 邮箱验证成功的结果。 */
export interface VerifyEmailResult {
  email: string
}
