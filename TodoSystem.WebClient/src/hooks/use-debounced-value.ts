import { useEffect, useState } from 'react'

/**
 * 防抖：返回稳定下来之后的值。
 *
 * 搜索框必须用它。把每次按键直接写进请求条件，输入四个字会连发四次请求，
 * 而且响应顺序不保证 —— 先发的慢请求后到，列表就显示成上一次关键字的结果。
 */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs)

    return () => clearTimeout(timer)
  }, [value, delayMs])

  return debounced
}
