import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from './App'

const articleId = '8a73e7fc-58e8-463b-9f3d-d2d641380adb'

function article(version: number, title = 'First article', contentMarkdown = '# First article\n') {
  return {
    id: articleId,
    type: 'article',
    createdAt: '2026-08-31T17:00:00Z',
    createdBy: '50c68ff7-a599-4bf8-849b-775c84919f9a',
    currentRevision: {
      id: `c28bcfb5-0b81-4f69-9f88-206af785118${version}`,
      version,
      title,
      contentMarkdown,
      createdAt: '2026-08-31T17:00:00Z',
      createdBy: '50c68ff7-a599-4bf8-849b-775c84919f9a',
    },
  }
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

beforeEach(() => localStorage.clear())
afterEach(() => { cleanup(); vi.restoreAllMocks() })

test('creates, previews, and saves exact Markdown into the local tree', async () => {
  const markdown = '# Heading\n\n- one  \n- two\n'
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    if (String(input) === '/api/articles' && init?.method === 'POST') return jsonResponse(article(1, 'Precise notes', markdown), 201)
    throw new Error(`Unexpected request: ${String(input)}`)
  })

  render(<App />)
  fireEvent.click(screen.getByRole('button', { name: 'Create article' }))
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: 'Precise notes' } })
  fireEvent.change(screen.getByLabelText(/^Markdown source/), { target: { value: markdown } })
  fireEvent.click(screen.getByRole('tab', { name: 'Preview' }))

  expect(screen.getByRole('heading', { name: 'Heading' })).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: 'Create' }))

  expect(await screen.findByText('Saved revision 1')).toBeInTheDocument()
  expect(screen.getByText('Precise notes')).toBeInTheDocument()
  const createCall = fetchMock.mock.calls.find(([url]) => String(url) === '/api/articles')
  expect(JSON.parse(String(createCall?.[1]?.body))).toEqual({ title: 'Precise notes', contentMarkdown: markdown })
  expect(localStorage.getItem('knowledge.localArticleIds')).toContain(articleId)
})

test('loads the local tree, reopens an article, and updates with its latest version', async () => {
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId]))
  const updated = article(4, 'Updated title', 'line one\nline two\n')
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    if (String(input) === `/api/articles/${articleId}` && init?.method === 'PUT') return jsonResponse(updated)
    if (String(input) === `/api/articles/${articleId}`) return jsonResponse(article(3))
    throw new Error(`Unexpected request: ${String(input)}`)
  })

  render(<App />)
  fireEvent.click(await screen.findByText('First article'))
  await waitFor(() => expect(screen.getByLabelText(/^Markdown source/)).toHaveValue('# First article\n'))
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: 'Updated title' } })
  fireEvent.change(screen.getByLabelText(/^Markdown source/), { target: { value: 'line one\nline two\n' } })
  fireEvent.click(screen.getByRole('button', { name: 'Save' }))

  expect(await screen.findByText('Saved revision 4')).toBeInTheDocument()
  const updateCall = fetchMock.mock.calls.find(([, init]) => init?.method === 'PUT')
  expect(JSON.parse(String(updateCall?.[1]?.body))).toEqual({ expectedRevisionVersion: 3, title: 'Updated title', contentMarkdown: 'line one\nline two\n' })
})

test('preserves a conflicting draft and reloads current server content on request', async () => {
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId]))
  let getCount = 0
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    if (init?.method === 'PUT') return jsonResponse({ title: 'Revision conflict', currentRevisionVersion: 2 }, 409)
    if (String(input) === `/api/articles/${articleId}`) {
      getCount += 1
      return jsonResponse(getCount <= 2 ? article(1) : article(2, 'Server title', '# Server\n'))
    }
    throw new Error(`Unexpected request: ${String(input)}`)
  })

  render(<App />)
  fireEvent.click(await screen.findByText('First article'))
  await waitFor(() => expect(screen.getByLabelText(/^Markdown source/)).toHaveValue('# First article\n'))
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: 'My draft' } })
  fireEvent.click(screen.getByRole('button', { name: 'Save' }))

  expect(await screen.findByText(/now revision 2/)).toBeInTheDocument()
  expect(screen.getByDisplayValue('My draft')).toBeInTheDocument()
  const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true)
  fireEvent.click(screen.getByRole('button', { name: 'Reload server version' }))
  expect(confirm).toHaveBeenCalledOnce()
  expect(await screen.findByDisplayValue('Server title')).toBeInTheDocument()
  expect(screen.getByLabelText(/^Markdown source/)).toHaveValue('# Server\n')
})

test('shows validation and unreachable-server errors accessibly', async () => {
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    if (String(input) === '/health/ready') throw new TypeError('offline')
    if (init?.method === 'POST') return jsonResponse({ title: 'Validation failed', errors: { title: ['Title is required.'] } }, 400)
    throw new Error(`Unexpected request: ${String(input)}`)
  })

  render(<App />)
  expect(await screen.findByText('Server unavailable')).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: 'Create article' }))
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: '   ' } })
  fireEvent.change(screen.getByLabelText(/^Markdown source/), { target: { value: '# Draft' } })
  fireEvent.click(screen.getByRole('button', { name: 'Create' }))

  expect(await screen.findByText('Title is required.')).toBeInTheDocument()
  expect(screen.getByRole('alert')).toHaveTextContent('Review the highlighted fields')
})

test('removes an indexed article when reopening returns not found', async () => {
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId]))
  let getCount = 0
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    if (String(input) === `/api/articles/${articleId}`) {
      getCount += 1
      return getCount === 1 ? jsonResponse(article(1)) : jsonResponse({ title: 'Article not found' }, 404)
    }
    throw new Error(`Unexpected request: ${String(input)}`)
  })

  render(<App />)
  fireEvent.click(await screen.findByText('First article'))
  expect(await screen.findByRole('alert')).toHaveTextContent('not found')
  await waitFor(() => expect(localStorage.getItem('knowledge.localArticleIds')).toBe('[]'))
})

function deferredResponse() {
  let resolve!: (response: Response) => void
  const promise = new Promise<Response>((resolvePromise) => { resolve = resolvePromise })
  return { promise, resolve }
}

test.each(['create', 'update'])('locks editing and navigation during a pending %s', async (operation) => {
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId]))
  const pending = deferredResponse()
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    if (init?.method === 'POST' || init?.method === 'PUT') return pending.promise
    return jsonResponse(article(1))
  })

  render(<App />)
  const treeEntry = await screen.findByRole('button', { name: 'First article Revision 1' })
  if (operation === 'create') fireEvent.click(screen.getByRole('button', { name: 'New' }))
  else {
    fireEvent.click(treeEntry)
    await screen.findByDisplayValue('First article')
  }
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: 'Submitted title' } })
  fireEvent.click(screen.getByRole('button', { name: operation === 'create' ? 'Create' : 'Save' }))

  expect(screen.getByLabelText(/^Title/)).toBeDisabled()
  expect(screen.getByLabelText(/^Markdown source/)).toBeDisabled()
  expect(screen.getByRole('button', { name: 'New' })).toBeDisabled()
  expect(treeEntry).toHaveAttribute('aria-disabled', 'true')
  expect(screen.getByRole('button', { name: 'Saving' })).toBeDisabled()
  fireEvent.click(screen.getByRole('button', { name: 'New' }))
  fireEvent.click(treeEntry)
  expect(fetchMock.mock.calls.filter(([, init]) => init?.method)).toHaveLength(1)

  await act(async () => pending.resolve(jsonResponse(article(2, 'Submitted title', ''))))
  expect(screen.getByText('Saved revision 2')).toBeInTheDocument()
  expect(screen.getByLabelText(/^Title/)).toHaveValue('Submitted title')
  expect(screen.getByLabelText(/^Title/)).toBeEnabled()
  expect(screen.getByLabelText(/^Markdown source/)).toBeEnabled()
  expect(screen.getByRole('button', { name: 'New' })).toBeEnabled()
})

test.each([200, 404, 500])('ignores a superseded open response (%s) after starting a new draft', async (status) => {
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId]))
  const pending = deferredResponse()
  let reads = 0
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    return ++reads === 1 ? jsonResponse(article(1)) : pending.promise
  })

  render(<App />)
  fireEvent.click(await screen.findByText('First article'))
  fireEvent.click(screen.getByRole('button', { name: 'New' }))
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: 'Newer draft' } })
  fireEvent.change(screen.getByLabelText(/^Markdown source/), { target: { value: 'Keep this body' } })
  await act(async () => pending.resolve(jsonResponse(status === 200 ? article(1) : {}, status)))

  expect(screen.getByLabelText(/^Title/)).toHaveValue('Newer draft')
  expect(screen.getByLabelText(/^Markdown source/)).toHaveValue('Keep this body')
  expect(screen.getByText('New article draft')).toBeInTheDocument()
  expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  expect(screen.getByText('First article')).toBeInTheDocument()
})

test.each(['older-first', 'newer-first'])('keeps the latest article selection when opens finish %s', async (order) => {
  const second = { ...article(3, 'Second article', 'Second body'), id: 'second-id' }
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId, second.id]))
  const firstOpen = deferredResponse()
  const secondOpen = deferredResponse()
  const reads = new Map<string, number>()
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    const url = String(input)
    if (url === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    reads.set(url, (reads.get(url) ?? 0) + 1)
    if (reads.get(url) === 1) return jsonResponse(url.endsWith(articleId) ? article(1) : second)
    return url.endsWith(articleId) ? firstOpen.promise : secondOpen.promise
  })

  render(<App />)
  fireEvent.click(await screen.findByText('First article'))
  fireEvent.click(screen.getByText('Second article'))
  if (order === 'older-first') {
    await act(async () => firstOpen.resolve(jsonResponse(article(1))))
    expect(screen.queryByLabelText(/^Title/)).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Second article Revision 3' })).toHaveAttribute('aria-disabled', 'true')
    await act(async () => secondOpen.resolve(jsonResponse(second)))
  } else {
    await act(async () => secondOpen.resolve(jsonResponse(second)))
    await act(async () => firstOpen.resolve(jsonResponse(article(1))))
  }
  expect(screen.getByLabelText(/^Title/)).toHaveValue('Second article')
  expect(screen.getByLabelText(/^Markdown source/)).toHaveValue('Second body')
  expect(screen.getByText('Opened revision 3')).toBeInTheDocument()
})

test('merges startup tree results with articles created while loading', async () => {
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify(['startup-id']))
  const startup = deferredResponse()
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    if (init?.method === 'POST') return jsonResponse(article(1, 'Created during loading', ''), 201)
    return startup.promise
  })

  render(<App />)
  fireEvent.click(screen.getByRole('button', { name: 'New' }))
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: 'Created during loading' } })
  fireEvent.click(screen.getByRole('button', { name: 'Create' }))
  await screen.findByText('Saved revision 1')
  await act(async () => startup.resolve(jsonResponse({ ...article(1, 'Startup article'), id: 'startup-id' })))

  expect(screen.getByRole('button', { name: 'Startup article Revision 1' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Created during loading Revision 1' })).toBeInTheDocument()
})

test.each(['QuotaExceededError', 'SecurityError'])('adopts successful creates and updates when storage throws %s', async (errorName) => {
  vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new DOMException('Storage unavailable', errorName) })
  let version = 0
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    const body = JSON.parse(String(init?.body)) as { title: string; contentMarkdown: string }
    return jsonResponse(article(++version, body.title, body.contentMarkdown))
  })

  render(<App />)
  fireEvent.click(screen.getByRole('button', { name: 'New' }))
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: 'Saved despite storage' } })
  fireEvent.click(screen.getByRole('button', { name: 'Create' }))
  await screen.findByText('Saved revision 1')
  expect(screen.getByRole('alert')).toHaveTextContent('browser could not remember')
  for (const revision of [2, 3]) {
    fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: `Edit ${revision}` } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))
    await screen.findByText(`Saved revision ${revision}`)
  }

  const writes = fetchMock.mock.calls.filter(([, init]) => init?.method)
  expect(writes.map(([, init]) => init?.method)).toEqual(['POST', 'PUT', 'PUT'])
  expect(writes.slice(1).map(([url, init]) => [url, JSON.parse(String(init?.body)).expectedRevisionVersion])).toEqual([
    [`/api/articles/${articleId}`, 1], [`/api/articles/${articleId}`, 2],
  ])
  expect(screen.getByRole('button', { name: 'Edit 3 Revision 3' })).toBeInTheDocument()
  expect(screen.queryByText(/Save failed/)).not.toBeInTheDocument()
})

test('finishes loading when removing a missing index entry cannot be persisted', async () => {
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId]))
  vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new DOMException('Quota exceeded', 'QuotaExceededError') })
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => String(input) === '/health/ready'
    ? jsonResponse({ status: 'Healthy', checks: [] }) : jsonResponse({}, 404))

  render(<App />)
  expect(await screen.findByText('No articles yet. Create your first article.')).toBeInTheDocument()
  expect(screen.getByRole('alert')).toHaveTextContent('browser could not remember')
})

test.each(['source', 'preview'])('creates empty Markdown and saves a cleared body in %s mode', async (mode) => {
  let version = 0
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    const body = JSON.parse(String(init?.body)) as { title: string; contentMarkdown: string }
    return jsonResponse(article(++version, body.title, body.contentMarkdown))
  })

  render(<App />)
  fireEvent.click(screen.getByRole('button', { name: 'New' }))
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: 'Empty article' } })
  if (mode === 'preview') fireEvent.click(screen.getByRole('tab', { name: 'Preview' }))
  fireEvent.click(screen.getByRole('button', { name: 'Create' }))
  await screen.findByText('Saved revision 1')
  fireEvent.click(screen.getByRole('tab', { name: 'Markdown source' }))
  fireEvent.change(screen.getByLabelText(/^Markdown source/), { target: { value: 'Temporary body' } })
  fireEvent.click(screen.getByRole('button', { name: 'Save' }))
  await screen.findByText('Saved revision 2')
  fireEvent.change(screen.getByLabelText(/^Markdown source/), { target: { value: '' } })
  if (mode === 'preview') fireEvent.click(screen.getByRole('tab', { name: 'Preview' }))
  fireEvent.click(screen.getByRole('button', { name: 'Save' }))
  await screen.findByText('Saved revision 3')

  const writes = fetchMock.mock.calls.filter(([, init]) => init?.method)
  expect(writes.map(([, init]) => JSON.parse(String(init?.body)).contentMarkdown)).toEqual(['', 'Temporary body', ''])
})

test.each([
  ['existing', 'same'],
  ['existing', 'other'],
  ['existing', 'new'],
  ['new', 'other'],
  ['new', 'new'],
])('confirms before discarding an %s draft when navigating to %s', async (draftKind, destination) => {
  const second = { ...article(2, 'Second article', 'Second body'), id: 'second-id' }
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId, second.id]))
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    return jsonResponse(String(input).endsWith(articleId) ? article(1) : second)
  })
  const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false)

  render(<App />)
  await screen.findByText('First article')
  if (draftKind === 'new') fireEvent.click(screen.getByRole('button', { name: 'New' }))
  else {
    fireEvent.click(screen.getByText('First article'))
    await screen.findByDisplayValue('First article')
  }
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: 'Unsaved title' } })
  fireEvent.change(screen.getByLabelText(/^Markdown source/), { target: { value: 'Unsaved body' } })
  const navigate = () => fireEvent.click(destination === 'new'
    ? screen.getByRole('button', { name: 'New' })
    : screen.getByText(destination === 'same' ? 'First article' : 'Second article'))
  const requestCount = fetchMock.mock.calls.length
  navigate()

  expect(confirm).toHaveBeenCalledOnce()
  expect(fetchMock).toHaveBeenCalledTimes(requestCount)
  expect(screen.getByLabelText(/^Title/)).toHaveValue('Unsaved title')
  expect(screen.getByLabelText(/^Markdown source/)).toHaveValue('Unsaved body')
  expect(screen.getByRole('button', { name: draftKind === 'new' ? 'Create' : 'Save' })).toBeEnabled()

  confirm.mockReturnValue(true)
  navigate()
  expect(confirm).toHaveBeenCalledTimes(2)
  if (destination === 'new') {
    expect(screen.getByLabelText(/^Title/)).toHaveValue('')
    expect(screen.getByLabelText(/^Markdown source/)).toHaveValue('')
  } else {
    await screen.findByDisplayValue(destination === 'same' ? 'First article' : 'Second article')
    expect(screen.getByLabelText(/^Markdown source/)).toHaveValue(destination === 'same' ? '# First article\n' : 'Second body')
  }
})

test('protects a cleared body and allows navigation without a prompt after saving', async () => {
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId]))
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    return jsonResponse(init?.method === 'PUT' ? article(2, 'First article', '') : article(1))
  })
  const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false)

  render(<App />)
  fireEvent.click(await screen.findByText('First article'))
  await screen.findByDisplayValue('First article')
  fireEvent.change(screen.getByLabelText(/^Markdown source/), { target: { value: '' } })
  fireEvent.click(screen.getByRole('button', { name: 'New' }))
  expect(confirm).toHaveBeenCalledOnce()
  expect(screen.getByLabelText(/^Title/)).toHaveValue('First article')
  expect(screen.getByLabelText(/^Markdown source/)).toHaveValue('')

  fireEvent.click(screen.getByRole('button', { name: 'Save' }))
  await screen.findByText('Saved revision 2')
  fireEvent.click(screen.getByRole('button', { name: 'New' }))
  expect(screen.getByLabelText(/^Title/)).toHaveValue('')
  fireEvent.click(screen.getByText('First article'))
  await screen.findByDisplayValue('First article')
  fireEvent.click(screen.getByRole('button', { name: 'New' }))
  expect(screen.getByLabelText(/^Title/)).toHaveValue('')
  expect(confirm).toHaveBeenCalledOnce()
})

test.each(['network', '403', '500'])('shows and retries a startup %s failure without losing indexed IDs', async (failure) => {
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId]))
  let recovered = false
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    if (String(input) === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    if (recovered) return jsonResponse(article(1))
    if (failure === 'network') throw new TypeError('offline')
    return jsonResponse({ title: 'Request failed' }, Number(failure))
  })

  render(<App />)
  expect(await screen.findByRole('alert')).toHaveTextContent('Could not load 1 indexed article')
  expect(screen.getByText('Local workspace ready')).toBeInTheDocument()
  expect(screen.queryByText('No articles yet. Create your first article.')).not.toBeInTheDocument()
  expect(JSON.parse(localStorage.getItem('knowledge.localArticleIds')!)).toEqual([articleId])

  fireEvent.click(screen.getByRole('button', { name: 'Retry loading articles' }))
  await waitFor(() => expect(screen.getByRole('button', { name: 'Retry loading articles' })).toBeEnabled())
  expect(screen.getByRole('alert')).toHaveTextContent('Could not load 1 indexed article')
  expect(JSON.parse(localStorage.getItem('knowledge.localArticleIds')!)).toEqual([articleId])

  recovered = true
  fireEvent.click(screen.getByRole('button', { name: 'Retry loading articles' }))
  expect(await screen.findByText('First article')).toBeInTheDocument()
  expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  expect(fetchMock.mock.calls.filter(([url]) => String(url) === `/api/articles/${articleId}`)).toHaveLength(3)
})

test('keeps loaded articles and a draft while retrying only failed entries in a partially loaded tree', async () => {
  const recovered = { ...article(2, 'Recovered article'), id: 'failed-id' }
  localStorage.setItem('knowledge.localArticleIds', JSON.stringify([articleId, recovered.id, 'missing-id']))
  const retry = deferredResponse()
  let failedReads = 0
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    const url = String(input)
    if (url === '/health/ready') return jsonResponse({ status: 'Healthy', checks: [] })
    if (url.endsWith('missing-id')) return jsonResponse({}, 404)
    if (url.endsWith(recovered.id)) return ++failedReads === 1 ? jsonResponse({}, 500) : retry.promise
    return jsonResponse(article(1))
  })

  render(<App />)
  fireEvent.click(await screen.findByText('First article'))
  await screen.findByDisplayValue('First article')
  fireEvent.change(screen.getByLabelText(/^Title/), { target: { value: 'Keep my draft' } })
  expect(screen.getByRole('alert')).toHaveTextContent('Could not load 1 indexed article')
  expect(JSON.parse(localStorage.getItem('knowledge.localArticleIds')!)).toEqual([articleId, recovered.id])
  const requestsBeforeRetry = fetchMock.mock.calls.length
  fireEvent.click(screen.getByRole('button', { name: 'Retry loading articles' }))

  expect(screen.getByRole('button', { name: 'Retry loading articles' })).toBeDisabled()
  expect(screen.getByRole('button', { name: 'First article Revision 1' })).toBeInTheDocument()
  expect(screen.getByLabelText(/^Title/)).toHaveValue('Keep my draft')
  expect(screen.getByLabelText(/^Title/)).toBeEnabled()
  await act(async () => retry.resolve(jsonResponse(recovered)))

  expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'First article Revision 1' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Recovered article Revision 2' })).toBeInTheDocument()
  expect(screen.getByLabelText(/^Title/)).toHaveValue('Keep my draft')
  expect(fetchMock.mock.calls.slice(requestsBeforeRetry).map(([url]) => url)).toEqual(['/api/articles/failed-id'])
})
