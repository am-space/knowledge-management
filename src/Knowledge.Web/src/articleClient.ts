export interface ArticleRevision {
  id: string
  version: number
  title: string
  contentMarkdown: string
  createdAt: string
  createdBy: string
}

export interface Article {
  id: string
  type: 'article'
  createdAt: string
  createdBy: string
  currentRevision: ArticleRevision
}

interface ProblemDetails {
  title?: string
  errors?: Record<string, string[]>
  currentRevisionVersion?: number
}

export class ArticleApiError extends Error {
  constructor(
    readonly kind: 'validation' | 'not-found' | 'conflict' | 'forbidden' | 'server' | 'unreachable',
    message: string,
    readonly errors: Record<string, string[]> = {},
    readonly currentRevisionVersion?: number,
  ) {
    super(message)
  }
}

async function send(url: string, init?: RequestInit): Promise<Article> {
  let response: Response
  try {
    response = await fetch(url, { ...init, headers: { Accept: 'application/json', ...init?.headers } })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ArticleApiError('unreachable', 'The server could not be reached. Check that it is running and try again.')
  }

  if (response.ok) return (await response.json()) as Article

  let problem: ProblemDetails = {}
  try { problem = (await response.json()) as ProblemDetails } catch { /* Use the status fallback. */ }
  const kind = response.status === 400 ? 'validation' : response.status === 404 ? 'not-found'
    : response.status === 409 ? 'conflict' : response.status === 403 ? 'forbidden' : 'server'
  throw new ArticleApiError(kind, problem.title ?? `The article request failed (${response.status}).`, problem.errors, problem.currentRevisionVersion)
}

export function createArticle(title: string, contentMarkdown: string, signal?: AbortSignal) {
  return send('/api/articles', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ title, contentMarkdown }), signal })
}

export function getArticle(id: string, signal?: AbortSignal) {
  return send(`/api/articles/${id}`, { signal })
}

export function updateArticle(id: string, expectedRevisionVersion: number, title: string, contentMarkdown: string, signal?: AbortSignal) {
  return send(`/api/articles/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ expectedRevisionVersion, title, contentMarkdown }), signal })
}
