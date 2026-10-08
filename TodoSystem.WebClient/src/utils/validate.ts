/**
 * 表单校验规则。集中放这里是因为登录页和注册页的密码规则必须一致，
 * 两处各写一遍迟早漂移。后端的规则才是最终裁判，这里只是为了少发一次注定失败的请求。
 */

/** 刻意用宽松版本：浏览器 type="email" 能通过的，这里也应该通过。 */
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/** 与后端 RegisterRequest 的校验保持一致。 */
export const PASSWORD_MIN_LENGTH = 6

export function checkEmail(email: string): boolean {
  return EMAIL_PATTERN.test(email.trim())
}

export function checkPassword(password: string): boolean {
  return password.length >= PASSWORD_MIN_LENGTH
}
