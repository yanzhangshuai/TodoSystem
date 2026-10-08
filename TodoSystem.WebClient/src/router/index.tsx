import { Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from '@/components'
import { useGoogleOneTap } from '@/hooks'
import DashboardPage from '@/pages/DashboardPage'
import LoginPage from '@/pages/LoginPage'
import RegisterPage from '@/pages/RegisterPage'
import VerifyEmailPage from '@/pages/VerifyEmailPage'
import { useAuth } from '@/stores'

/** 路由表。路由相关的判断集中在这里，页面组件不关心自己挂在哪。 */
export default function AppRouter() {
  const { profile, config, initializing, signInWithGoogle } = useAuth()

  // 未登录时全站生效。等 initializing 结束再启用 —— 会话恢复期间 profile 还是 null，
  // 提前弹提示会让已登录用户在刷新瞬间看到登录提示。
  useGoogleOneTap({
    clientId: config?.googleClientId ?? null,
    enabled: !initializing && !profile,
    onCredential: (idToken) => {
      // 自动登录失败不打扰用户，登录页的按钮仍然可用
      void signInWithGoogle(idToken).catch((error) => {
        console.warn('[OneTap] 自动登录失败：', error)
      })
    },
  })

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* 验证链接从邮件里点进来时用户多半没登录，所以这条不能放进 ProtectedRoute */}
      <Route path="/verify-email" element={<VerifyEmailPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/" element={<DashboardPage />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
