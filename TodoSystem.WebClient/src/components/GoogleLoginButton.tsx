import { useGoogleButton } from '@/hooks'
import ErrorBanner from './ErrorBanner'

interface Props {
  clientId: string | null
  disabled?: boolean
  onCredential: (idToken: string) => void
  onError?: (message: string) => void
  /** 按钮文案，登录用 continue_with，绑定用 signin_with */
  text?: 'continue_with' | 'signin_with' | 'signup_with'
  /** 按钮宽度（px） */
  width?: number
}

/**
 * 渲染官方「使用 Google 账号继续」按钮。点击后 Google 回调返回 id_token，
 * 由父组件 POST 给后端换取应用自己的 JWT。
 * 所有副作用都在 useGoogleButton 里，本组件只负责摆位置。
 */
export default function GoogleLoginButton({
  clientId,
  disabled,
  onCredential,
  onError,
  text = 'continue_with',
  width = 320,
}: Props) {
  const { containerRef, status } = useGoogleButton({ clientId, text, width, onCredential, onError })

  return (
    <div className={disabled ? 'pointer-events-none opacity-60' : undefined}>
      <div className="relative">
        {/* 必须永远在，且永远是第一个子节点 —— 见 useGoogleButton 的注释 */}
        <div ref={containerRef} className="gsi-slot" />

        {status === 'loading' && (
          <div className="pointer-events-none absolute inset-0 flex items-center justify-center text-xs text-gray-400">
            正在加载 Google 登录…
          </div>
        )}

        {status === 'unconfigured' && (
          <div className="pointer-events-none absolute inset-0 flex items-center justify-center px-4 text-center text-xs text-gray-500">
            后端未配置 Google ClientId
          </div>
        )}
      </div>

      {status === 'error' && (
        <ErrorBanner
          compact
          className="mt-2 text-center"
          message="Google 登录不可用，请检查网络或 ClientId 配置"
        />
      )}
    </div>
  )
}
