import { useAuth } from '@/stores'
import { useAsyncAction } from './use-async-action'

/**
 * 把 Google 账号绑定到当前登录用户。
 * 和登录的区别：登录是换身份，绑定是给当前身份加一种登录方式，走的是另一个接口。
 */
export function useBindGoogle() {
  const { bindGoogle } = useAuth()
  const { pending, error, setError, run } = useAsyncAction('绑定 Google 失败')

  async function bind(idToken: string): Promise<void> {
    await run(() => bindGoogle(idToken))
  }

  // setError 要暴露出去：GoogleLoginButton 还会报「按钮没渲染出来」这类
  // 前端问题（比如来源没在 Google 控制台登记），那些错误不经过 run()。
  return { pending, error, setError, bind }
}
