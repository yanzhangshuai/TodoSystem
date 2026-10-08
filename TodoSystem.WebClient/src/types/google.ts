/**
 * Google Identity Services 在 window 上暴露的类型。
 *
 * 只声明本项目用到的成员，不引入 Google 官方类型包 —— 官方包体积大、更新频繁，
 * 而我们只用 initialize / renderButton / prompt / cancel / disableAutoSelect。
 */

/** initialize 的 callback 收到的对象。credential 就是 id_token（JWT）。 */
export interface GoogleCredentialResponse {
  credential?: string
  /** 用户通过哪种方式选中的账号，调试用 */
  select_by?: string
}

export interface GoogleIdConfiguration {
  client_id: string
  callback: (response: GoogleCredentialResponse) => void
  /** 静默自动登录：不弹 UI，条件满足时直接返回 credential */
  auto_select?: boolean
  /** 点击提示框外部是否关闭它 */
  cancel_on_tap_outside?: boolean
  /** 按钮走 FedCM 流（Chrome 桌面 M125+ / Android M128+） */
  use_fedcm_for_button?: boolean
  /** FedCM 按钮流下的自动登录 */
  button_auto_select?: boolean
}

/**
 * One Tap 提示框的状态通知。
 *
 * 迁移到 FedCM 后 isDisplayMoment / isDisplayed / isNotDisplayed /
 * getNotDisplayedReason 已废弃、不再返回数据，所以这里不声明。
 * 本项目不使用这个对象，详见 utils/google-identity.ts 的 promptOneTap()。
 */
export interface PromptMomentNotification {
  isSkippedMoment?: () => boolean
  getSkippedReason?: () => string
  isDismissedMoment?: () => boolean
  getDismissedReason?: () => string
}

export interface GoogleButtonOptions {
  type?: 'standard' | 'icon'
  theme?: 'outline' | 'filled_blue' | 'filled_black'
  size?: 'large' | 'medium' | 'small'
  text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin'
  shape?: 'rectangular' | 'pill' | 'circle' | 'square'
  logo_alignment?: 'left' | 'center'
  width?: number
}

export interface GoogleIdentityServices {
  initialize: (config: GoogleIdConfiguration) => void
  renderButton: (parent: HTMLElement, options: GoogleButtonOptions) => void
  prompt: (callback?: (notification: PromptMomentNotification) => void) => void
  cancel: () => void
  disableAutoSelect: () => void
}

/** 凭证处理器的统一形态，按钮和 One Tap 都用它注册。 */
export interface GoogleCredentialHandlers {
  onCredential: (idToken: string) => void
  onError?: (message: string) => void
}

declare global {
  interface Window {
    google?: {
      accounts?: {
        id?: GoogleIdentityServices
      }
    }
  }
}
