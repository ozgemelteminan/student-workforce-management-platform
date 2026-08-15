export type Skill = {
  id: string
  name: string
  description?: string | null
  isActive: boolean
}

export type ReferenceDataPayload = {
  name: string
  description?: string | null
}
