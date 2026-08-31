import CloudDoneOutlined from '@mui/icons-material/CloudDoneOutlined'
import CloudOffOutlined from '@mui/icons-material/CloudOffOutlined'
import {
  AppBar,
  Box,
  Chip,
  Container,
  CssBaseline,
  Paper,
  Stack,
  ThemeProvider,
  Toolbar,
  Typography,
  createTheme,
  useMediaQuery,
} from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { getReadiness } from './healthClient'

type ConnectionState = 'checking' | 'connected' | 'unavailable'

export default function App() {
  const prefersDarkMode = useMediaQuery('(prefers-color-scheme: dark)')
  const theme = useMemo(
    () => createTheme({ palette: { mode: prefersDarkMode ? 'dark' : 'light' } }),
    [prefersDarkMode],
  )
  const [connection, setConnection] = useState<ConnectionState>('checking')

  useEffect(() => {
    const controller = new AbortController()
    getReadiness(controller.signal)
      .then(() => setConnection('connected'))
      .catch(() => setConnection('unavailable'))
    return () => controller.abort()
  }, [])

  const connected = connection === 'connected'

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <AppBar position="static" color="transparent" elevation={0}>
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            Knowledge
          </Typography>
          <Chip
            icon={connected ? <CloudDoneOutlined /> : <CloudOffOutlined />}
            color={connected ? 'success' : connection === 'checking' ? 'default' : 'error'}
            label={connection === 'checking' ? 'Checking server' : connected ? 'Server ready' : 'Server unavailable'}
            variant="outlined"
          />
        </Toolbar>
      </AppBar>
      <Container maxWidth="md" sx={{ py: 8 }}>
        <Paper variant="outlined" sx={{ p: { xs: 3, sm: 6 } }}>
          <Stack spacing={2}>
            <Typography variant="overline" color="primary">
              Executable foundation
            </Typography>
            <Typography variant="h3" component="h1">
              Your knowledge, ready to grow.
            </Typography>
            <Typography color="text.secondary" sx={{ maxWidth: 620 }}>
              The application shell is connected. Knowledge workflows arrive in the next milestone.
            </Typography>
            <Box aria-live="polite" sx={{ pt: 2 }}>
              <Typography variant="body2">
                {connected
                  ? 'The server and configured persistence profile are ready.'
                  : connection === 'checking'
                    ? 'Checking application readiness…'
                    : 'The server could not be reached. Start it and refresh this page.'}
              </Typography>
            </Box>
          </Stack>
        </Paper>
      </Container>
    </ThemeProvider>
  )
}
