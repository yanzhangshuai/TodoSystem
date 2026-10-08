import { Link, useSearchParams } from 'react-router-dom'
import { ErrorBanner } from '@/components'
import { useVerifyEmail } from '@/hooks'
import { AuthLayout } from '@/layouts'

/**
 * 邮箱验证落地页。用户是从邮件里点进来的，所以这个页面必须匿名可访问 ——
 * 套上 ProtectedRoute 的话，未登录用户会被弹回登录页，永远验证不了。
 */
export default function VerifyEmailPage() {
  const [params] = useSearchParams()
  const { state, email, error } = useVerifyEmail(params.get('token'))

  const subtitle =
    state === 'pending' ? '正在验证你的邮箱' : state === 'success' ? '验证完成' : '验证未通过'

  return (
    <AuthLayout title="邮箱验证" subtitle={subtitle}>
      {state === 'pending' && <p className="text-sm text-gray-500">正在验证，请稍候…</p>}

      {state === 'success' && (
        <div className="space-y-4">
          <div className="rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-700">
            {email} 已验证
          </div>

          <p className="text-sm leading-relaxed text-gray-500">
            这个邮箱现在可以用来登录，也可以直接用 Google 登录 —— 会自动合并到本账号。
          </p>

          <Link
            to="/"
            className="block rounded-lg bg-gray-900 px-4 py-2.5 text-center text-sm text-white transition hover:bg-gray-800"
          >
            进入 TodoSystem
          </Link>
        </div>
      )}

      {state === 'failed' && (
        <div className="space-y-4">
          <ErrorBanner message={error} />

          <p className="text-sm leading-relaxed text-gray-500">
            登录后可以在页面顶部重新发送验证邮件。
          </p>

          <Link
            to="/login"
            className="block rounded-lg border border-gray-300 px-4 py-2.5 text-center text-sm text-gray-700 transition hover:bg-gray-50"
          >
            去登录
          </Link>
        </div>
      )}
    </AuthLayout>
  )
}
