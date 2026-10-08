import { useEffect, useRef } from 'react'
import {
  cancelOneTapPrompt,
  ensureGoogleIdentity,
  promptOneTap,
  setGoogleCredentialHandlers,
} from '@/utils'

interface Options {
  clientId: string | null
  /** 已登录时传 false —— 不弹提示，也不注册处理器 */
  enabled: boolean
  onCredential: (idToken: string) => void
  onError?: (message: string) => void
}

/**
 * Google One Tap 自动登录。在路由层调用，未登录时全站生效。
 * 不产出 DOM —— 提示框由 GIS 画在页面右上角。
 *
 * 能否真正弹出取决于 Google 侧的条件，代码控制不了：浏览器里要有 Google 会话、
 * 当前来源要登记为「已获授权的 JavaScript 来源」、用户没手动关过提示框
 * （关过之后冷却 2 小时 → 1 天 → 1 周 → 4 周）。ITP 浏览器（Safari / Firefox /
 * iOS 上的 Chrome）不支持静默自动登录，两次静默登录之间还有 10 分钟间隔。
 */
export function useGoogleOneTap({ clientId, enabled, onCredential, onError }: Options): void {
  const credentialRef = useRef(onCredential)
  const errorRef = useRef(onError)
  credentialRef.current = onCredential
  errorRef.current = onError

  useEffect(() => {
    if (!enabled || !clientId) {
      return
    }

    let cancelled = false

    // 注册为 fallback：One Tap 是自动弹出的，意图比按钮弱。
    // 页面上有按钮时，凭证优先交给按钮的处理器。
    setGoogleCredentialHandlers(
      {
        onCredential: (idToken) => credentialRef.current(idToken),
        onError: (message) => errorRef.current?.(message),
      },
      'fallback',
    )

    void (async () => {
      try {
        await ensureGoogleIdentity(clientId)

        if (cancelled) {
          return
        }

        promptOneTap()
      } catch (error) {
        if (cancelled) {
          return
        }

        // One Tap 是加分项，失败不打扰用户 —— 登录按钮仍然可用，所以只记日志。
        console.debug('[OneTap] 初始化失败：', error instanceof Error ? error.message : error)
      }
    })()

    return () => {
      cancelled = true
      cancelOneTapPrompt()
      setGoogleCredentialHandlers(null, 'fallback')
    }
  }, [enabled, clientId])
}
