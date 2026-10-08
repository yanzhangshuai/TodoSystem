import { useEffect, useRef, useState } from 'react'
import { ensureGoogleIdentity, setGoogleCredentialHandlers } from '@/utils'

export type GoogleButtonStatus = 'loading' | 'ready' | 'unconfigured' | 'error'

interface Options {
  clientId: string | null
  /** 按钮文案：登录用 continue_with，绑定用 signin_with */
  text: 'continue_with' | 'signin_with' | 'signup_with'
  /** 按钮宽度（px） */
  width: number
  onCredential: (idToken: string) => void
  onError?: (message: string) => void
}

/**
 * 渲染官方 Google 按钮的全部副作用，组件那边只剩 JSX。
 * 初始化和凭证回调都走 utils/google-identity 的共享入口 ——
 * GIS 的 initialize() 全局只有一份 config，各组件自己调会互相覆盖。
 *
 * 调用方拿到的 containerRef 所指的 div 必须始终挂载，且在父节点里始终是第一个子节点。
 * clientId 是异步取回的，status 会随之变化；容器一旦参与条件渲染，React 会把它
 * 连同里面已渲染好的 GIS iframe 一起摘掉，containerRef.current 变成 null，
 * renderButton 被静默跳过 —— 表现是登录页「或」下面一片空白且没有任何报错。
 * 所以 loading / unconfigured 的提示一律绝对定位浮在容器之上，绝不替换容器。
 */
export function useGoogleButton({ clientId, text, width, onCredential, onError }: Options) {
  const containerRef = useRef<HTMLDivElement>(null)
  const [status, setStatus] = useState<GoogleButtonStatus>('loading')

  // 用 ref 保存回调，避免父组件每次重渲染都重新 initialize
  const credentialRef = useRef(onCredential)
  const errorRef = useRef(onError)
  credentialRef.current = onCredential
  errorRef.current = onError

  useEffect(() => {
    if (!clientId) {
      setStatus('unconfigured')

      // 配置被撤回时清掉上一次渲染残留的按钮
      if (containerRef.current) {
        containerRef.current.innerHTML = ''
      }

      return
    }

    let cancelled = false

    // 容器一直挂着，但此刻提示层可能还盖在上面（status 仍是 unconfigured）。
    // 先切回 loading 把容器露出来，再去渲染。
    setStatus('loading')

    // 注册为 primary：用户明确点了这个按钮，优先级高于 One Tap 的 fallback
    setGoogleCredentialHandlers({
      onCredential: (idToken) => credentialRef.current(idToken),
      onError: (message) => errorRef.current?.(message),
    })

    void (async () => {
      try {
        const gsi = await ensureGoogleIdentity(clientId)

        if (cancelled) {
          return
        }

        const container = containerRef.current

        // 容器始终挂载，正常不该取不到。真取不到就明确报错，别静默跳过然后谎报 ready。
        if (!container) {
          setStatus('error')
          errorRef.current?.('Google 按钮容器未就绪')
          return
        }

        container.innerHTML = ''

        gsi.renderButton(container, {
          type: 'standard',
          theme: 'outline',
          size: 'large',
          text,
          shape: 'rectangular',
          logo_alignment: 'left',
          width,
        })

        // renderButton 在当前来源未登记为「已获授权的 JavaScript 来源」时会静默失败：
        // 不抛错、不回调、容器留空。等一拍再查一次，把这个静默失败变成看得见的错误。
        await new Promise<void>((resolve) => {
          setTimeout(resolve, 0)
        })

        if (cancelled) {
          return
        }

        if (container.childElementCount === 0) {
          setStatus('error')
          errorRef.current?.(
            `Google 按钮未能渲染：当前来源 ${window.location.origin} 可能未登记为「已获授权的 JavaScript 来源」`,
          )
          return
        }

        setStatus('ready')
      } catch (error) {
        if (cancelled) {
          return
        }

        setStatus('error')
        errorRef.current?.(error instanceof Error ? error.message : 'Google 登录初始化失败')
      }
    })()

    return () => {
      cancelled = true
      setGoogleCredentialHandlers(null, 'primary')
    }
  }, [clientId, text, width])

  return { containerRef, status }
}
