interface Props {
  /** 提交中。按钮会禁用并换成 pendingText */
  pending: boolean
  /** 空闲时的文案 */
  children: string
  /** 提交中的文案 */
  pendingText: string
}

/** 表单主按钮。提交中自动禁用并换文案。 */
export default function SubmitButton({ pending, children, pendingText }: Props) {
  return (
    <button
      type="submit"
      disabled={pending}
      className="w-full rounded-lg bg-gray-900 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-60"
    >
      {pending ? pendingText : children}
    </button>
  )
}
