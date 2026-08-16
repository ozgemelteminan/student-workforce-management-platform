import { Button, Card, CardContent, CardHeader, EmptyState, ErrorState, Skeleton } from '../../../components/ui'
import { dayOfWeekValues, formatTimeRange, minutesFromTimeOnly, normalizeTimeOnly } from '../timeOnly'
import type { Availability, AvailabilityStatus, CourseSchedule, DayOfWeek } from '../types'

type EmptyAudience = 'student' | 'staff'

type TimetableBlock = {
  id: string
  dayOfWeek: DayOfWeek
  startTime: string
  endTime: string
  startMinutes: number
  endMinutes: number
  column: number
  columns: number
} & (
  | { kind: 'course'; item: CourseSchedule }
  | { kind: 'availability'; item: Availability }
)

type CourseWeeklyTimetableProps = {
  schedule: CourseSchedule[]
  availability?: Availability[]
  isLoading: boolean
  isError?: boolean
  emptyAudience?: EmptyAudience
  onRetry?: () => void
}

export function CourseWeeklyTimetable({
  schedule,
  availability = [],
  isLoading,
  isError,
  emptyAudience = 'student',
  onRetry,
}: CourseWeeklyTimetableProps) {
  if (isLoading) {
    return (
      <Card>
        <CardHeader><h2 className="text-sm font-semibold">Weekly timetable</h2></CardHeader>
        <CardContent><Skeleton className="h-80" /></CardContent>
      </Card>
    )
  }

  if (isError) {
    return (
      <Card>
        <CardContent>
          <ErrorState title="Could not load timetable." description="Course schedule records could not be fetched." retryAction={onRetry ? <Button variant="outline" onClick={onRetry}>Retry</Button> : undefined} />
        </CardContent>
      </Card>
    )
  }

  if (schedule.length === 0 && availability.length === 0) {
    return (
      <Card>
        <CardHeader><h2 className="text-sm font-semibold">Weekly timetable</h2></CardHeader>
        <CardContent>
          <EmptyState title={emptyAudience === 'staff' ? 'This student has not added a course schedule yet.' : 'No courses added yet.'} className="min-h-48" />
        </CardContent>
      </Card>
    )
  }

  const { start, end } = deriveTimetableRange(schedule, availability)
  const hourMarks = buildHourMarks(start, end)
  const blocksByDay = groupTimetableBlocks(schedule, availability)
  const pixelsPerMinute = 1.1
  const bodyHeight = Math.max(240, (end - start) * pixelsPerMinute)

  return (
    <Card>
      <CardHeader><h2 className="text-sm font-semibold">Weekly timetable</h2></CardHeader>
      <CardContent>
        <div className="overflow-x-auto pb-2" aria-label="Weekly course timetable">
          <div className="min-w-[58rem]">
            <div className="grid grid-cols-[4.5rem_repeat(7,minmax(7rem,1fr))] border-b border-border">
              <div aria-hidden="true" />
              {dayOfWeekValues.map((day) => <div key={day} className="px-2 pb-2 text-center text-xs font-semibold uppercase text-text-muted">{day}</div>)}
            </div>
            <div className="relative grid grid-cols-[4.5rem_repeat(7,minmax(7rem,1fr))]" style={{ height: bodyHeight }}>
              <div className="relative border-r border-border">
                {hourMarks.map((mark) => (
                  <div key={mark} className="absolute right-2 -translate-y-2 text-xs text-text-muted" style={{ top: (mark - start) * pixelsPerMinute }}>
                    {formatHour(mark)}
                  </div>
                ))}
              </div>
              {dayOfWeekValues.map((day) => (
                <div key={day} className="relative border-r border-border last:border-r-0" data-testid={`timetable-day-${day}`}>
                  {hourMarks.map((mark) => <div key={mark} aria-hidden="true" className="absolute left-0 right-0 border-t border-border/70" style={{ top: (mark - start) * pixelsPerMinute }} />)}
                  {(blocksByDay[day] ?? []).map((block) => <TimetableBlockArticle key={`${block.kind}-${block.id}`} block={block} timelineStart={start} pixelsPerMinute={pixelsPerMinute} />)}
                </div>
              ))}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function TimetableBlockArticle({ block, timelineStart, pixelsPerMinute }: { block: TimetableBlock; timelineStart: number; pixelsPerMinute: number }) {
  const style = {
    top: (block.startMinutes - timelineStart) * pixelsPerMinute,
    height: Math.max(40, (block.endMinutes - block.startMinutes) * pixelsPerMinute - 4),
    left: `calc(${block.column * (100 / block.columns)}% + 0.25rem)`,
    width: `calc(${100 / block.columns}% - 0.5rem)`,
  }

  if (block.kind === 'availability') {
    return (
      <article className={`absolute overflow-hidden rounded-md border px-2 py-1 text-xs shadow-sm ${availabilityBlockClass(block.item.status)}`} style={style}>
        <p className="truncate font-semibold text-text-primary">{formatAvailabilityStatus(block.item.status)}</p>
        <p className="mt-0.5 text-text-secondary">{formatTimeRange(block.startTime, block.endTime)}</p>
        {block.item.reason ? <p className="truncate text-text-secondary">{block.item.reason}</p> : null}
      </article>
    )
  }

  return (
    <article className="absolute overflow-hidden rounded-md border border-brand/30 bg-brand-subtle px-2 py-1 text-xs shadow-sm" style={style}>
      <p className="truncate font-semibold text-text-primary">{block.item.courseCode}</p>
      <p className="truncate text-text-primary">{block.item.courseName}</p>
      <p className="mt-0.5 text-text-secondary">{formatTimeRange(block.startTime, block.endTime)}</p>
      {block.item.location ? <p className="truncate text-text-secondary">{block.item.location}</p> : null}
    </article>
  )
}

function deriveTimetableRange(schedule: CourseSchedule[], availability: Availability[]) {
  const items = [...schedule, ...availability]
  const starts = items.map((item) => minutesFromTimeOnly(item.startTime)).filter((value): value is number => value !== null)
  const ends = items.map((item) => minutesFromTimeOnly(item.endTime)).filter((value): value is number => value !== null)
  const first = starts.length ? Math.min(...starts) : 8 * 60
  const last = ends.length ? Math.max(...ends) : 18 * 60
  return {
    start: Math.max(0, Math.floor(Math.min(first, 8 * 60) / 60) * 60),
    end: Math.min(24 * 60, Math.ceil(Math.max(last, 18 * 60) / 60) * 60),
  }
}

function buildHourMarks(start: number, end: number) {
  const marks: number[] = []
  for (let value = start; value <= end; value += 60) {
    marks.push(value)
  }
  return marks
}

function groupTimetableBlocks(schedule: CourseSchedule[], availability: Availability[]) {
  return dayOfWeekValues.reduce<Record<DayOfWeek, TimetableBlock[]>>((result, day) => {
    const courseBlocks = schedule
      .filter((course) => course.dayOfWeek === day)
      .map((course) => {
        const startMinutes = minutesFromTimeOnly(course.startTime) ?? 0
        const endMinutes = minutesFromTimeOnly(course.endTime) ?? startMinutes + 60
        return { id: course.id, kind: 'course' as const, item: course, dayOfWeek: course.dayOfWeek, startTime: normalizeTimeOnly(course.startTime), endTime: normalizeTimeOnly(course.endTime), startMinutes, endMinutes, column: 0, columns: 1 }
      })
    const availabilityBlocks = availability
      .filter((item) => item.dayOfWeek === day)
      .map((item) => {
        const startMinutes = minutesFromTimeOnly(item.startTime) ?? 0
        const endMinutes = minutesFromTimeOnly(item.endTime) ?? startMinutes + 60
        return { id: item.id, kind: 'availability' as const, item, dayOfWeek: item.dayOfWeek, startTime: normalizeTimeOnly(item.startTime), endTime: normalizeTimeOnly(item.endTime), startMinutes, endMinutes, column: 0, columns: 1 }
      })
    const blocks = [...courseBlocks, ...availabilityBlocks].sort((first, second) => first.startMinutes - second.startMinutes || first.endMinutes - second.endMinutes || first.kind.localeCompare(second.kind))
    result[day] = assignOverlapColumns(blocks)
    return result
  }, {} as Record<DayOfWeek, TimetableBlock[]>)
}

function assignOverlapColumns(blocks: TimetableBlock[]) {
  const active: TimetableBlock[] = []
  return blocks.map((block) => {
    for (let index = active.length - 1; index >= 0; index -= 1) {
      const activeBlock = active[index]
      if (activeBlock && activeBlock.endMinutes <= block.startMinutes) active.splice(index, 1)
    }
    const usedColumns = new Set(active.map((item) => item.column))
    let column = 0
    while (usedColumns.has(column)) column += 1
    block.column = column
    active.push(block)
    const columns = Math.max(1, ...active.map((item) => item.column + 1))
    active.forEach((item) => {
      item.columns = Math.max(item.columns, columns)
    })
    return block
  })
}

function formatAvailabilityStatus(status: AvailabilityStatus) {
  const labels: Record<AvailabilityStatus, string> = {
    AVAILABLE: 'Available',
    PREFERRED: 'Preferred',
    UNAVAILABLE: 'Unavailable',
  }
  return labels[status]
}

function availabilityBlockClass(status: AvailabilityStatus) {
  const classes: Record<AvailabilityStatus, string> = {
    AVAILABLE: 'border-green-200 bg-green-50',
    PREFERRED: 'border-blue-200 bg-blue-50',
    UNAVAILABLE: 'border-red-200 bg-red-50',
  }
  return classes[status]
}

function formatHour(minutes: number) {
  return `${String(Math.floor(minutes / 60)).padStart(2, '0')}:00`
}
