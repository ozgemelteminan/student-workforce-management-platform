import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { CourseWeeklyTimetable } from './CourseWeeklyTimetable'
import type { CourseSchedule } from '../types'

const baseCourse: CourseSchedule = {
  id: '11111111-1111-4111-8111-111111111111',
  studentId: '22222222-2222-4222-8222-222222222222',
  semesterId: '33333333-3333-4333-8333-333333333333',
  courseName: 'Algorithms',
  courseCode: 'CMPE 222',
  dayOfWeek: 'Tuesday',
  startTime: '09:00',
  endTime: '10:00',
  location: 'CSM 303',
}

describe('CourseWeeklyTimetable', () => {
  it('renders days exactly once in Monday-first order', () => {
    render(<CourseWeeklyTimetable schedule={[baseCourse]} isLoading={false} />)

    const dayLabels = screen.getAllByText(/^(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)$/).map((item) => item.textContent)
    expect(dayLabels).toEqual(['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'])
  })

  it('renders an existing course in the correct day with code, title, time, and location but no UUIDs', () => {
    render(<CourseWeeklyTimetable schedule={[baseCourse]} isLoading={false} />)

    const tuesday = screen.getByTestId('timetable-day-Tuesday')
    expect(within(tuesday).getByText('CMPE 222')).toBeInTheDocument()
    expect(within(tuesday).getByText('Algorithms')).toBeInTheDocument()
    expect(within(tuesday).getByText('09:00-10:00')).toBeInTheDocument()
    expect(within(tuesday).getByText('CSM 303')).toBeInTheDocument()
    expect(screen.queryByText(baseCourse.id)).not.toBeInTheDocument()
    expect(screen.queryByText(baseCourse.studentId)).not.toBeInTheDocument()
  })

  it('renders weekend, overlapping, and back-to-back courses without hiding them', () => {
    const courses: CourseSchedule[] = [
      baseCourse,
      { ...baseCourse, id: 'course-2', courseCode: 'MATH 101', courseName: 'Calculus', dayOfWeek: 'Tuesday', startTime: '09:30', endTime: '10:30', location: 'B 12' },
      { ...baseCourse, id: 'course-3', courseCode: 'HIST 210', courseName: 'History', dayOfWeek: 'Tuesday', startTime: '10:30', endTime: '11:30', location: 'A 4' },
      { ...baseCourse, id: 'course-4', courseCode: 'PHYS 104', courseName: 'Physics Lab', dayOfWeek: 'Sunday', startTime: '13:00', endTime: '15:00', location: 'Lab 2' },
    ]

    render(<CourseWeeklyTimetable schedule={courses} isLoading={false} />)

    const tuesday = screen.getByTestId('timetable-day-Tuesday')
    const sunday = screen.getByTestId('timetable-day-Sunday')
    expect(within(tuesday).getByText('CMPE 222')).toBeInTheDocument()
    expect(within(tuesday).getByText('MATH 101')).toBeInTheDocument()
    expect(within(tuesday).getByText('HIST 210')).toBeInTheDocument()
    expect(within(sunday).getByText('PHYS 104')).toBeInTheDocument()
  })

  it('uses student and staff empty-state copy distinctly', () => {
    const { rerender } = render(<CourseWeeklyTimetable schedule={[]} isLoading={false} emptyAudience="student" />)

    expect(screen.getByText('No courses added yet.')).toBeInTheDocument()
    rerender(<CourseWeeklyTimetable schedule={[]} isLoading={false} emptyAudience="staff" />)
    expect(screen.getByText('This student has not added a course schedule yet.')).toBeInTheDocument()
  })
})
