import { test, expect } from '@playwright/test'

test('local Article survives create, preview, reopen, edit, conflict, and empty-body saves', async ({ page }) => {
  const initialMarkdown = '# Initial heading\n\nExact **Markdown** with trailing spaces.  \n\n- First\n- Second\n'
  const updatedMarkdown = '# Updated heading\n\nA new immutable revision.\n'
  const tree = page.getByRole('navigation', { name: 'Knowledge tree' })
  const title = page.getByRole('textbox', { name: 'Title', exact: true })
  const source = page.getByRole('textbox', { name: 'Markdown source', exact: true })

  await page.goto('/')
  await expect(page.getByText('Local workspace ready', { exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Create article', exact: true }).click()
  await expect(title).toBeFocused()
  await title.fill('Browser Article')
  await source.fill(initialMarkdown)
  await page.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Initial heading' })).toBeVisible()
  await page.getByRole('tab', { name: 'Markdown source' }).click()
  await expect(source).toHaveValue(initialMarkdown)

  const creation = page.waitForResponse(response => response.url().endsWith('/api/articles') && response.request().method() === 'POST')
  await page.getByRole('button', { name: 'Create', exact: true }).click()
  const createdResponse = await creation
  expect(createdResponse.status()).toBe(201)
  const created = await createdResponse.json()
  expect(created.currentRevision.version).toBe(1)
  expect(created.currentRevision.contentMarkdown).toBe(initialMarkdown)
  await expect(page.getByText('Saved revision 1', { exact: true })).toBeVisible()

  // A full reload exercises the browser index and fresh HTTP reads from SQLite.
  await page.reload()
  await tree.getByRole('button', { name: 'Browser Article Revision 1' }).click()
  await expect(source).toHaveValue(initialMarkdown)
  await expect(title).toHaveValue('Browser Article')
  await title.fill('Edited Browser Article')
  await source.fill(updatedMarkdown)
  await page.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Updated heading' })).toBeVisible()
  await page.getByRole('button', { name: 'Save', exact: true }).click()
  await expect(page.getByText('Saved revision 2', { exact: true })).toBeVisible()

  await page.reload()
  await tree.getByRole('button', { name: 'Edited Browser Article Revision 2' }).click()
  await expect(source).toHaveValue(updatedMarkdown)
  const persistedResponse = await page.request.get(`/api/articles/${created.id}`)
  expect(persistedResponse.status()).toBe(200)
  const persisted = await persistedResponse.json()
  expect(persisted.id).toBe(created.id)
  expect(persisted.currentRevision.version).toBe(2)
  expect(persisted.currentRevision.id).not.toBe(created.currentRevision.id)

  // A second writer makes the browser's revision stale; no HTTP responses are mocked.
  const otherWrite = await page.request.put(`/api/articles/${created.id}`, {
    data: { expectedRevisionVersion: 2, title: 'Other writer', contentMarkdown: 'Other content' },
  })
  expect(otherWrite.status()).toBe(200)
  await source.fill('My unsaved draft')
  await page.getByRole('button', { name: 'Save', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('now revision 3')
  await expect(source).toHaveValue('My unsaved draft')
  page.once('dialog', dialog => dialog.dismiss())
  await page.getByRole('button', { name: 'New', exact: true }).click()
  await expect(source).toHaveValue('My unsaved draft')
  page.once('dialog', dialog => dialog.accept())
  await page.getByRole('button', { name: 'Reload server version' }).click()
  await expect(source).toHaveValue('Other content')

  await source.fill('')
  await page.getByRole('button', { name: 'Save', exact: true }).click()
  await expect(page.getByText('Saved revision 4', { exact: true })).toBeVisible()
  await page.reload()
  await tree.getByRole('button', { name: 'Other writer Revision 4' }).click()
  await expect(source).toHaveValue('')
  await page.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(page.getByText('Nothing to preview yet.')).toBeVisible()
})
