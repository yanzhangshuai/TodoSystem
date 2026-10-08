import { useCallback, useState } from 'react'
import { ApiError } from '@/utils'

/**
 * 提交异步动作的通用外壳，只做两件事：管 pending、把异常翻译成错误文案。
 *
 * 不负责导航 —— 要不要跳转是业务自己的事，所以 run() 返回 boolean。
 *
 * 全项目统一约定：读用 loading，动作用 pending。动作 Hook 一律返回这个
 * 通用名，需要区分的调用方在解构处改名（`const { pending: listBusy } = useTodoLists()`）。
 */
export function useAsyncAction(defaultErrorMessage: string) {
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const run = useCallback(
    async (action: () => Promise<void>, errorMessage = defaultErrorMessage): Promise<boolean> => {
      setError(null)
      setPending(true)

      try {
        await action()
        return true
      } catch (e) {
        // ApiError 的 message 是后端下发的业务原因，可直接展示；
        // 其它异常（网络断了、代码 bug）才用兜底文案。
        setError(e instanceof ApiError ? e.message : errorMessage)
        return false
      } finally {
        setPending(false)
      }
    },
    [defaultErrorMessage],
  )

  return { pending, error, setError, run }
}
