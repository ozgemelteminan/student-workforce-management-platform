import { passwordRequirements } from '../authSchemas'

export function PasswordRequirements() {
  return (
    <div className="rounded-md border border-border bg-surface-secondary px-3 py-2 text-xs text-text-secondary">
      <p className="font-medium text-text-primary">Password requirements</p>
      <ul className="mt-1 list-disc space-y-0.5 pl-4">
        {passwordRequirements.map((requirement) => (
          <li key={requirement}>{requirement}</li>
        ))}
      </ul>
    </div>
  )
}
