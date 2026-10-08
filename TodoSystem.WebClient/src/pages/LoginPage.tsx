import { Link } from 'react-router-dom'
import { ErrorBanner, FormField, GoogleLoginButton, OrDivider, SubmitButton } from '@/components'
import { useLogin } from '@/hooks'
import { AuthLayout } from '@/layouts'
import { useAuth } from '@/stores'

/**
 * 登录页。逻辑全在 useLogin() 里，本组件只负责画。
 * clientId 拿不到时 GoogleLoginButton 会自己显示「未配置」。
 */
export default function LoginPage() {
  const { config } = useAuth()
  const { form, update, error, setError, pending, handleSubmit, handleGoogleCredential } =
    useLogin()

  return (
    <AuthLayout
      title="TodoSystem"
      subtitle="登录你的账号"
      footer={
        <p className="mt-6 text-center text-xs leading-relaxed text-gray-400">
          联调种子账号：ADMIN / USER，密码 123
        </p>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <FormField
          id="userName"
          label="用户名或邮箱"
          value={form.userName}
          onChange={(value) => update('userName', value)}
          autoComplete="username"
          placeholder="ADMIN"
        />

        <FormField
          id="password"
          label="密码"
          type="password"
          value={form.password}
          onChange={(value) => update('password', value)}
          autoComplete="current-password"
          placeholder="••••••"
        />

        <ErrorBanner message={error} />

        <SubmitButton pending={pending} pendingText="登录中…">
          登录
        </SubmitButton>
      </form>

      <OrDivider />

      <GoogleLoginButton
        clientId={config?.googleClientId ?? null}
        disabled={pending}
        onCredential={handleGoogleCredential}
        onError={setError}
      />

      <p className="mt-6 text-center text-sm text-gray-500">
        还没有账号？
        <Link to="/register" className="ml-1 text-gray-900 underline underline-offset-2">
          注册
        </Link>
      </p>
    </AuthLayout>
  )
}
