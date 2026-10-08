import { useMemo, useState } from 'react'
import {
  ErrorBanner,
  GoogleLoginButton,
  TagManager,
  TodoItemRow,
  TodoListSidebar,
  type TodoItemActions,
} from '@/components'
import { useBindGoogle, useDebouncedValue, useTags, useTodoItems, useTodoLists } from '@/hooks'
import { AppLayout } from '@/layouts'
import { useAuth } from '@/stores'
import {
  EMPTY_TODO_FILTERS,
  TODO_PRIORITIES,
  TODO_PRIORITY_LABELS,
  type TodoFilters,
  type TodoItem,
  type TodoPriority,
  type TodoSelection,
} from '@/types'

/**
 * 首页 / 控制台。数据逻辑全在 Hook 里，本组件只负责画和串起来。
 * 三个 Hook 都返回 pending，在这个页面里必然撞名，所以在解构处各自改名。
 */
export default function DashboardPage() {
  const { profile, config } = useAuth()

  /** 左侧选中范围：'all' | 'inbox' | 清单 id。 */
  const [selection, setSelection] = useState<TodoSelection>('all')

  /** 新建待办用的输入。和筛选是两回事，不能共用一个「关键字」状态。 */
  const [title, setTitle] = useState('')
  const [priority, setPriority] = useState<TodoPriority>('NORMAL')

  /**
   * 关键字单独存一份未防抖的输入值（输入框要即时回显），真正发给后端的条件在渲染时合并。
   * 不用 useEffect 把防抖值同步回 state —— 那会多一次渲染，中间态也容易出 bug。
   */
  const [keywordInput, setKeywordInput] = useState('')
  const [filters, setFilters] = useState<TodoFilters>(EMPTY_TODO_FILTERS)
  const debouncedKeyword = useDebouncedValue(keywordInput)

  const {
    lists,
    includeArchived,
    setIncludeArchived,
    loading: listsLoading,
    pending: listBusy,
    error: listError,
    create: createList,
    update: updateList,
    remove: removeList,
  } = useTodoLists()

  const {
    tags,
    pending: tagBusy,
    error: tagError,
    create: createTag,
    update: updateTag,
    remove: removeTag,
  } = useTags()

  // 拆成两个基本类型再传下去：对象每次渲染都是新引用，会让请求无限循环（见 useTodoItems）
  const listId = typeof selection === 'number' ? selection : null
  const unassigned = selection === 'inbox'

  const scope = useMemo(() => ({ listId, unassigned }), [listId, unassigned])

  const query = useMemo<TodoFilters>(
    () => ({ ...filters, keyword: debouncedKeyword.trim() }),
    [filters, debouncedKeyword],
  )

  const {
    items,
    loading,
    pending: itemBusy,
    error,
    create: createItem,
    createSubtask,
    toggle,
    setStatus,
    saveItem,
    setDueAt,
    moveToList,
    remove: removeItem,
    attachTag,
    detachTag,
  } = useTodoItems(scope, query)

  const { pending: linking, error: linkError, setError: setLinkError, bind } = useBindGoogle()

  /** 依赖全是稳定函数，所以这个对象实际只创建一次，不会让所有行白白重渲染。 */
  const itemActions = useMemo<TodoItemActions>(
    () => ({
      toggle,
      setStatus,
      remove: removeItem,
      save: saveItem,
      setDueAt,
      moveToList,
      attachTag,
      detachTag,
      createSubtask,
    }),
    [toggle, setStatus, removeItem, saveItem, setDueAt, moveToList, attachTag, detachTag, createSubtask],
  )

  /**
   * 按 parentId 把扁平结果分成主任务和各自的子任务。后端返回的是扁平数组，
   * 前端分组而不是再发一轮 parentId 查询，也避免了「父任务还没回来、子任务先到」的顺序问题。
   *
   * !ids.has(parentId) 那个分支：搜索时命中的子任务可能带着一个没命中的父任务，
   * 照常归到不存在的父节点下就会凭空消失，所以把它当顶层渲染。
   */
  const childrenByParent = useMemo(() => {
    const ids = new Set(items.map((item) => item.id))
    const roots: TodoItem[] = []
    const byParent = new Map<number, TodoItem[]>()

    for (const item of items) {
      if (item.parentId === null || !ids.has(item.parentId)) {
        roots.push(item)
        continue
      }

      const siblings = byParent.get(item.parentId)

      if (siblings) {
        siblings.push(item)
      } else {
        byParent.set(item.parentId, [item])
      }
    }

    return { roots, byParent }
  }, [items])

  /**
   * 统计只算顶层待办。把子任务也算进「未完成」会让数字虚高 ——
   * 用户心里数的是「还有几件事」，一个拆成五步的任务却会变成五个。
   */
  const { roots, byParent } = childrenByParent
  const doneCount = roots.filter((item) => item.status === 'DONE').length
  const overdueCount = roots.filter((item) => item.isOverdue).length
  const openCount = roots.length - doneCount

  const hasFilter = keywordInput.trim() !== '' || filters.priority !== null || filters.tagId !== null

  const currentName =
    selection === 'all'
      ? '全部待办'
      : selection === 'inbox'
        ? '未归类'
        : (lists.find((list) => list.id === selection)?.name ?? '清单')

  async function handleAdd(): Promise<void> {
    const trimmed = title.trim()

    if (!trimmed) {
      return
    }

    if (await createItem(trimmed, priority)) {
      setTitle('')
      setPriority('NORMAL')
    }
  }

  function clearFilters(): void {
    setKeywordInput('')
    setFilters(EMPTY_TODO_FILTERS)
  }

  const selectClass =
    'rounded-lg border border-gray-300 px-2 py-1.5 text-sm text-gray-700 outline-none focus:border-gray-500'

  return (
    <AppLayout>
      <div className="mb-6 grid gap-4 sm:grid-cols-3">
        <div className="rounded-2xl border border-gray-200 bg-white p-5">
          <div className="text-xs text-gray-500">未完成</div>
          <div className="mt-1 text-2xl font-medium text-gray-900">{openCount}</div>
        </div>

        <div className="rounded-2xl border border-gray-200 bg-white p-5">
          <div className="text-xs text-gray-500">已完成</div>
          <div className="mt-1 text-2xl font-medium text-gray-900">{doneCount}</div>
        </div>

        <div className="rounded-2xl border border-gray-200 bg-white p-5">
          <div className="text-xs text-gray-500">已逾期</div>
          <div
            className={[
              'mt-1 text-2xl font-medium',
              overdueCount > 0 ? 'text-red-600' : 'text-gray-900',
            ].join(' ')}
          >
            {overdueCount}
          </div>
        </div>
      </div>

      <div className="grid gap-6 md:grid-cols-[240px_1fr]">
        <div className="space-y-6">
          <TodoListSidebar
            lists={lists}
            selected={selection}
            loading={listsLoading}
            pending={listBusy}
            includeArchived={includeArchived}
            onToggleArchived={setIncludeArchived}
            onSelect={setSelection}
            onCreate={createList}
            onUpdate={updateList}
            onRemove={removeList}
          />

          <TagManager
            tags={tags}
            pending={tagBusy}
            onCreate={createTag}
            onUpdate={updateTag}
            onRemove={removeTag}
          />
        </div>

        <section className="rounded-2xl border border-gray-200 bg-white">
          <div className="space-y-3 border-b border-gray-100 px-4 py-3">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h2 className="text-sm font-medium text-gray-900">{currentName}</h2>

              <div className="flex items-center gap-2">
                <select
                  value={priority}
                  onChange={(e) => setPriority(e.target.value as TodoPriority)}
                  title="新待办的优先级"
                  className={selectClass}
                >
                  {TODO_PRIORITIES.map((value) => (
                    <option key={value} value={value}>
                      {TODO_PRIORITY_LABELS[value]}
                    </option>
                  ))}
                </select>

                <input
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      void handleAdd()
                    }
                  }}
                  placeholder="添加待办，回车确认"
                  maxLength={200}
                  className="w-56 rounded-lg border border-gray-300 px-3 py-1.5 text-sm outline-none focus:border-gray-500"
                />

                <button
                  type="button"
                  disabled={itemBusy || !title.trim()}
                  onClick={() => void handleAdd()}
                  className="rounded-lg bg-gray-900 px-3 py-1.5 text-sm text-white transition hover:bg-gray-800 disabled:opacity-40"
                >
                  添加
                </button>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <input
                value={keywordInput}
                onChange={(e) => setKeywordInput(e.target.value)}
                placeholder="搜索标题…"
                maxLength={200}
                className="w-44 rounded-lg border border-gray-300 px-3 py-1.5 text-sm outline-none focus:border-gray-500"
              />

              <select
                value={filters.priority ?? ''}
                onChange={(e) =>
                  setFilters((f) => ({ ...f, priority: (e.target.value || null) as TodoPriority | null }))
                }
                className={selectClass}
              >
                <option value="">全部优先级</option>
                {TODO_PRIORITIES.map((value) => (
                  <option key={value} value={value}>
                    {TODO_PRIORITY_LABELS[value]}
                  </option>
                ))}
              </select>

              <select
                value={filters.tagId ?? ''}
                onChange={(e) =>
                  setFilters((f) => ({ ...f, tagId: e.target.value ? Number(e.target.value) : null }))
                }
                className={selectClass}
              >
                <option value="">全部标签</option>
                {tags.map((tag) => (
                  <option key={tag.id} value={tag.id}>
                    {tag.name}
                  </option>
                ))}
              </select>

              {hasFilter && (
                <button
                  type="button"
                  onClick={clearFilters}
                  className="rounded-lg px-2.5 py-1.5 text-sm text-gray-500 transition hover:bg-gray-100 hover:text-gray-800"
                >
                  清除筛选
                </button>
              )}

              {hasFilter && (
                <span className="text-[11px] text-gray-400">
                  {roots.length} 条结果
                </span>
              )}
            </div>
          </div>

          <div className="px-4 pt-3">
            <ErrorBanner message={error} />
            <ErrorBanner message={listError} />
            <ErrorBanner message={tagError} />
          </div>

          {loading ? (
            <div className="py-10 text-center text-sm text-gray-400">加载中…</div>
          ) : roots.length === 0 ? (
            <div className="py-10 text-center text-sm text-gray-400">
              {hasFilter ? '没有符合条件的待办' : '暂无待办'}
            </div>
          ) : (
            <ul>
              {roots.map((item) => (
                <TodoItemRow
                  key={item.id}
                  item={item}
                  subtasks={byParent.get(item.id) ?? []}
                  allTags={tags}
                  lists={lists}
                  busy={itemBusy}
                  actions={itemActions}
                />
              ))}
            </ul>
          )}
        </section>
      </div>

      {profile && !profile.hasGoogleBound && (
        <section className="mt-6 rounded-2xl border border-gray-200 bg-white p-5">
          <h2 className="text-sm font-medium text-gray-900">绑定 Google 账号</h2>
          <p className="mt-1 mb-3 text-xs leading-relaxed text-gray-500">
            绑定后可以直接用 Google 一键登录。
            {!profile.emailVerified &&
              '你的邮箱尚未验证，所以系统不会自动把 Google 登录合并到本账号——必须在这里主动绑定。'}
          </p>

          <ErrorBanner className="mb-3" message={linkError} />

          <GoogleLoginButton
            clientId={config?.googleClientId ?? null}
            disabled={linking}
            text="signin_with"
            width={280}
            onCredential={bind}
            onError={setLinkError}
          />
        </section>
      )}
    </AppLayout>
  )
}
