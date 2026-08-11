import { describe, expect, it } from 'vitest'
import { formatFileSize, validateUploadFile } from './filePresentation'

describe('file presentation helpers', () => {
  it('formats byte sizes with binary units', () => {
    expect(formatFileSize(842)).toBe('842 B')
    expect(formatFileSize(24 * 1024)).toBe('24 KB')
    expect(formatFileSize(8.4 * 1024 * 1024)).toBe('8.4 MB')
    expect(formatFileSize(1.2 * 1024 * 1024 * 1024)).toBe('1.2 GB')
  })

  it('mirrors the canonical upload size and extension preflight', () => {
    expect(validateUploadFile(new File(['x'], 'notes.txt', { type: 'text/plain' }))).toBeNull()
    expect(validateUploadFile(new File(['x'], 'payload.exe', { type: 'application/octet-stream' }))).toBe('This file type is not allowed.')
    expect(validateUploadFile(new File(['x'], 'image.png', { type: 'application/pdf' }))).toBe('The file type does not match the selected file extension.')
  })
})
