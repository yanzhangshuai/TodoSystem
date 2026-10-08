import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '@/stores'
import { useAsyncAction } from './use-async-action'

/** 登录表单的字段。 */
export interface LoginForm {
  userName: string
  password: string
}

/** 登录页的全部逻辑：表单状态、提交、Google 回调、登录后跳转。 */
export function useLogin() {
  const { signIn, signInWithGoogle } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { pending, error, setError, run } = useAsyncAction('登录失败，请稍后重试')

  const [form, setForm] = useState<LoginForm>({ userName: '', password: '' })

  /** 登录前想去哪就回哪。ProtectedRoute 会把原路径放进 location.state。 */
  const redirectTo = (location.state as { from?: string } | null)?.from ?? '/'

  function update(field: keyof LoginForm, value: string): void {
    setForm((previous) => {
      const next = { ...previous }
      next[field] = value
      return next
    })
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()

    if (await run(() => signIn(form.userName.trim(), form.password))) {
      navigate(redirectTo, { replace: true })
    }
  }

  async function handleGoogleCredential(idToken: string): Promise<void> {
    if (await run(() => signInWithGoogle(idToken), 'Google 登录失败')) {
      navigate(redirectTo, { replace: true })
    }
  }

  return { form, update, error, setError, pending, handleSubmit, handleGoogleCredential }
}
