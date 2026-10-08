interface Props {
  /** 为 null / 空字符串时不渲染任何东西 —— 调用方不用写 `{error && ...}` */
  message: string | null | undefined
  /** 更小的字号，用于卡片内的次要提示 */
  compact?: boolean
  className?: string
}

/** 统一的错误提示条。全站只有这一处定义红色提示长什么样。 */
export default function ErrorBanner({ message, compact, className }: Props) {
  if (!message) {
    return null
  }

  const classes = [
    'rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-red-600',
    compact ? 'text-xs leading-relaxed' : 'text-sm',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return <div className={classes}>{message}</div>
}
