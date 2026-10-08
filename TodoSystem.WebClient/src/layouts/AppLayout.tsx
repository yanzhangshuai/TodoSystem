import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { EmailVerificationBanner } from '@/components'
import { useAuth } from '@/stores'

/**
 * 登录之后的外壳：顶栏（账号信息 + 退出）+ 内容区。
 *
 * 「退出登录」放在这里而不是页面里 —— 每个登录后的页面都需要它，
 * 而且退出后要跳转，逻辑只该有一份。
 */
export default function AppLayout({ children }: { children: ReactNode }) {
  const { profile, signOut } = useAuth()
  const navigate = useNavigate()

  async function handleSignOut(): Promise<void> {
    await signOut()
    navigate('/login', { replace: true })
  }

  return (
    <div className="min-h-full">
      <header className="border-b border-gray-200 bg-white">
        <div className="mx-auto flex max-w-4xl items-center justify-between px-6 py-4">
          <div className="flex items-center gap-3">
            {profile?.avatarUrl ? (
              <img
                src={profile.avatarUrl}
                alt=""
                className="h-9 w-9 rounded-full border border-gray-200 object-cover"
              />
            ) : (
              <div className="flex h-9 w-9 items-center justify-center rounded-full bg-gray-900 text-sm text-white">
                {profile?.userName.slice(0, 1).toUpperCase()}
              </div>
            )}

            <div className="leading-tight">
              <div className="text-sm font-medium text-gray-900">{profile?.userName}</div>
              <div className="text-xs text-gray-500">
                {profile?.email}
                <span className="ml-2 rounded bg-gray-100 px-1.5 py-0.5 text-[11px] text-gray-600">
                  {profile?.role}
                </span>
                {profile && !profile.emailVerified && (
                  <span className="ml-2 rounded bg-amber-50 px-1.5 py-0.5 text-[11px] text-amber-700">
                    邮箱未验证
                  </span>
                )}
              </div>
            </div>
          </div>

          <button
            type="button"
            onClick={handleSignOut}
            className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm text-gray-700 transition hover:bg-gray-50"
          >
            退出登录
          </button>
        </div>
      </header>

      <EmailVerificationBanner />

      <main className="mx-auto max-w-4xl px-6 py-8">{children}</main>
    </div>
  )
}
