import { useState } from 'react'
import { TAG_COLORS, type Tag, type UpdateTagInput } from '@/types'

interface Props {
  tags: Tag[]
  pending: boolean
  onCreate: (name: string, color: string | null) => Promise<boolean>
  onUpdate: (tag: Tag, patch: Partial<UpdateTagInput>) => Promise<boolean>
  onRemove: (id: number) => void
}

/**
 * 固定调色板。不给自由取色是有意的：标签颜色唯一的用途是一眼区分，
 * 让用户随手挑十六进制值很容易挑出两个几乎一样的颜色，深色值配深色文字也看不清。
 */
function ColorPicker({
  value,
  disabled,
  onChange,
}: {
  value: string | null
  disabled: boolean
  onChange: (color: string) => void
}) {
  return (
    <div className="flex items-center gap-1">
      {TAG_COLORS.map((color) => (
        <button
          key={color}
          type="button"
          title={color}
          disabled={disabled}
          onClick={() => onChange(color)}
          style={{ backgroundColor: color }}
          className={[
            'h-4 w-4 rounded-full transition disabled:opacity-40',
            value === color
              ? 'ring-2 ring-gray-900 ring-offset-1'
              : 'hover:scale-110 hover:ring-1 hover:ring-gray-300',
          ].join(' ')}
        />
      ))}
    </div>
  )
}

/** 标签圆点。没有颜色的标签用灰点占位，保持每行左边对齐。 */
function TagDot({ color }: { color: string | null }) {
  return (
    <span
      className="h-2 w-2 shrink-0 rounded-full"
      style={{ backgroundColor: color ?? '#D1D5DB' }}
    />
  )
}

/**
 * 标签管理：新建（带颜色）、改名 / 改色、删除。
 * 删除标签会同时清掉它与待办的关联，但不动待办本身。
 */
export default function TagManager({ tags, pending, onCreate, onUpdate, onRemove }: Props) {
  const [name, setName] = useState('')
  const [color, setColor] = useState<string>(TAG_COLORS[0])

  /** 正在编辑的标签 id。同时只允许编辑一个 —— 多个编辑框并排会让人不知道在改哪个。 */
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editName, setEditName] = useState('')
  const [editColor, setEditColor] = useState<string | null>(null)

  async function handleCreate(): Promise<void> {
    const trimmed = name.trim()

    if (!trimmed) {
      return
    }

    if (await onCreate(trimmed, color)) {
      setName('')
    }
  }

  function startEdit(tag: Tag): void {
    setEditingId(tag.id)
    setEditName(tag.name)
    setEditColor(tag.color)
  }

  async function commitEdit(tag: Tag): Promise<void> {
    const trimmed = editName.trim()

    // 清空名字当成放弃 —— 空名标签在列表里没法点，只能靠删除收场
    if (!trimmed) {
      setEditingId(null)
      return
    }

    const changed = trimmed !== tag.name || editColor !== tag.color
    setEditingId(null)

    if (changed) {
      await onUpdate(tag, { name: trimmed, color: editColor })
    }
  }

  return (
    <section className="rounded-2xl border border-gray-200 bg-white p-4">
      <h2 className="mb-3 text-sm font-medium text-gray-900">标签</h2>

      {tags.length === 0 ? (
        <p className="text-xs text-gray-400">还没有标签</p>
      ) : (
        <ul className="mb-3 space-y-1.5">
          {tags.map((tag) =>
            editingId === tag.id ? (
              <li key={tag.id} className="rounded-lg border border-gray-200 p-2">
                <div className="flex items-center gap-1.5">
                  <TagDot color={editColor} />
                  <input
                    autoFocus
                    value={editName}
                    maxLength={50}
                    disabled={pending}
                    onChange={(e) => setEditName(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        void commitEdit(tag)
                      } else if (e.key === 'Escape') {
                        setEditingId(null)
                      }
                    }}
                    className="min-w-0 flex-1 rounded border border-gray-300 px-2 py-1 text-xs outline-none focus:border-gray-500"
                  />
                </div>

                <div className="mt-2 flex items-center justify-between gap-2">
                  <ColorPicker value={editColor} disabled={pending} onChange={setEditColor} />

                  <div className="flex gap-1">
                    <button
                      type="button"
                      disabled={pending || !editName.trim()}
                      onClick={() => void commitEdit(tag)}
                      className="rounded border border-gray-300 px-2 py-0.5 text-[11px] text-gray-700 transition hover:bg-gray-50 disabled:opacity-40"
                    >
                      保存
                    </button>
                    <button
                      type="button"
                      onClick={() => setEditingId(null)}
                      className="rounded px-2 py-0.5 text-[11px] text-gray-500 transition hover:bg-gray-100"
                    >
                      取消
                    </button>
                  </div>
                </div>
              </li>
            ) : (
              <li
                key={tag.id}
                className="group flex items-center gap-1.5 rounded-lg px-1.5 py-1 hover:bg-gray-50"
              >
                <TagDot color={tag.color} />

                <span className="min-w-0 flex-1 truncate text-xs text-gray-700">{tag.name}</span>

                <button
                  type="button"
                  title="编辑标签"
                  disabled={pending}
                  onClick={() => startEdit(tag)}
                  className="shrink-0 rounded px-1 text-xs text-gray-400 opacity-0 transition group-hover:opacity-100 hover:text-gray-700 disabled:opacity-30"
                >
                  ✎
                </button>

                <button
                  type="button"
                  title="删除标签"
                  disabled={pending}
                  onClick={() => void onRemove(tag.id)}
                  className="shrink-0 rounded px-1 text-xs text-gray-400 opacity-0 transition group-hover:opacity-100 hover:text-red-600 disabled:opacity-30"
                >
                  ×
                </button>
              </li>
            ),
          )}
        </ul>
      )}

      <div className="space-y-2 border-t border-gray-100 pt-3">
        <div className="flex gap-1">
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                void handleCreate()
              }
            }}
            placeholder="新建标签"
            maxLength={50}
            className="min-w-0 flex-1 rounded-lg border border-gray-300 px-2 py-1.5 text-sm outline-none focus:border-gray-500"
          />

          <button
            type="button"
            disabled={pending || !name.trim()}
            onClick={() => void handleCreate()}
            className="rounded-lg border border-gray-300 px-2.5 py-1.5 text-sm text-gray-700 transition hover:bg-gray-50 disabled:opacity-40"
          >
            添加
          </button>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-[11px] text-gray-400">颜色</span>
          <ColorPicker value={color} disabled={pending} onChange={setColor} />
        </div>
      </div>
    </section>
  )
}
