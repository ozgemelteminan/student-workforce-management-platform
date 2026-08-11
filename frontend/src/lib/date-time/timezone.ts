import { formatDistanceToNowStrict, isValid, parseISO } from 'date-fns'
import { formatInTimeZone, toZonedTime } from 'date-fns-tz'
import { env } from '../env'

export const DISPLAY_TIME_ZONE = env.displayTimeZone

export function formatIstanbulDate(value: string | Date, pattern = 'dd.MM.yyyy'): string {
  return formatInTimeZone(toDate(value), DISPLAY_TIME_ZONE, pattern)
}

export function formatIstanbulDateTime(value: string | Date, pattern = 'dd.MM.yyyy HH:mm'): string {
  return formatInTimeZone(toDate(value), DISPLAY_TIME_ZONE, pattern)
}

export function formatRelativeTime(value: string | Date): string {
  return formatDistanceToNowStrict(toDate(value), { addSuffix: true })
}

export function getCurrentIstanbulDateTime(): Date {
  return toZonedTime(new Date(), DISPLAY_TIME_ZONE)
}

export function formatDateOnly(dateOnly: string, pattern = 'dd.MM.yyyy'): string {
  const parsed = parseDateOnly(dateOnly)
  return formatInTimeZone(parsed, DISPLAY_TIME_ZONE, pattern)
}

export function parseDateOnly(dateOnly: string): Date {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateOnly)
  if (!match) {
    throw new Error(`Invalid date-only value: ${dateOnly}`)
  }

  const [, year, month, day] = match
  return new Date(Date.UTC(Number(year), Number(month) - 1, Number(day), 12, 0, 0))
}

export function isPastUtcInstant(value: string | Date, now = new Date()): boolean {
  return toDate(value).getTime() < now.getTime()
}

function toDate(value: string | Date): Date {
  const date = typeof value === 'string' ? parseISO(value) : value
  if (!isValid(date)) {
    throw new Error('Invalid date/time value.')
  }
  return date
}

