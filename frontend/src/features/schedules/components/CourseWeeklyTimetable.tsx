import { Button, Card, CardContent, CardHeader, EmptyState, ErrorState, Skeleton } from '../../../components/ui'
import { dayOfWeekValues, formatTimeRange, minutesFromTimeOnly, normalizeTimeOnly } from '../timeOnly'
import type { CourseSchedule, DayOfWeek } from '../types'

type EmptyAudience = 'student' | 'staff'

type CourseBlock = CourseSchedule & {
  startMinutes: number
  endMinutes: number
  column: number
  columns: number
}

export function CourseWeeklyTimetable({
  schedule,
  isLoading,
  isError,
  emptyAudience = 'student',
  onRetry,
}: {
  schedule: CourseSchedule[]
  isLoading: boolean
  isError?: boolean
  emptyAudience?: EmptyAudience
  onRetry?: () => void
}) {
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

  if (schedule.length === 0) {
    return (
      <Card>
        <CardHeader><h2 className="text-sm font-semibold">Weekly timetable</h2></CardHeader>
        <CardContent>
          <EmptyState title={emptyAudience === 'staff' ? 'This student has not added a course schedule yet.' : 'No courses added yet.'} className="min-h-48" />
        </CardContent>
      </Card>
    )
  }

  const { start, end } = deriveTimetableRange(schedule)
  const hourMarks = buildHourMarks(start, end)
  const blocksByDay = groupCourseBlocks(schedule)
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
                  {(blocksByDay[day] ?? []).map((course) => (
                    <article
                      key={course.id}
                      className="absolute overflow-hidden rounded-md border border-brand/30 bg-brand-subtle px-2 py-1 text-xs shadow-sm"
                      style={{
                        top: (course.startMinutes - start) * pixelsPerMinute,
                        height: Math.max(40, (course.endMinutes - course.startMinutes) * pixelsPerMinute - 4),
                        left: `calc(${course.column * (100 / course.columns)}% + 0.25rem)`,
                        width: `calc(${100 / course.columns}% - 0.5rem)`,
                      }}
                    >
                      <p className="truncate font-semibold text-text-primary">{course.courseCode}</p>
                      <p className="truncate text-text-primary">{course.courseName}</p>
                      <p className="mt-0.5 text-text-secondary">{formatTimeRange(course.startTime, course.endTime)}</p>
                      {course.location ? <p className="truncate text-text-secondary">{course.location}</p> : null}
                    </article>
                  ))}
                </div>
              ))}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function deriveTimetableRange(schedule: CourseSchedule[]) {
  const starts = schedule.map((course) => minutesFromTimeOnly(course.startTime)).filter((value): value is number => value !== null)
  const ends = schedule.map((course) => minutesFromTimeOnly(course.endTime)).filter((value): value is number => value !== null)
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

function groupCourseBlocks(schedule: CourseSchedule[]) {
  return dayOfWeekValues.reduce<Record<DayOfWeek, CourseBlock[]>>((result, day) => {
    const courses = schedule
      .filter((course) => course.dayOfWeek === day)
      .map((course) => {
        const startMinutes = minutesFromTimeOnly(course.startTime) ?? 0
        const endMinutes = minutesFromTimeOnly(course.endTime) ?? startMinutes + 60
        return { ...course, startTime: normalizeTimeOnly(course.startTime), endTime: normalizeTimeOnly(course.endTime), startMinutes, endMinutes, column: 0, columns: 1 }
      })
      .sort((first, second) => first.startMinutes - second.startMinutes || first.endMinutes - second.endMinutes)
    result[day] = assignOverlapColumns(courses)
    return result
  }, {} as Record<DayOfWeek, CourseBlock[]>)
}

function assignOverlapColumns(courses: CourseBlock[]) {
  const active: CourseBlock[] = []
  return courses.map((course) => {
    for (let index = active.length - 1; index >= 0; index -= 1) {
      const activeCourse = active[index]
      if (activeCourse && activeCourse.endMinutes <= course.startMinutes) active.splice(index, 1)
    }
    const usedColumns = new Set(active.map((item) => item.column))
    let column = 0
    while (usedColumns.has(column)) column += 1
    course.column = column
    active.push(course)
    const columns = Math.max(1, ...active.map((item) => item.column + 1))
    active.forEach((item) => {
      item.columns = Math.max(item.columns, columns)
    })
    return course
  })
}

function formatHour(minutes: number) {
  return `${String(Math.floor(minutes / 60)).padStart(2, '0')}:00`
}
