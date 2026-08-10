export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080'
export const apiVersion = import.meta.env.VITE_API_VERSION ?? '/api/v1'

export function buildApiUrl(path: string): string {
  return `${apiBaseUrl}${apiVersion}${path.startsWith('/') ? path : `/${path}`}`
}
