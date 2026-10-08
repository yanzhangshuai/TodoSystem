import AppRouter from '@/router'
import { AuthProvider } from '@/stores'

/** 组合根：只套 Provider，不放业务逻辑。 */
export default function App() {
  return (
    <AuthProvider>
      <AppRouter />
    </AuthProvider>
  )
}
