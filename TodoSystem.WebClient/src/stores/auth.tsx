import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import {
  fetchAuthConfig,
  fetchProfile,
  linkGoogle,
  login,
  loginWithGoogle,
  logout,
  register,
} from '@/apis'
import type { AuthConfig, UserProfile } from '@/types'
import { disableGoogleAutoSelect, tokenStore } from '@/utils'

/**
 * 全局登录态。跨页面共享的状态和动作放这里，页面组件只读它、不自己管。
 * Provider 和 useAuth 放同一个文件 —— 它们是同一件事的两面。
 */

export interface AuthStoreValue {
  profile: UserProfile | null
  /** 后端下发的公开配置，含 Google ClientId；加载失败时为 null */
  config: AuthConfig | null
  /** 首次进入时正在恢复会话 */
  initializing: boolean
  signIn: (userName: string, password: string) => Promise<void>
  signUp: (userName: string, email: string, password: string) => Promise<void>
  signInWithGoogle: (idToken: string) => Promise<void>
  /** 把 Google 账号绑定到当前登录用户 */
  bindGoogle: (idToken: string) => Promise<void>
  signOut: () => Promise<void>
}

const AuthContext = createContext<AuthStoreValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [config, setConfig] = useState<AuthConfig | null>(null)
  const [initializing, setInitializing] = useState(true)

  const loadProfile = useCallback(async () => {
    if (!tokenStore.getAccess() && !tokenStore.getRefresh()) {
      setProfile(null)
      return
    }

    try {
      setProfile(await fetchProfile())
    } catch {
      // 令牌失效或后端不可用，一律当作未登录
      tokenStore.clear()
      setProfile(null)
    }
  }, [])

  useEffect(() => {
    let cancelled = false

    void (async () => {
      // 公开配置失败不应该阻塞登录页渲染
      try {
        const cfg = await fetchAuthConfig()
        if (!cancelled) {
          setConfig(cfg)
        }
      } catch {
        if (!cancelled) {
          setConfig(null)
        }
      }

      await loadProfile()

      if (!cancelled) {
        setInitializing(false)
      }
    })()

    return () => {
      cancelled = true
    }
  }, [loadProfile])

  const value = useMemo<AuthStoreValue>(
    () => ({
      profile,
      config,
      initializing,

      async signIn(userName: string, password: string) {
        await login(userName, password)
        await loadProfile()
      },

      async signUp(userName: string, email: string, password: string) {
        await register(userName, email, password)
        await loadProfile()
      },

      async signInWithGoogle(idToken: string) {
        await loginWithGoogle(idToken)
        await loadProfile()
      },

      async bindGoogle(idToken: string) {
        setProfile(await linkGoogle(idToken))
      },

      async signOut() {
        try {
          // 注销接口失败不拦住退出 —— 令牌可能已过期，后端返 401 是正常情况
          await logout(tokenStore.getRefresh())
        } catch {
          // 忽略：退出是尽力而为，本地状态以清干净为准
        }

        tokenStore.clear()

        // 必须关掉自动登录，否则刚登出 One Tap 立刻把用户静默登回来，
        // 表现为「点了退出但页面没变」。Google 会记进 g_state cookie，一天内不再自动登录。
        disableGoogleAutoSelect()

        setProfile(null)
      },
    }),
    [profile, config, initializing, loadProfile],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

/** 读取全局登录态。必须在 <AuthProvider> 内部使用。 */
export function useAuth(): AuthStoreValue {
  const ctx = useContext(AuthContext)

  if (!ctx) {
    throw new Error('useAuth 必须在 <AuthProvider> 内部使用')
  }

  return ctx
}
