import { cn } from '../../lib/utils/cn'

export type MissingDataKind = 'absent' | 'not-set' | 'not-available-yet'

const labels: Record<MissingDataKind, string> = {
  absent: '—',
  'not-set': 'Not set',
  'not-available-yet': 'Not available yet',
}

export function MissingData({ kind = 'absent', className }: { kind?: MissingDataKind; className?: string }) {
  return <span className={cn('text-text-muted', className)}>{labels[kind]}</span>
}
