import type { AuthConfig } from '@/types'
import { http } from '@/utils'

/** 公开接口，不需要登录。与凭证方案无关，后端只有 v1。 */

/** 前端启动时拉取公开配置（Google ClientId）。 */
export function fetchAuthConfig(): Promise<AuthConfig> {
  return http.get<AuthConfig>('/api/v1/common/config')
}
