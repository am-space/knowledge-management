import { render, screen } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import App from './App'

afterEach(() => vi.restoreAllMocks())

test('shows the ready state when the server health request succeeds', async () => {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(
    new Response(JSON.stringify({ status: 'Healthy', checks: [] }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }),
  )

  render(<App />)

  expect(await screen.findByText('Server ready')).toBeInTheDocument()
  expect(screen.getByText(/configured persistence profile are ready/i)).toBeInTheDocument()
})
