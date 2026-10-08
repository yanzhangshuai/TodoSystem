import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/stores'

/**
 * 路由守卫：未登录时重定向到登录页。
 * 原路径记在 state 里，登录后可以跳回去。
 */
export default function ProtectedRoute() {
  const { profile, initializing } = useAuth()
  const location = useLocation()

  if (initializing) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-gray-500">正在加载…</div>
    )
  }

  if (!profile) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}
