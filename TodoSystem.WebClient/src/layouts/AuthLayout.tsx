import type { ReactNode } from 'react'

interface Props {
  title: string
  subtitle: string
  /** 卡片外部的次要内容，如「联调种子账号…」 */
  footer?: ReactNode
  children: ReactNode
}

/** 登录 / 注册页共用的布局：居中、限宽、白卡片。页面组件就只剩表单本身。 */
export default function AuthLayout({ title, subtitle, footer, children }: Props) {
  return (
    <div className="flex min-h-full items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <h1 className="text-2xl font-medium text-gray-900">{title}</h1>
          <p className="mt-2 text-sm text-gray-500">{subtitle}</p>
        </div>

        <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">{children}</div>

        {footer}
      </div>
    </div>
  )
}
