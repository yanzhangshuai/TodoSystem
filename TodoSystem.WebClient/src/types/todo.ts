/**
 * 待办领域的前端类型。取值必须和后端常量类一一对应：
 * TodoStatus ←→ TodoStatuses，TodoPriority ←→ TodoPriorities。
 * 用联合类型而不是 enum，是因为接口返回的就是字符串字面量，能直接参与类型收窄。
 */

/** 待办状态。 */
export type TodoStatus = 'TODO' | 'IN_PROGRESS' | 'DONE'

/** 待办优先级。 */
export type TodoPriority = 'LOW' | 'NORMAL' | 'HIGH' | 'URGENT'

export const TODO_STATUSES: readonly TodoStatus[] = ['TODO', 'IN_PROGRESS', 'DONE']

export const TODO_PRIORITIES: readonly TodoPriority[] = ['LOW', 'NORMAL', 'HIGH', 'URGENT']

/** 状态的中文展示名。 */
export const TODO_STATUS_LABELS: Record<TodoStatus, string> = {
  TODO: '待开始',
  IN_PROGRESS: '进行中',
  DONE: '已完成',
}

/** 优先级的中文展示名。 */
export const TODO_PRIORITY_LABELS: Record<TodoPriority, string> = {
  LOW: '低',
  NORMAL: '普通',
  HIGH: '高',
  URGENT: '紧急',
}

/** 优先级对应的徽标配色（Tailwind 类名）。 */
export const TODO_PRIORITY_CLASSES: Record<TodoPriority, string> = {
  LOW: 'bg-gray-100 text-gray-600',
  NORMAL: 'bg-sky-50 text-sky-700',
  HIGH: 'bg-amber-50 text-amber-700',
  URGENT: 'bg-red-50 text-red-700',
}

/** 左侧导航当前选中的范围：'all' 全部、'inbox' 未归类、number 是清单 id。 */
export type TodoSelection = 'all' | 'inbox' | number

export interface TodoList {
  id: number
  name: string
  description: string | null
  color: string | null
  sortOrder: number
  isArchived: boolean
  itemCount: number
  completedCount: number
  createdAt: string
  updatedAt: string
}

export interface Tag {
  id: number
  name: string
  color: string | null
}

export interface TodoItem {
  id: number
  listId: number | null
  listName: string | null
  parentId: number | null
  title: string
  description: string | null
  status: TodoStatus
  priority: TodoPriority
  dueAt: string | null
  completedAt: string | null
  sortOrder: number
  /** 由后端按「有截止时间 + 未完成 + 已过期」算好，前端不重算 */
  isOverdue: boolean
  childCount: number
  completedChildCount: number
  tags: Tag[]
  createdAt: string
  updatedAt: string
}

/** 单条待办详情：在扁平模型之外带上子任务。 */
export interface TodoItemDetail {
  item: TodoItem
  children: TodoItem[]
}

export interface CreateTodoListInput {
  name: string
  description?: string | null
  color?: string | null
  sortOrder?: number | null
}

/** 后端是 PUT 整体替换语义，所以字段都是必填（可空的除外）。 */
export interface UpdateTodoListInput {
  name: string
  description?: string | null
  color?: string | null
  sortOrder?: number | null
  isArchived: boolean
}

export interface CreateTagInput {
  name: string
  color?: string | null
}

/** 后端 UpdateTagRequest，整体替换语义（color 传 null 表示清空颜色）。 */
export interface UpdateTagInput {
  name: string
  color?: string | null
}

/**
 * 标签可选色。用固定调色板而不是自由取色：标签颜色唯一的用途是一眼区分，
 * 让用户随手挑十六进制值很容易挑出两个几乎一样的颜色，深色值配浅色文字也看不清。
 */
export const TAG_COLORS: readonly string[] = [
  '#E24B4A',
  '#EF9F27',
  '#639922',
  '#378ADD',
  '#7F77DD',
  '#D4537E',
]

/**
 * 待办列表的筛选条件。不含「范围」（清单 / 未归类）—— 那个由左侧导航决定，
 * 两者是正交的两件事，混在一个对象里会让「切清单时筛选被重置」这类 bug 难以避免。
 */
export interface TodoFilters {
  /** 标题关键字，已做防抖处理后的值。 */
  keyword: string
  priority: TodoPriority | null
  tagId: number | null
}

/** 空筛选条件，用于重置。 */
export const EMPTY_TODO_FILTERS: TodoFilters = {
  keyword: '',
  priority: null,
  tagId: null,
}

export interface CreateTodoItemInput {
  title: string
  description?: string | null
  listId?: number | null
  parentId?: number | null
  status?: TodoStatus | null
  priority?: TodoPriority | null
  dueAt?: string | null
  sortOrder?: number | null
}

/** 后端 UpdateTodoItemRequest，整体替换语义。 */
export interface UpdateTodoItemInput {
  title: string
  description?: string | null
  listId?: number | null
  status: TodoStatus
  priority: TodoPriority
  dueAt?: string | null
  sortOrder?: number | null
}

export interface TodoItemQuery {
  listId?: number | null
  unassigned?: boolean
  parentId?: number | null
  tagId?: number | null
  statuses?: readonly TodoStatus[]
  priority?: TodoPriority | null
  dueBefore?: string | null
  keyword?: string | null
}
