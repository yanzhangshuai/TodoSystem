import { useRef, useState } from 'react'
import type { TodoList, TodoSelection, UpdateTodoListInput } from '@/types'

interface Props {
  lists: TodoList[]
  selected: TodoSelection
  loading: boolean
  pending: boolean
  includeArchived: boolean
  onToggleArchived: (next: boolean) => void
  onSelect: (selection: TodoSelection) => void
  onCreate: (name: string) => Promise<boolean>
  onUpdate: (list: TodoList, patch: Partial<UpdateTodoListInput>) => Promise<boolean>
  onRemove: (id: number) => void
}

/**
 * 左侧清单导航。
 *
 * 「全部待办」和「未归类」是两个虚拟项，不对应任何数据库行：all 不加 listId 过滤，
 * inbox 是 listId IS NULL。它们必须和真实清单共用一套选中态，否则会出现两套高亮逻辑。
 */
export default function TodoListSidebar({
  lists,
  selected,
  loading,
  pending,
  includeArchived,
  onToggleArchived,
  onSelect,
  onCreate,
  onUpdate,
  onRemove,
}: Props) {
  const [name, setName] = useState('')
  const [renamingId, setRenamingId] = useState<number | null>(null)

  /**
   * Escape 取消重命名用。
   *
   * 提交路径只有 onBlur 一条，Enter 也走 blur() 触发它 —— 两边各提交一次会打两个 PUT。
   * Escape 需要「blur 时别提交」，用 ref 当一次性开关（state 在这里没用：
   * blur 处理器闭包拿到的是上一次渲染的值）。
   */
  const cancelRename = useRef(false)

  async function handleCreate(): Promise<void> {
    const trimmed = name.trim()

    if (!trimmed) {
      return
    }

    if (await onCreate(trimmed)) {
      setName('')
    }
  }

  /** 提交重命名。值直接从 DOM 读（输入框非受控），避免 Escape 后 blur 时 state 还没刷新。 */
  async function commitRename(list: TodoList, value: string): Promise<void> {
    setRenamingId(null)

    if (cancelRename.current) {
      cancelRename.current = false
      return
    }

    const trimmed = value.trim()

    // 没改、或者被清空了，都当成「放弃」——空名字会让清单在导航里彻底消失
    if (!trimmed || trimmed === list.name) {
      return
    }

    await onUpdate(list, { name: trimmed })
  }

  function navClass(active: boolean): string {
    return [
      'flex min-w-0 flex-1 items-center justify-between rounded-lg px-3 py-2 text-left text-sm transition',
      active ? 'bg-gray-900 text-white' : 'text-gray-700 hover:bg-gray-100',
    ].join(' ')
  }

  /** 行尾的小按钮：平时隐形，hover 才浮出来，避免导航被按钮塞满。 */
  const rowButtonClass =
    'shrink-0 rounded px-1.5 py-1 text-xs text-gray-400 opacity-0 transition hover:bg-gray-100 disabled:opacity-30 group-hover:opacity-100'

  return (
    <aside className="rounded-2xl border border-gray-200 bg-white p-4">
      <h2 className="mb-3 text-sm font-medium text-gray-900">清单</h2>

      <nav className="space-y-1">
        <button type="button" onClick={() => onSelect('all')} className={navClass(selected === 'all')}>
          <span>全部待办</span>
        </button>

        <button
          type="button"
          onClick={() => onSelect('inbox')}
          className={navClass(selected === 'inbox')}
        >
          <span>未归类</span>
        </button>

        {loading && <div className="px-3 py-2 text-xs text-gray-400">加载中…</div>}

        {!loading &&
          lists.map((list) => (
            <div key={list.id} className="group flex items-center gap-1">
              {renamingId === list.id ? (
                <input
                  // 非受控 + key：进入编辑态时用 defaultValue 填一次，之后由 DOM 自己管
                  key={`rename-${list.id}`}
                  autoFocus
                  defaultValue={list.name}
                  maxLength={100}
                  disabled={pending}
                  onBlur={(e) => void commitRename(list, e.currentTarget.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.currentTarget.blur()
                    } else if (e.key === 'Escape') {
                      cancelRename.current = true
                      e.currentTarget.blur()
                    }
                  }}
                  className="min-w-0 flex-1 rounded-lg border border-gray-300 px-2 py-1.5 text-sm outline-none focus:border-gray-500"
                />
              ) : (
                <>
                  <button
                    type="button"
                    onClick={() => onSelect(list.id)}
                    onDoubleClick={() => setRenamingId(list.id)}
                    title="双击改名"
                    className={[navClass(selected === list.id), list.isArchived ? 'opacity-60' : ''].join(
                      ' ',
                    )}
                  >
                    <span className="truncate">{list.name}</span>

                    <span className="ml-2 flex shrink-0 items-center gap-1.5">
                      {list.isArchived && (
                        <span
                          className={[
                            'rounded px-1 text-[10px]',
                            selected === list.id
                              ? 'bg-white/20 text-gray-200'
                              : 'bg-gray-200 text-gray-500',
                          ].join(' ')}
                        >
                          已归档
                        </span>
                      )}

                      <span
                        className={[
                          'text-[11px]',
                          selected === list.id ? 'text-gray-300' : 'text-gray-400',
                        ].join(' ')}
                      >
                        {list.completedCount}/{list.itemCount}
                      </span>
                    </span>
                  </button>

                  <button
                    type="button"
                    title="重命名"
                    disabled={pending}
                    onClick={() => setRenamingId(list.id)}
                    className={rowButtonClass}
                  >
                    ✎
                  </button>

                  <button
                    type="button"
                    title={list.isArchived ? '取消归档' : '归档（不删除，只是收起来）'}
                    disabled={pending}
                    onClick={() => void onUpdate(list, { isArchived: !list.isArchived })}
                    className={rowButtonClass}
                  >
                    {list.isArchived ? '恢复' : '归档'}
                  </button>

                  <button
                    type="button"
                    title="删除清单"
                    disabled={pending}
                    onClick={() => void onRemove(list.id)}
                    className="shrink-0 rounded px-1.5 py-1 text-xs text-gray-400 opacity-0 transition hover:bg-red-50 hover:text-red-600 disabled:opacity-30 group-hover:opacity-100"
                  >
                    ×
                  </button>
                </>
              )}
            </div>
          ))}
      </nav>

      <label className="mt-3 flex cursor-pointer items-center gap-2 px-1 text-xs text-gray-500">
        <input
          type="checkbox"
          checked={includeArchived}
          onChange={(e) => onToggleArchived(e.target.checked)}
          className="h-3.5 w-3.5 cursor-pointer accent-gray-900"
        />
        显示已归档
      </label>

      <div className="mt-4 border-t border-gray-100 pt-3">
        <div className="flex gap-1">
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                void handleCreate()
              }
            }}
            placeholder="新建清单"
            maxLength={100}
            className="min-w-0 flex-1 rounded-lg border border-gray-300 px-2 py-1.5 text-sm outline-none focus:border-gray-500"
          />

          <button
            type="button"
            disabled={pending || !name.trim()}
            onClick={() => void handleCreate()}
            className="rounded-lg bg-gray-900 px-2.5 py-1.5 text-sm text-white transition hover:bg-gray-800 disabled:opacity-40"
          >
            添加
          </button>
        </div>

        <p className="mt-2 text-[11px] leading-relaxed text-gray-400">
          删除清单不会删除其中的待办，它们会退回「未归类」。
          <br />
          双击清单名可以改名。
        </p>
      </div>
    </aside>
  )
}
