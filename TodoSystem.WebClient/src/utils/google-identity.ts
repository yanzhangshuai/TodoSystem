import type {
  GoogleCredentialHandlers,
  GoogleCredentialResponse,
  GoogleIdConfiguration,
  GoogleIdentityServices,
} from '@/types'

/**
 * Google Identity Services 脚本加载与全局初始化。
 *
 * window.google.accounts.id 是页面级单例 —— initialize() 的 config 全页只能有一份。
 * 这个约束来自平台，所以状态只能放模块级，做不成实例字段。
 */

const SCRIPT_SRC = 'https://accounts.google.com/gsi/client'

/** 静默自动登录开关。config 全局唯一，只能在这里改，别从组件传。 */
const AUTO_SELECT = true

/** 脚本加载的 single-flight Promise。null 表示还没开始加载。 */
let loadPromise: Promise<void> | null = null

/** 已用哪个 clientId initialize 过，没变就不重复调。 */
let initializedClientId: string | null = null

/** 按钮这类明确交互入口注册的处理器。 */
let primaryHandlers: GoogleCredentialHandlers | null = null

/** One Tap 注册的兜底处理器，没有 primary 时生效。 */
let fallbackHandlers: GoogleCredentialHandlers | null = null

function loadGoogleIdentityScript(): Promise<void> {
  if (typeof window === 'undefined') {
    return Promise.reject(new Error('非浏览器环境'))
  }

  if (window.google?.accounts?.id) {
    return Promise.resolve()
  }

  if (loadPromise) {
    return loadPromise
  }

  loadPromise = new Promise<void>((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>('script[data-gsi]')

    if (existing) {
      existing.addEventListener('load', () => resolve())
      existing.addEventListener('error', () => reject(new Error('Google 登录脚本加载失败')))
      return
    }

    const script = document.createElement('script')
    script.src = SCRIPT_SRC
    script.async = true
    script.defer = true
    script.dataset.gsi = 'true'
    script.onload = () => resolve()
    script.onerror = () => reject(new Error('Google 登录脚本加载失败'))

    document.head.appendChild(script)
  })

  return loadPromise
}

function getGoogleIdentity(): GoogleIdentityServices | null {
  return window.google?.accounts?.id ?? null
}

function dispatchCredential(response: GoogleCredentialResponse): void {
  const handlers = primaryHandlers ?? fallbackHandlers

  if (!handlers) {
    return
  }

  if (response.credential) {
    handlers.onCredential(response.credential)
  } else {
    handlers.onError?.('未获取到 Google 凭证')
  }
}

/**
 * 注册凭证处理器。按钮注册 primary，One Tap 注册 fallback。
 *
 * 分发时优先 primary，否则 Dashboard 上的「绑定 Google」会被 One Tap
 * 的登录处理器抢走，出现「点了绑定结果登出了」。
 */
export function setGoogleCredentialHandlers(
  handlers: GoogleCredentialHandlers | null,
  priority: 'primary' | 'fallback' = 'primary',
): void {
  if (priority === 'primary') {
    primaryHandlers = handlers
  } else {
    fallbackHandlers = handlers
  }
}

/** 确保脚本就绪并完成 initialize。同一个 clientId 只初始化一次。 */
export async function ensureGoogleIdentity(clientId: string): Promise<GoogleIdentityServices> {
  if (!clientId) {
    throw new Error('未配置 Google ClientId')
  }

  await loadGoogleIdentityScript()

  const gsi = getGoogleIdentity()

  if (!gsi) {
    throw new Error('Google 登录脚本未就绪')
  }

  if (initializedClientId === clientId) {
    return gsi
  }

  const config: GoogleIdConfiguration = {
    client_id: clientId,
    auto_select: AUTO_SELECT,
    // 点提示框外部不关闭：登录页上用户可能正要去点按钮
    cancel_on_tap_outside: false,
    // 按钮走 FedCM（Chrome 桌面 M125+ / Android M128+），不依赖第三方 Cookie
    use_fedcm_for_button: true,
    button_auto_select: AUTO_SELECT,
    callback: dispatchCredential,
  }

  gsi.initialize(config)

  initializedClientId = clientId

  return gsi
}

/**
 * 弹出 One Tap。条件满足时 GIS 静默返回 id_token、完全不显示 UI，
 * 不满足才退化成右上角的提示框。
 *
 * 刻意不传回调：PromptMomentNotification 上的状态方法（isDisplayMoment /
 * isDisplayed / isNotDisplayed / getNotDisplayedReason / getSkippedReason）
 * 已随 FedCM 迁移废弃，读其中任何一个都会触发 GSI_LOGGER 废弃警告。
 * 凭证是从 initialize 的 callback 回来的，与 prompt 的状态通知无关。
 *
 * 另外 use_fedcm_for_prompt 开关已废弃且会被忽略 —— FedCM 现在是默认行为。
 */
export function promptOneTap(): void {
  getGoogleIdentity()?.prompt()
}

/** 关闭当前显示的 One Tap 提示框。 */
export function cancelOneTapPrompt(): void {
  getGoogleIdentity()?.cancel()
}

/**
 * 关闭自动登录。登出时必须调用，否则刚登出就会被 One Tap 静默登回来。
 * Google 会把这次退出记进 g_state cookie，一天内不再自动登录；提示框本身仍可用。
 * 只影响自动登录，不影响按钮。
 */
export function disableGoogleAutoSelect(): void {
  getGoogleIdentity()?.disableAutoSelect()
}
