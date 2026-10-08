import { useState } from 'react'
import { resendVerification } from '@/apis'
import { useAuth } from '@/stores'
import { ApiError } from '@/utils'

/**
 * 邮箱未验证时的提示条。
 *
 * 重发接口对「邮箱不存在」也返回成功（防账号探测），所以这里不区分账号状态，
 * 发出去就展示「已发送」。
 */
export default function EmailVerificationBanner() {
  const { profile } = useAuth()
  const [pending, setPending] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!profile || profile.emailVerified) {
    return null
  }

  const email = profile.email

  async function handleResend(): Promise<void> {
    setPending(true)
    setError(null)

    try {
      await resendVerification(email)
      setSent(true)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : '发送失败，请稍后重试')
    } finally {
      setPending(false)
    }
  }

  return (
    <div className="border-b border-amber-200 bg-amber-50">
      <div className="mx-auto flex max-w-4xl flex-wrap items-center gap-x-3 gap-y-1 px-6 py-2.5 text-sm">
        <span className="text-amber-900">{email} 尚未验证。</span>

        {sent ? (
          <span className="text-amber-700">验证邮件已发送，请查收。</span>
        ) : (
          <button
            type="button"
            onClick={handleResend}
            disabled={pending}
            className="text-amber-900 underline underline-offset-2 disabled:opacity-50"
          >
            {pending ? '发送中…' : '重新发送验证邮件'}
          </button>
        )}

        {error && <span className="text-red-600">{error}</span>}
      </div>
    </div>
  )
}
