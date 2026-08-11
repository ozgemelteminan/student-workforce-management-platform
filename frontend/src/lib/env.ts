import { z } from 'zod'

const envSchema = z.object({
  VITE_API_BASE_URL: z.string().url(),
  VITE_API_VERSION: z.string().startsWith('/'),
  VITE_DISPLAY_TIMEZONE: z.literal('Europe/Istanbul').default('Europe/Istanbul'),
})

const parsedEnv = envSchema.safeParse({
  VITE_API_BASE_URL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080',
  VITE_API_VERSION: import.meta.env.VITE_API_VERSION ?? '/api/v1',
  VITE_DISPLAY_TIMEZONE: import.meta.env.VITE_DISPLAY_TIMEZONE ?? 'Europe/Istanbul',
})

if (!parsedEnv.success) {
  throw new Error(`Invalid frontend environment configuration: ${parsedEnv.error.message}`)
}

export const env = {
  apiBaseUrl: parsedEnv.data.VITE_API_BASE_URL.replace(/\/$/, ''),
  apiVersion: parsedEnv.data.VITE_API_VERSION,
  displayTimeZone: parsedEnv.data.VITE_DISPLAY_TIMEZONE,
} as const

