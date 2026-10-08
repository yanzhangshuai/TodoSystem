interface Props {
  label?: string
}

/** 「—— 或 ——」分隔线。用于把 Google 按钮和邮箱表单分开。 */
export default function OrDivider({ label = '或' }: Props) {
  return (
    <div className="my-5 flex items-center gap-3">
      <span className="h-px flex-1 bg-gray-200" />
      <span className="text-xs text-gray-400">{label}</span>
      <span className="h-px flex-1 bg-gray-200" />
    </div>
  )
}
