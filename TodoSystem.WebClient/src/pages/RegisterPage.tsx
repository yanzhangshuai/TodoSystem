import { Link } from 'react-router-dom'
import { ErrorBanner, FormField, GoogleLoginButton, OrDivider, SubmitButton } from '@/components'
import { useRegister } from '@/hooks'
import { AuthLayout } from '@/layouts'
import { useAuth } from '@/stores'

/**
 * 注册页。Google 按钮放最前面 —— 首次登录即注册，比填四个字段快。
 * 逻辑全在 useRegister() 里。
 */
export default function RegisterPage() {
  const { config } = useAuth()
  const { form, update, error, setError, pending, handleSubmit, handleGoogleCredential } =
    useRegister()

  return (
    <AuthLayout title="创建账号" subtitle="用邮箱注册，或用 Google 一键注册">
      <GoogleLoginButton
        clientId={config?.googleClientId ?? null}
        disabled={pending}
        onCredential={handleGoogleCredential}
        onError={setError}
      />

      <OrDivider label="或使用邮箱" />

      <form onSubmit={handleSubmit} className="space-y-4">
        <FormField
          id="userName"
          label="用户名"
          value={form.userName}
          onChange={(value) => update('userName', value)}
          placeholder="不超过 50 个字符"
        />

        <FormField
          id="email"
          label="邮箱"
          type="email"
          value={form.email}
          onChange={(value) => update('email', value)}
          placeholder="you@example.com"
        />

        <FormField
          id="password"
          label="密码"
          type="password"
          value={form.password}
          onChange={(value) => update('password', value)}
          autoComplete="new-password"
          placeholder="至少 6 位"
        />

        <FormField
          id="confirm"
          label="确认密码"
          type="password"
          value={form.confirm}
          onChange={(value) => update('confirm', value)}
          autoComplete="new-password"
          placeholder="再输一次"
        />

        <ErrorBanner message={error} />

        <SubmitButton pending={pending} pendingText="注册中…">
          注册
        </SubmitButton>
      </form>

      <p className="mt-6 text-center text-sm text-gray-500">
        已有账号？
        <Link to="/login" className="ml-1 text-gray-900 underline underline-offset-2">
          登录
        </Link>
      </p>
    </AuthLayout>
  )
}
