import type { VerifyEmailResult } from '@/types'
import { http } from '@/utils'

/**
 * 邮箱验证接口，走 v1。这两个动作与凭证方案无关，所以后端只有 v1 一份。
 */

/** 消费邮件里的令牌。同一个链接重复调用会返回成功，不必自己做防重。 */
export function verifyEmail(token: string): Promise<VerifyEmailResult> {
  return http.post<VerifyEmailResult>('/api/v1/email-verification/verify', { token })
}

/**
 * 重发验证邮件。后端对「邮箱不存在」也返回成功（防账号探测），
 * 所以这里拿不到「这个邮箱没注册过」这类反馈，也不用处理。
 */
export function resendVerification(email: string): Promise<void> {
  return http.post<void>('/api/v1/email-verification/resend', { email })
}
