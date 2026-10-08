import { useEffect, useState } from 'react'
import { verifyEmail } from '@/apis'
import { ApiError } from '@/utils'

export type VerifyEmailState = 'pending' | 'success' | 'failed'

/**
 * 验证页逻辑：进入就用 URL 里的 token 换结果。
 *
 * 没有额外做「只请求一次」的保护 —— StrictMode 下 effect 本来就会跑两遍，
 * 而后端把同一个链接的重复验证当作成功，所以多发一次是安全的。
 */
export function useVerifyEmail(token: string | null) {
  const [state, setState] = useState<VerifyEmailState>('pending')
  const [email, setEmail] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!token) {
      setState('failed')
      setError('链接不完整，缺少验证令牌')
      return
    }

    let cancelled = false

    void (async () => {
      try {
        const result = await verifyEmail(token)

        if (!cancelled) {
          setEmail(result.email)
          setState('success')
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof ApiError ? e.message : '验证失败，请稍后重试')
          setState('failed')
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [token])

  return { state, email, error }
}
