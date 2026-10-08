import { useState } from 'react'
import {
  TODO_PRIORITIES,
  TODO_PRIORITY_CLASSES,
  TODO_PRIORITY_LABELS,
  TODO_STATUSES,
  TODO_STATUS_LABELS,
  type Tag,
  type TodoItem,
  type TodoList,
  type TodoPriority,
  type TodoStatus,
  type UpdateTodoItemInput,
} from '@/types'

/**
 * 一行待办需要的全部写操作。
 *
 * 九个动作加上数据字段要接十四个 props，真正的数据字段会被淹没在回调里，
 * 所以按「动作集合」传一个对象，数据字段保持显式 —— 读 props 就知道这行依赖什么。
 */
export interface TodoItemActions {
  toggle: (item: TodoItem) => void
  setStatus: (item: TodoItem, status: TodoStatus) => void
  remove: (id: number) => void
  save: (itemId: number, input: UpdateTodoItemInput) => Promise<boolean>
  setDueAt: (item: TodoItem, dueAt: string | null) => void
  moveToList: (item: TodoItem, listId: number | null) => void
  attachTag: (itemId: number, tagId: number) => void
  detachTag: (itemId: number, tagId: number) => void
  createSubtask: (parent: TodoItem, title: string) => Promise<boolean>
}

interface Props {
  item: TodoItem
  /** 这一行的子任务。由页面从扁平结果里按 parentId 分好组传进来。 */
  subtasks: TodoItem[]
  allTags: Tag[]
  lists: TodoList[]
  busy: boolean
  actions: TodoItemActions
}

/** ISO 时间 → `<input type="date">` 需要的 yyyy-MM-dd（按本地时区）。 */
function toDateInput(iso: string | null): string {
  if (!iso) {
    return ''
  }

  const date = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

/** yyyy-MM-dd（本地）→ ISO UTC 字符串。后端统一按 UTC 存。 */
function fromDateInput(value: string): string | null {
  if (!value) {
    return null
  }

  return new Date(`${value}T00:00:00`).toISOString()
}

/**
 * 编辑表单。
 *
 * 拆成独立组件是因为它只在编辑态挂载，收起时这些 state 跟着销毁 ——
 * 放在行组件里的话，收起后再打开会看到上一次没保存的残留输入。
 *
 * 这里的 status 是整体替换（走 PUT），和行内「开始」按钮（走专门的 status 端点）
 * 是两条路：后者是高频单字段动作，前者是「一次改好几处」。
 */
function EditForm({
  item,
  lists,
  busy,
  onSave,
  onCancel,
}: {
  item: TodoItem
  lists: TodoList[]
  busy: boolean
  onSave: (itemId: number, input: UpdateTodoItemInput) => Promise<boolean>
  onCancel: () => void
}) {
  const [title, setTitle] = useState(item.title)
  const [description, setDescription] = useState(item.description ?? '')
  const [status, setStatus] = useState<TodoStatus>(item.status)
  const [priority, setPriority] = useState<TodoPriority>(item.priority)
  const [listId, setListId] = useState<number | null>(item.listId)
  const [dueAt, setDueAt] = useState(toDateInput(item.dueAt))

  async function handleSubmit(): Promise<void> {
    const trimmed = title.trim()

    if (!trimmed) {
      return
    }

    const ok = await onSave(item.id, {
      title: trimmed,
      // 空描述存 null 而不是空串：null 表示「没有」，空串是「有一段空文字」，
      // 后者在列表里会渲染出一个多余的空行
      description: description.trim() || null,
      listId,
      status,
      priority,
      dueAt: fromDateInput(dueAt),
      sortOrder: item.sortOrder,
    })

    if (ok) {
      onCancel()
    }
  }

  const fieldClass =
    'w-full rounded-lg border border-gray-300 px-2 py-1.5 text-sm outline-none focus:border-gray-500'

  return (
    <div className="mt-2 space-y-2 rounded-lg border border-gray-200 bg-gray-50/60 p-3">
      <input
        autoFocus
        value={title}
        maxLength={200}
        disabled={busy}
        onChange={(e) => setTitle(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') {
            void handleSubmit()
          } else if (e.key === 'Escape') {
            onCancel()
          }
        }}
        placeholder="标题"
        className={fieldClass}
      />

      <textarea
        value={description}
        maxLength={2000}
        rows={2}
        disabled={busy}
        onChange={(e) => setDescription(e.target.value)}
        placeholder="描述（可选）"
        className={[fieldClass, 'resize-y'].join(' ')}
      />

      <div className="grid gap-2 sm:grid-cols-2">
        <label className="flex items-center gap-2 text-xs text-gray-500">
          <span className="w-12 shrink-0">状态</span>
          <select
            value={status}
            disabled={busy}
            onChange={(e) => setStatus(e.target.value as TodoStatus)}
            className={fieldClass}
          >
            {TODO_STATUSES.map((value) => (
              <option key={value} value={value}>
                {TODO_STATUS_LABELS[value]}
              </option>
            ))}
          </select>
        </label>

        <label className="flex items-center gap-2 text-xs text-gray-500">
          <span className="w-12 shrink-0">优先级</span>
          <select
            value={priority}
            disabled={busy}
            onChange={(e) => setPriority(e.target.value as TodoPriority)}
            className={fieldClass}
          >
            {TODO_PRIORITIES.map((value) => (
              <option key={value} value={value}>
                {TODO_PRIORITY_LABELS[value]}
              </option>
            ))}
          </select>
        </label>

        <label className="flex items-center gap-2 text-xs text-gray-500">
          <span className="w-12 shrink-0">清单</span>
          <select
            value={listId ?? ''}
            disabled={busy}
            onChange={(e) => setListId(e.target.value ? Number(e.target.value) : null)}
            className={fieldClass}
          >
            <option value="">未归类</option>
            {lists.map((list) => (
              <option key={list.id} value={list.id}>
                {list.name}
              </option>
            ))}
          </select>
        </label>

        <label className="flex items-center gap-2 text-xs text-gray-500">
          <span className="w-12 shrink-0">截止</span>
          <input
            type="date"
            value={dueAt}
            disabled={busy}
            onChange={(e) => setDueAt(e.target.value)}
            className={fieldClass}
          />
        </label>
      </div>

      <div className="flex justify-end gap-1">
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg px-2.5 py-1.5 text-sm text-gray-600 transition hover:bg-gray-100"
        >
          取消
        </button>
        <button
          type="button"
          disabled={busy || !title.trim()}
          onClick={() => void handleSubmit()}
          className="rounded-lg bg-gray-900 px-3 py-1.5 text-sm text-white transition hover:bg-gray-800 disabled:opacity-40"
        >
          保存
        </button>
      </div>
    </div>
  )
}

/**
 * 子任务行。刻意做得比父行轻：没有编辑表单、没有标签、没有移动清单。
 * 给它一整套编辑能力只会让缩进的区域比主体还重，反而看不清主次。
 */
function SubtaskRow({
  subtask,
  busy,
  actions,
}: {
  subtask: TodoItem
  busy: boolean
  actions: TodoItemActions
}) {
  const done = subtask.status === 'DONE'

  return (
    <li className="flex items-center gap-2 py-1">
      <input
        type="checkbox"
        checked={done}
        disabled={busy}
        onChange={() => actions.toggle(subtask)}
        className="h-3.5 w-3.5 shrink-0 cursor-pointer accent-gray-900"
      />

      <span
        className={['min-w-0 flex-1 truncate text-xs', done ? 'text-gray-400 line-through' : 'text-gray-700'].join(
          ' ',
        )}
      >
        {subtask.title}
      </span>

      {!done && subtask.status === 'IN_PROGRESS' && (
        <button
          type="button"
          disabled={busy}
          title="点一下回到「待开始」"
          onClick={() => actions.setStatus(subtask, 'TODO')}
          className="shrink-0 rounded bg-blue-50 px-1.5 py-0.5 text-[10px] text-blue-700 transition hover:bg-blue-100 disabled:opacity-40"
        >
          {TODO_STATUS_LABELS.IN_PROGRESS}
        </button>
      )}

      <button
        type="button"
        title="删除子任务"
        disabled={busy}
        onClick={() => actions.remove(subtask.id)}
        className="shrink-0 rounded px-1 text-xs text-gray-300 transition hover:bg-red-50 hover:text-red-600 disabled:opacity-40"
      >
        ×
      </button>
    </li>
  )
}

/**
 * 一行待办。
 *
 * 子任务计数用 subtasks.length（当前显示出来的数量）而不是后端的 childCount ——
 * 筛选状态下后端给的是全部子任务数，界面只列出了匹配的那几条，对不上会让用户以为丢数据。
 * childCount 是留给「只拉父任务」那种客户端的（用 parentId=null 过滤时才有意义）。
 */
export default function TodoItemRow({ item, subtasks, allTags, lists, busy, actions }: Props) {
  const [editing, setEditing] = useState(false)
  const [expanded, setExpanded] = useState(false)
  const [subtaskTitle, setSubtaskTitle] = useState('')

  const done = item.status === 'DONE'
  const inProgress = item.status === 'IN_PROGRESS'
  /** 只有顶层待办能加子任务 —— 后端限制两层，这里也不给入口。 */
  const canHaveSubtasks = item.parentId === null
  const completedSubtasks = subtasks.filter((s) => s.status === 'DONE').length

  // 只列出还没挂上的标签，避免下拉框里出现已经生效的选项
  const availableTags = allTags.filter((tag) => !item.tags.some((t) => t.id === tag.id))

  async function handleAddSubtask(): Promise<void> {
    const trimmed = subtaskTitle.trim()

    if (!trimmed) {
      return
    }

    if (await actions.createSubtask(item, trimmed)) {
      setSubtaskTitle('')
      setExpanded(true)
    }
  }

  return (
    <li className="border-b border-gray-100 last:border-b-0">
      <div className="flex items-start gap-3 px-4 py-3">
        <input
          type="checkbox"
          checked={done}
          disabled={busy}
          onChange={() => actions.toggle(item)}
          className="mt-0.5 h-4 w-4 shrink-0 cursor-pointer accent-gray-900"
        />

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <span
              className={['text-sm', done ? 'text-gray-400 line-through' : 'text-gray-900'].join(' ')}
            >
              {item.title}
            </span>

            <span
              className={['rounded px-1.5 py-0.5 text-[11px]', TODO_PRIORITY_CLASSES[item.priority]].join(
                ' ',
              )}
            >
              {TODO_PRIORITY_LABELS[item.priority]}
            </span>

            {/*
              状态按钮：未完成时才出现，点一下在「待开始 / 进行中」之间切。
              不做三态循环 —— 从「进行中」点一下变成「已完成」是误触，
              而「已完成」已经有复选框这个更直觉的入口。
            */}
            {!done && (
              <button
                type="button"
                disabled={busy}
                onClick={() => actions.setStatus(item, inProgress ? 'TODO' : 'IN_PROGRESS')}
                title={inProgress ? '点一下回到「待开始」' : '标记为「进行中」'}
                className={[
                  'rounded px-1.5 py-0.5 text-[11px] transition disabled:opacity-40',
                  inProgress
                    ? 'bg-blue-50 text-blue-700 hover:bg-blue-100'
                    : 'text-gray-400 hover:bg-gray-100 hover:text-gray-700',
                ].join(' ')}
              >
                {inProgress ? TODO_STATUS_LABELS.IN_PROGRESS : '开始'}
              </button>
            )}

            {item.isOverdue && (
              <span className="rounded bg-red-50 px-1.5 py-0.5 text-[11px] text-red-700">已逾期</span>
            )}

            {subtasks.length > 0 && (
              <button
                type="button"
                onClick={() => setExpanded((v) => !v)}
                title={expanded ? '收起子任务' : '展开子任务'}
                className="rounded px-1 py-0.5 text-[11px] text-gray-400 transition hover:bg-gray-100 hover:text-gray-600"
              >
                {expanded ? '▾' : '▸'} 子任务 {completedSubtasks}/{subtasks.length}
              </button>
            )}

            {item.listName && <span className="text-[11px] text-gray-400">#{item.listName}</span>}
          </div>

          {item.description && (
            <p className="mt-1 line-clamp-2 text-xs leading-relaxed whitespace-pre-wrap text-gray-500">
              {item.description}
            </p>
          )}

          <div className="mt-1.5 flex flex-wrap items-center gap-2">
            {/* 已挂的标签，点 × 摘掉 */}
            {item.tags.map((tag) => (
              <span
                key={tag.id}
                className="flex items-center gap-1 rounded-full bg-gray-100 py-0.5 pr-1 pl-2 text-[11px] text-gray-600"
              >
                <span
                  className="h-1.5 w-1.5 shrink-0 rounded-full"
                  style={{ backgroundColor: tag.color ?? '#D1D5DB' }}
                />
                {tag.name}
                <button
                  type="button"
                  title="摘掉标签"
                  disabled={busy}
                  onClick={() => actions.detachTag(item.id, tag.id)}
                  className="rounded-full px-0.5 text-gray-400 transition hover:text-red-600 disabled:opacity-40"
                >
                  ×
                </button>
              </span>
            ))}

            {availableTags.length > 0 && (
              <select
                value=""
                disabled={busy}
                onChange={(e) => {
                  if (e.target.value) {
                    actions.attachTag(item.id, Number(e.target.value))
                  }
                }}
                className="rounded border border-gray-200 bg-white px-1 py-0.5 text-[11px] text-gray-500 outline-none"
              >
                <option value="">+ 标签</option>
                {availableTags.map((tag) => (
                  <option key={tag.id} value={tag.id}>
                    {tag.name}
                  </option>
                ))}
              </select>
            )}

            <input
              type="date"
              value={toDateInput(item.dueAt)}
              disabled={busy}
              title="截止时间"
              onChange={(e) => actions.setDueAt(item, fromDateInput(e.target.value))}
              className="rounded border border-gray-200 px-1 py-0.5 text-[11px] text-gray-500 outline-none"
            />

            {canHaveSubtasks && (
              <button
                type="button"
                disabled={busy}
                onClick={() => setExpanded((v) => !v)}
                className="rounded px-1 py-0.5 text-[11px] text-gray-400 transition hover:bg-gray-100 hover:text-gray-600"
              >
                + 子任务
              </button>
            )}
          </div>

          {editing && (
            <EditForm
              item={item}
              lists={lists}
              busy={busy}
              onSave={actions.save}
              onCancel={() => setEditing(false)}
            />
          )}

          {canHaveSubtasks && expanded && (
            <div className="mt-2 border-l-2 border-gray-100 pl-3">
              {subtasks.length === 0 ? (
                <p className="py-1 text-xs text-gray-400">还没有子任务</p>
              ) : (
                <ul>
                  {subtasks.map((subtask) => (
                    <SubtaskRow key={subtask.id} subtask={subtask} busy={busy} actions={actions} />
                  ))}
                </ul>
              )}

              <div className="mt-1 flex items-center gap-1">
                <input
                  value={subtaskTitle}
                  maxLength={200}
                  disabled={busy}
                  onChange={(e) => setSubtaskTitle(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      void handleAddSubtask()
                    }
                  }}
                  placeholder="添加子任务，回车确认"
                  className="min-w-0 flex-1 rounded border border-gray-200 px-2 py-1 text-xs outline-none focus:border-gray-400"
                />

                <button
                  type="button"
                  disabled={busy || !subtaskTitle.trim()}
                  onClick={() => void handleAddSubtask()}
                  className="rounded border border-gray-300 px-2 py-1 text-xs text-gray-700 transition hover:bg-gray-50 disabled:opacity-40"
                >
                  添加
                </button>
              </div>
            </div>
          )}
        </div>

        <div className="flex shrink-0 items-center gap-0.5">
          <button
            type="button"
            title={editing ? '收起编辑' : '编辑'}
            disabled={busy}
            onClick={() => setEditing((v) => !v)}
            className={[
              'rounded px-1.5 py-1 text-xs transition disabled:opacity-40',
              editing ? 'bg-gray-100 text-gray-900' : 'text-gray-400 hover:bg-gray-100 hover:text-gray-700',
            ].join(' ')}
          >
            ✎
          </button>

          <button
            type="button"
            title="删除待办（子任务一并删除）"
            disabled={busy}
            onClick={() => actions.remove(item.id)}
            className="rounded px-1.5 py-1 text-xs text-gray-400 transition hover:bg-red-50 hover:text-red-600 disabled:opacity-40"
          >
            ×
          </button>
        </div>
      </div>
    </li>
  )
}
