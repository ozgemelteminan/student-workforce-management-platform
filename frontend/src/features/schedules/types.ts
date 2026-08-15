export type DayOfWeek = 'Sunday' | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday'
export type AvailabilityStatus = 'AVAILABLE' | 'UNAVAILABLE' | 'PREFERRED'
export type SemesterStatus = 'PLANNED' | 'ACTIVE' | 'ARCHIVED'

export type Semester = {
  id: string
  name: string
  startDate: string
  endDate: string
  status: SemesterStatus
  isActive: boolean
  concurrencyToken: string
}

export type CourseSchedule = {
  id: string
  studentId: string
  semesterId: string
  courseName: string
  courseCode: string
  dayOfWeek: DayOfWeek
  startTime: string
  endTime: string
  location?: string
}

export type Availability = {
  id: string
  studentId: string
  semesterId: string
  dayOfWeek: DayOfWeek
  startTime: string
  endTime: string
  status: AvailabilityStatus
  reason?: string
  concurrencyToken: string
}

export type SchedulePayload = {
  studentId: string
  semesterId: string
  courseName: string
  courseCode: string
  dayOfWeek: DayOfWeek
  startTime: string
  endTime: string
  location?: string
}

export type UpdateSchedulePayload = Omit<SchedulePayload, 'studentId' | 'semesterId'>

export type AvailabilityPayload = {
  studentId: string
  semesterId: string
  dayOfWeek: DayOfWeek
  startTime: string
  endTime: string
  status: AvailabilityStatus
  reason?: string
}

export type UpdateAvailabilityPayload = Omit<AvailabilityPayload, 'studentId' | 'semesterId'>
