import type { CampusPresence, MeetingStatus, MeetingType, TimesheetStatus } from './types'

export const meetingStatusLabels: Record<MeetingStatus, string> = {
  DRAFT: 'Draft',
  AVAILABILITY_REQUESTED: 'Availability requested',
  CONFIRMED: 'Confirmed',
  CANCELLED: 'Cancelled',
}

export const meetingTypeLabels: Record<MeetingType, string> = {
  IN_PERSON: 'In person',
  ONLINE: 'Online',
  HYBRID: 'Hybrid',
}

export const campusPresenceLabels: Record<CampusPresence, string> = {
  ON_CAMPUS: 'On campus',
  OFF_CAMPUS: 'Off campus',
  UNSURE: 'Unsure',
}

export const timesheetStatusLabels: Record<TimesheetStatus, string> = {
  DRAFT: 'Draft',
  SUBMITTED: 'Submitted',
  APPROVED: 'Approved',
  NEEDS_CORRECTION: 'Needs correction',
}
