import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Badge, Button, Card, CardContent, DataTable, ErrorState, MissingData, PageHeader, Pagination, SearchInput, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../components/ui'
import type { Student, StudentFilters } from '../../features/students/types'
import { useStudents } from '../../features/students/useStudentQueries'
import { formatIstanbulDate } from '../../lib/date-time'

export function StudentsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const filters = useMemo(() => filtersFromSearch(searchParams), [searchParams])
  const [draftSearch, setDraftSearch] = useState(filters.search ?? '')
  const query = useStudents(filters)

  useEffect(() => {
    const handle = window.setTimeout(() => updateFilters({ search: draftSearch || undefined, page: 1 }), 350)
    return () => window.clearTimeout(handle)
  }, [draftSearch])

  const updateFilters = (patch: Partial<StudentFilters>) => {
    const next = { ...filters, ...patch }
    setSearchParams(filtersToSearch(next))
  }

  const columns = [
    { key: 'name', header: 'Student', cell: (student: Student) => <div className="min-w-52"><p className="font-medium">{student.firstName} {student.lastName}</p><p className="truncate text-xs text-text-muted">{student.email}</p></div> },
    { key: 'department', header: 'Department', cell: (student: Student) => student.department || <MissingData kind="not-set" /> },
    { key: 'active', header: 'Status', cell: (student: Student) => <Badge variant={student.isActive ? 'success' : 'neutral'}>{student.isActive ? 'Active' : 'Inactive'}</Badge> },
    { key: 'created', header: 'Created', cell: (student: Student) => formatIstanbulDate(student.createdAt), className: 'hidden lg:table-cell' },
  ]

  return (
    <div className="space-y-5">
      <PageHeader title="Students" description="Manage student workforce records, active status, skills context, and workload signals." />
      <Card>
        <CardContent className="space-y-4">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-end">
            <div className="min-w-64 flex-1">
              <SearchInput label="Search students" value={draftSearch} onChange={(event) => setDraftSearch(event.target.value)} onClear={() => setDraftSearch('')} placeholder="Name, email, or department" />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Select value={filters.sortBy ?? 'name'} onValueChange={(value) => updateFilters({ sortBy: value as StudentFilters['sortBy'], page: 1 })}>
                <SelectTrigger aria-label="Sort students"><SelectValue /></SelectTrigger>
                <SelectContent>{['name', 'email', 'department', 'created'].map((sort) => <SelectItem key={sort} value={sort}>{sort}</SelectItem>)}</SelectContent>
              </Select>
              <Select value={filters.sortDirection ?? 'asc'} onValueChange={(value) => updateFilters({ sortDirection: value as 'asc' | 'desc', page: 1 })}>
                <SelectTrigger aria-label="Sort direction"><SelectValue /></SelectTrigger>
                <SelectContent><SelectItem value="asc">Ascending</SelectItem><SelectItem value="desc">Descending</SelectItem></SelectContent>
              </Select>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <Badge variant="neutral">{query.data?.totalCount ?? 0} students</Badge>
            {(filters.search || filters.sortBy !== 'name' || filters.sortDirection !== 'asc') ? <Button variant="ghost" size="sm" onClick={() => { setDraftSearch(''); setSearchParams(new URLSearchParams()) }}>Clear filters</Button> : null}
          </div>
        </CardContent>
      </Card>
      {query.isError ? <ErrorState title="Could not load students." description="The workforce list could not be fetched." retryAction={<Button variant="outline" onClick={() => void query.refetch()}>Retry</Button>} /> : (
        <DataTable
          columns={columns}
          rows={query.data?.items ?? []}
          getRowKey={(student) => student.id}
          isLoading={query.isLoading}
          onRowClick={(student) => navigate(`/students/${student.id}`)}
          emptyState={<div className="rounded-lg border border-border bg-surface p-8 text-center"><p className="font-medium">No students found.</p><p className="mt-1 text-sm text-text-secondary">Adjust filters or invite students through the admin invitation flow.</p></div>}
          pagination={query.data ? <Pagination {...query.data} onPageChange={(page) => updateFilters({ page })} /> : undefined}
        />
      )}
    </div>
  )
}

function filtersFromSearch(searchParams: URLSearchParams): StudentFilters {
  return {
    page: Number(searchParams.get('page') ?? 1),
    pageSize: Number(searchParams.get('pageSize') ?? 20),
    search: searchParams.get('search') ?? undefined,
    sortBy: (searchParams.get('sortBy') as StudentFilters['sortBy']) ?? 'name',
    sortDirection: (searchParams.get('sortDirection') as 'asc' | 'desc') ?? 'asc',
  }
}

function filtersToSearch(filters: StudentFilters) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  return search
}
