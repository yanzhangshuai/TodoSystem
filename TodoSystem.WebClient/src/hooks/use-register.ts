import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/stores'
import { PASSWORD_MIN_LENGTH, checkEmail, checkPassword } from '@/utils'
import { useAsyncAction } from './use-async-action'

/** 注册表单的字段。 */
export interface RegisterForm {
  userName: string
  email: string
  password: string
  confirm: string
}

/**
 * 注册页的全部逻辑。客户端先校验一轮，少发一次注定失败的请求；
 * 真正的裁判仍是后端，所以后端返回的业务错误照样原样展示。
 */
export function useRegister() {
  const { signUp, signInWithGoogle } = useAuth()
  const navigate = useNavigate()
  const { pending, error, setError, run } = useAsyncAction('注册失败，请稍后重试')

  const [form, setForm] = useState<RegisterForm>({
    userName: '',
    email: '',
    password: '',
    confirm: '',
  })

  function update(field: keyof RegisterForm, value: string): void {
    setForm((previous) => {
      const next = { ...previous }
      next[field] = value
      return next
    })
  }

  function validate(): boolean {
    if (!checkEmail(form.email)) {
      setError('邮箱格式不正确')
      return false
    }

    if (!checkPassword(form.password)) {
      setError(`密码至少 ${PASSWORD_MIN_LENGTH} 位`)
      return false
    }

    if (form.password !== form.confirm) {
      setError('两次输入的密码不一致')
      return false
    }

    return true
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()

    if (!validate()) {
      return
    }

    if (await run(() => signUp(form.userName.trim(), form.email.trim(), form.password))) {
      navigate('/', { replace: true })
    }
  }

  async function handleGoogleCredential(idToken: string): Promise<void> {
    // Google 首次登录即注册，无需额外填写表单
    if (await run(() => signInWithGoogle(idToken), 'Google 注册失败')) {
      navigate('/', { replace: true })
    }
  }

  return { form, update, error, setError, pending, handleSubmit, handleGoogleCredential }
}
