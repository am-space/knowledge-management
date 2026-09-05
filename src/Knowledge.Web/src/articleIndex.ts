const storageKey = 'knowledge.localArticleIds'

export function loadArticleIds(): string[] {
  try {
    const value: unknown = JSON.parse(localStorage.getItem(storageKey) ?? '[]')
    return Array.isArray(value) ? value.filter((id): id is string => typeof id === 'string') : []
  } catch { return [] }
}

export function rememberArticleId(id: string): boolean {
  return persistArticleIds([id, ...loadArticleIds().filter((item) => item !== id)])
}

export function forgetArticleId(id: string): boolean {
  return persistArticleIds(loadArticleIds().filter((item) => item !== id))
}

function persistArticleIds(ids: string[]): boolean {
  try {
    localStorage.setItem(storageKey, JSON.stringify(ids))
    return true
  } catch { return false }
}
