interface Props {
  /** 同时用作 input 的 id 和 name */
  id: string
  label: string
  value: string
  onChange: (value: string) => void
  type?: 'text' | 'email' | 'password'
  placeholder?: string
  autoComplete?: string
  required?: boolean
}

/** 表单里「标签 + 输入框」的最小单元。登录页和注册页的字段样式统一在这里。 */
export default function FormField({
  id,
  label,
  value,
  onChange,
  type = 'text',
  placeholder,
  autoComplete,
  required = true,
}: Props) {
  return (
    <div>
      <label htmlFor={id} className="mb-1.5 block text-sm text-gray-700">
        {label}
      </label>

      <input
        id={id}
        name={id}
        type={type}
        autoComplete={autoComplete}
        required={required}
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none transition focus:border-gray-900 focus:ring-1 focus:ring-gray-900"
      />
    </div>
  )
}
