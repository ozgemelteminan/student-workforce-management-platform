export type SemesterStatus = 'PLANNED' | 'ACTIVE' | 'ARCHIVED'

export type Semester = {
  id: string
  name: string
  startDate: string
  endDate: string
  status: SemesterStatus
  concurrencyToken: string
  isActive: boolean
}

export type SemesterPayload = {
  name: string
  startDate: string
  endDate: string
  status: SemesterStatus
}
