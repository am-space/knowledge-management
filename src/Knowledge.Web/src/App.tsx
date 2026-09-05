import AddOutlined from '@mui/icons-material/AddOutlined'
import ArticleOutlined from '@mui/icons-material/ArticleOutlined'
import CloudDoneOutlined from '@mui/icons-material/CloudDoneOutlined'
import CloudOffOutlined from '@mui/icons-material/CloudOffOutlined'
import RefreshOutlined from '@mui/icons-material/RefreshOutlined'
import SaveOutlined from '@mui/icons-material/SaveOutlined'
import { Alert, AppBar, Box, Button, Chip, CircularProgress, CssBaseline, Divider, List, ListItemButton, ListItemIcon, ListItemText, Paper, Stack, Tab, Tabs, TextField, ThemeProvider, Toolbar, Typography, createTheme, useMediaQuery } from '@mui/material'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import { ArticleApiError, createArticle, getArticle, updateArticle, type Article } from './articleClient'
import { forgetArticleId, loadArticleIds, rememberArticleId } from './articleIndex'
import { getReadiness } from './healthClient'

type ConnectionState = 'checking' | 'connected' | 'unavailable'
type EditorMode = 'source' | 'preview'
interface Draft { title: string; contentMarkdown: string }
const emptyDraft: Draft = { title: '', contentMarkdown: '' }
const treeConcurrency = 4
const treeRequestTimeoutMs = 15_000

export default function App() {
  const prefersDarkMode = useMediaQuery('(prefers-color-scheme: dark)')
  const theme = useMemo(() => createTheme({ palette: { mode: prefersDarkMode ? 'dark' : 'light' }, typography: { fontFamily: 'Inter, ui-sans-serif, system-ui, sans-serif' }, shape: { borderRadius: 10 } }), [prefersDarkMode])
  const [connection, setConnection] = useState<ConnectionState>('checking')
  const [articles, setArticles] = useState<Article[]>([])
  const [treeRequestIds, setTreeRequestIds] = useState(loadArticleIds)
  const [remainingTreeRequests, setRemainingTreeRequests] = useState(treeRequestIds.length)
  const loadingTree = remainingTreeRequests > 0
  const [treeFailures, setTreeFailures] = useState<{ id: string; error: ArticleApiError }[]>([])
  const [selected, setSelected] = useState<Article | null>(null)
  const [draft, setDraft] = useState<Draft>(emptyDraft)
  const [creating, setCreating] = useState(false)
  const [saving, setSaving] = useState(false)
  const [openingId, setOpeningId] = useState<string | null>(null)
  const [error, setError] = useState<ArticleApiError | null>(null)
  const [status, setStatus] = useState('')
  const navigationRequest = useRef(0)
  const titleInput = useRef<HTMLInputElement>(null)
  const [createFocusRequest, setCreateFocusRequest] = useState(0)
  const [indexWarning, setIndexWarning] = useState(false)
  const [mode, setMode] = useState<EditorMode>('source')

  const replaceArticle = useCallback((article: Article) => {
    setArticles((current) => [article, ...current.filter((item) => item.id !== article.id)])
    if (!rememberArticleId(article.id)) setIndexWarning(true)
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    getReadiness(controller.signal).then(() => setConnection('connected')).catch((requestError: unknown) => {
      if (!(requestError instanceof DOMException && requestError.name === 'AbortError')) setConnection('unavailable')
    })
    return () => controller.abort()
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    let nextIndex = 0
    const loadNext = async () => {
      while (!controller.signal.aborted && nextIndex < treeRequestIds.length) {
        const id = treeRequestIds[nextIndex++]
        const requestController = new AbortController()
        const abortRequest = () => requestController.abort()
        controller.signal.addEventListener('abort', abortRequest, { once: true })
        const timeout = window.setTimeout(abortRequest, treeRequestTimeoutMs)
        try {
          const article = await getArticle(id, requestController.signal)
          if (controller.signal.aborted) return
          // An open or save may already have supplied a newer representation.
          setArticles((current) => current.some((item) => item.id === id) ? current : [...current, article])
          setTreeFailures((current) => current.filter((failure) => failure.id !== id))
        } catch (requestError) {
          if (controller.signal.aborted) return
          const apiError = requestController.signal.aborted
            ? new ArticleApiError('unreachable', 'Loading this article timed out. Please retry.')
            : normalizeError(requestError)
          if (apiError.kind === 'not-found') {
            if (!forgetArticleId(id)) setIndexWarning(true)
            setTreeFailures((current) => current.filter((failure) => failure.id !== id))
          } else setTreeFailures((current) => [...current.filter((failure) => failure.id !== id), { id, error: apiError }])
        } finally {
          window.clearTimeout(timeout)
          controller.signal.removeEventListener('abort', abortRequest)
          if (!controller.signal.aborted) setRemainingTreeRequests((remaining) => remaining - 1)
        }
      }
    }
    for (let worker = 0; worker < Math.min(treeConcurrency, treeRequestIds.length); worker += 1) void loadNext()
    return () => controller.abort()
  }, [treeRequestIds])

  useEffect(() => {
    if (createFocusRequest > 0) titleInput.current?.focus()
  }, [createFocusRequest])

  const confirmDiscard = () => {
    const hasUnsavedChanges = creating
      ? draft.title !== '' || draft.contentMarkdown !== ''
      : selected !== null && (draft.title !== selected.currentRevision.title || draft.contentMarkdown !== selected.currentRevision.contentMarkdown)
    return !hasUnsavedChanges || window.confirm('Discard unsaved changes? Cancel to keep editing and save your work.')
  }

  const beginCreate = () => {
    if (saving || !confirmDiscard()) return
    navigationRequest.current += 1
    setCreateFocusRequest((current) => current + 1)
    setOpeningId(null)
    setSelected(null); setDraft(emptyDraft); setCreating(true); setError(null); setStatus('New article draft'); setMode('source')
  }

  const openArticle = async (id: string) => {
    if (saving || !confirmDiscard()) return
    const request = ++navigationRequest.current
    setOpeningId(id); setError(null)
    try {
      const article = await getArticle(id)
      if (request !== navigationRequest.current) return
      replaceArticle(article); setSelected(article)
      setDraft({ title: article.currentRevision.title, contentMarkdown: article.currentRevision.contentMarkdown })
      setCreating(false); setStatus(`Opened revision ${article.currentRevision.version}`)
    } catch (requestError) {
      if (request !== navigationRequest.current) return
      const apiError = normalizeError(requestError); setError(apiError)
      if (apiError.kind === 'not-found') {
        if (!forgetArticleId(id)) setIndexWarning(true)
        setArticles((current) => current.filter((article) => article.id !== id))
        if (selected?.id === id) setSelected(null)
      }
    } finally { if (request === navigationRequest.current) setOpeningId(null) }
  }

  const save = async () => {
    if (saving || openingId !== null || (!creating && selected === null)) return
    setSaving(true); setError(null); setStatus('Saving…')
    try {
      const article = creating
        ? await createArticle(draft.title, draft.contentMarkdown)
        : await updateArticle(selected!.id, selected!.currentRevision.version, draft.title, draft.contentMarkdown)
      replaceArticle(article); setSelected(article)
      setDraft({ title: article.currentRevision.title, contentMarkdown: article.currentRevision.contentMarkdown })
      setCreating(false); setStatus(`Saved revision ${article.currentRevision.version}`)
    } catch (requestError) {
      setError(normalizeError(requestError)); setStatus('Save failed; your draft is preserved')
    } finally { setSaving(false) }
  }

  const hasEditor = creating || selected !== null
  const changed = creating || (selected !== null && (draft.title !== selected.currentRevision.title || draft.contentMarkdown !== selected.currentRevision.contentMarkdown))

  return <ThemeProvider theme={theme}>
    <CssBaseline />
    <AppBar position="static" color="transparent" elevation={0} sx={{ borderBottom: 1, borderColor: 'divider' }}>
      <Toolbar><Typography variant="h6" component="h1" sx={{ flexGrow: 1 }}>Knowledge</Typography>
        <Chip icon={connection === 'connected' ? <CloudDoneOutlined /> : <CloudOffOutlined />} color={connection === 'connected' ? 'success' : connection === 'checking' ? 'default' : 'error'} label={connection === 'checking' ? 'Checking server' : connection === 'connected' ? 'Local workspace ready' : 'Server unavailable'} variant="outlined" />
      </Toolbar>
    </AppBar>
    <Box component="main" sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '280px minmax(0, 1fr)' }, minHeight: 'calc(100vh - 65px)' }}>
      <Paper component="nav" aria-label="Knowledge tree" square elevation={0} sx={{ borderRight: { md: 1 }, borderBottom: { xs: 1, md: 0 }, borderColor: 'divider', p: 2 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1}><Box><Typography variant="overline" color="text.secondary">Personal workspace</Typography><Typography variant="h6">Articles</Typography></Box><Button startIcon={<AddOutlined />} onClick={beginCreate} disabled={saving}>New</Button></Stack>
        <Divider sx={{ my: 2 }} />
        {treeFailures.length > 0 && <Alert severity="error" sx={{ mb: 2 }} action={<Button color="inherit" disabled={loadingTree} onClick={() => {
          setRemainingTreeRequests(treeFailures.length)
          setTreeRequestIds(treeFailures.map((failure) => failure.id))
        }}>Retry loading articles</Button>}>Could not load {treeFailures.length} indexed {treeFailures.length === 1 ? 'article' : 'articles'}. {errorMessage(treeFailures[0].error)}</Alert>}
        {loadingTree && <Stack direction="row" spacing={1} alignItems="center"><CircularProgress size={18} /><Typography>Loading articles… ({remainingTreeRequests} remaining)</Typography></Stack>}
        {!loadingTree && treeFailures.length === 0 && articles.length === 0 && <Typography color="text.secondary">No articles yet. Create your first article.</Typography>}
        <List disablePadding>{articles.map((article) => <ListItemButton key={article.id} selected={selected?.id === article.id && !creating} onClick={() => void openArticle(article.id)} disabled={saving || openingId === article.id}><ListItemIcon><ArticleOutlined /></ListItemIcon><ListItemText primary={article.currentRevision.title} secondary={`Revision ${article.currentRevision.version}`} /></ListItemButton>)}</List>
      </Paper>
      <Box sx={{ p: { xs: 2, sm: 3, lg: 5 }, minWidth: 0 }}>
        {indexWarning && <Alert severity="warning" sx={{ mb: 2 }}>The browser could not remember changes to your article list. Server saves are unaffected, but the list may be incomplete after reloading.</Alert>}
        {!hasEditor ? <Stack alignItems="center" justifyContent="center" sx={{ minHeight: 360, textAlign: 'center' }} spacing={2}>{error && <Alert severity="error">{errorMessage(error)}</Alert>}<ArticleOutlined color="disabled" sx={{ fontSize: 64 }} /><Typography variant="h4" component="h2">Select an article</Typography><Typography color="text.secondary">Choose one from the tree or create a new Markdown article.</Typography><Button variant="contained" startIcon={<AddOutlined />} onClick={beginCreate} disabled={saving}>Create article</Button></Stack>
          : <Stack spacing={2} component="form" onSubmit={(event) => { event.preventDefault(); void save() }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2}><Box><Typography variant="overline" color="primary">{creating ? 'New article' : `Revision ${selected!.currentRevision.version}`}</Typography><Typography variant="h4" component="h2">{creating ? 'Create knowledge' : 'Edit article'}</Typography></Box><Button type="submit" variant="contained" startIcon={saving ? <CircularProgress size={18} color="inherit" /> : <SaveOutlined />} disabled={saving || openingId !== null || !changed}>{saving ? 'Saving' : creating ? 'Create' : 'Save'}</Button></Stack>
            {error && <Alert severity="error" action={error.kind === 'conflict' ? <Button color="inherit" startIcon={<RefreshOutlined />} onClick={() => selected && void openArticle(selected.id)} disabled={saving || openingId !== null}>Reload server version</Button> : undefined}>{errorMessage(error)}</Alert>}
            <TextField disabled={saving || openingId !== null} label="Title" inputRef={titleInput} value={draft.title} required inputProps={{ maxLength: 500 }} error={Boolean(error?.errors.title)} helperText={error?.errors.title?.join(' ')} onChange={(event) => setDraft((current) => ({ ...current, title: event.target.value }))} />
            <Box><Tabs value={mode} onChange={(_, value: EditorMode) => setMode(value)} aria-label="Article content view"><Tab value="source" label="Markdown source" /><Tab value="preview" label="Preview" /></Tabs>
              {mode === 'source' ? <TextField disabled={saving || openingId !== null} label="Markdown source" value={draft.contentMarkdown} multiline minRows={16} fullWidth error={Boolean(error?.errors.contentMarkdown)} helperText={error?.errors.contentMarkdown?.join(' ')} onChange={(event) => setDraft((current) => ({ ...current, contentMarkdown: event.target.value }))} sx={{ mt: 2 }} inputProps={{ style: { fontFamily: 'ui-monospace, SFMono-Regular, Consolas, monospace' } }} />
                : <Paper variant="outlined" aria-label="Markdown preview" sx={{ mt: 2, p: 3, minHeight: 400, overflowWrap: 'anywhere', '& pre': { overflowX: 'auto' }, '& img': { maxWidth: '100%' } }}>{draft.contentMarkdown ? <ReactMarkdown components={{ a: ({ href, title, children }) => <a href={href} title={title} target="_blank" rel="noopener noreferrer">{children}</a> }}>{draft.contentMarkdown}</ReactMarkdown> : <Typography color="text.secondary">Nothing to preview yet.</Typography>}</Paper>}
            </Box>
          </Stack>}
      </Box>
    </Box>
    <Box component="footer" aria-live="polite" sx={{ position: 'sticky', bottom: 0, px: 2, py: 1, borderTop: 1, borderColor: 'divider', bgcolor: 'background.paper' }}><Typography variant="body2" color="text.secondary">{status || (connection === 'unavailable' ? 'The server could not be reached. Start it and retry.' : 'Ready')}</Typography></Box>
  </ThemeProvider>
}

function normalizeError(error: unknown): ArticleApiError {
  return error instanceof ArticleApiError ? error : new ArticleApiError('server', 'Something went wrong. Please try again.')
}

function errorMessage(error: ArticleApiError): string {
  if (error.kind === 'conflict') return `This article changed on the server${error.currentRevisionVersion ? ` (now revision ${error.currentRevisionVersion})` : ''}. Your draft is preserved. Reload the server version when you are ready.`
  if (error.kind === 'not-found') return 'This article was not found. It may have been removed.'
  if (error.kind === 'validation') return 'Review the highlighted fields and try again.'
  if (error.kind === 'forbidden') return 'The local workspace is unavailable. Check the server configuration.'
  return error.message
}
