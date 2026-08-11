export const dayOfWeekValues = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'] as const
export const availabilityStatuses = ['AVAILABLE', 'UNAVAILABLE', 'PREFERRED'] as const

export function normalizeTimeOnly(value: string): string {
  const match = /^(\d{1,2}):(\d{2})(?::\d{2})?$/.exec(value.trim())
  if (!match) return value
  const hour = Number(match[1])
  const minute = Number(match[2])
  if (hour < 0 || hour > 23 || minute < 0 || minute > 59) return value
  return `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`
}

export function formatTimeRange(startTime: string, endTime: string): string {
  return `${normalizeTimeOnly(startTime)}-${normalizeTimeOnly(endTime)}`
}

export function isValidTimeRange(startTime: string, endTime: string): boolean {
  const start = normalizeTimeOnly(startTime)
  const end = normalizeTimeOnly(endTime)
  return /^\d{2}:\d{2}$/.test(start) && /^\d{2}:\d{2}$/.test(end) && end > start
}
