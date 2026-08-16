import { BarChart3, RefreshCw } from 'lucide-react'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { Badge, Button, Card, CardContent, CardHeader, EmptyState, ErrorState, PageHeader, Skeleton } from '../../components/ui'
import { useAnalytics } from '../../features/analytics/useAnalyticsQueries'
import { requestStatusLabels, requestTypeLabels } from '../../features/requests/requestPresentation'
import { statusLabels } from '../../features/tasks/taskPresentation'

export function AnalyticsPage() {
  const analytics = useAnalytics()
  const hasAnyData = Boolean(analytics.dashboard.data || analytics.taskStatus.data?.length || analytics.taskCategory.data?.length || analytics.workload.data?.length || analytics.requests.data?.length)
  const allFailed = !hasAnyData && [analytics.dashboard, analytics.taskStatus, analytics.taskCategory, analytics.workload, analytics.requests].every((query) => query.isError)
  const retryFailed = () => {
    void Promise.all([analytics.dashboard, analytics.taskStatus, analytics.taskCategory, analytics.workload, analytics.requests].filter((query) => query.isError).map((query) => query.refetch()))
  }

  return (
    <div className="space-y-5">
      <PageHeader title="Analytics" description="Operational aggregates from the analytics API." />
      {allFailed ? <ErrorState title="Analytics could not be loaded." description="Refresh the workspace or check your access." retryAction={<Button variant="outline" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={() => void Promise.all([analytics.dashboard.refetch(), analytics.taskStatus.refetch(), analytics.taskCategory.refetch(), analytics.workload.refetch(), analytics.requests.refetch()])}>Retry</Button>} /> : null}
      {!allFailed && [analytics.dashboard, analytics.taskStatus, analytics.taskCategory, analytics.workload, analytics.requests].some((query) => query.isError) ? <div className="rounded-md border border-warning/30 bg-warning/10 px-3 py-2 text-sm text-text-primary"><span>Some analytics widgets could not be loaded.</span> <Button size="sm" variant="ghost" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={retryFailed}>Retry failed widgets</Button></div> : null}
      {analytics.dashboard.isLoading ? <div className="grid gap-4 md:grid-cols-3"><Skeleton className="h-28" /><Skeleton className="h-28" /><Skeleton className="h-28" /></div> : null}
      {analytics.dashboard.isError && !analytics.dashboard.data ? <WidgetError title="KPI cards could not be loaded." onRetry={() => void analytics.dashboard.refetch()} /> : null}
      {analytics.dashboard.data ? (
        <div className="grid gap-4 md:grid-cols-3 xl:grid-cols-6">
          {Object.entries(analytics.dashboard.data).map(([key, value]) => <Metric key={key} label={labelize(key)} value={value} />)}
        </div>
      ) : null}
      <div className="grid gap-5 xl:grid-cols-2">
        <ChartCard title="Tasks by status" data={analytics.taskStatus.data?.map((item) => ({ label: statusLabels[item.status], value: item.count })) ?? []} isLoading={analytics.taskStatus.isLoading} isError={analytics.taskStatus.isError} onRetry={() => void analytics.taskStatus.refetch()} />
        <ChartCard title="Tasks by category" data={analytics.taskCategory.data?.map((item) => ({ label: item.categoryName, value: item.count })) ?? []} isLoading={analytics.taskCategory.isLoading} isError={analytics.taskCategory.isError} onRetry={() => void analytics.taskCategory.refetch()} />
      </div>
      <div className="grid gap-5 xl:grid-cols-2">
        <Card>
          <CardHeader><h2 className="text-sm font-semibold">Workload distribution</h2></CardHeader>
          <CardContent>
            {analytics.workload.isLoading ? <Skeleton className="h-48" /> : analytics.workload.isError ? <WidgetError title="Workload distribution could not be loaded." onRetry={() => void analytics.workload.refetch()} /> : !analytics.workload.data?.length ? <EmptyState title="No workload data." /> : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="text-xs uppercase text-text-muted"><tr><th className="py-2">Student</th><th>Active tasks</th><th>Workload minutes</th></tr></thead>
                  <tbody className="divide-y divide-border">{analytics.workload.data.map((item) => <tr key={item.studentId}><td className="py-2 font-medium">{item.studentName}</td><td>{item.activeTaskCount}</td><td>{item.activeWorkloadMinutes}</td></tr>)}</tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardHeader><h2 className="text-sm font-semibold">Request breakdown</h2></CardHeader>
          <CardContent className="space-y-2">
            {analytics.requests.isLoading ? <Skeleton className="h-48" /> : analytics.requests.isError ? <WidgetError title="Request breakdown could not be loaded." onRetry={() => void analytics.requests.refetch()} /> : !analytics.requests.data?.length ? <EmptyState title="No request analytics." /> : analytics.requests.data.map((item) => (
              <div key={`${item.type}-${item.status}`} className="flex items-center justify-between rounded-md border border-border px-3 py-2 text-sm">
                <span>{requestTypeLabels[item.type]}</span><Badge>{requestStatusLabels[item.status]}</Badge><strong>{item.count}</strong>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function Metric({ label, value }: { label: string; value: number }) {
  return <Card><CardContent><p className="text-xs text-text-secondary">{label}</p><p className="mt-2 text-2xl font-semibold text-text-primary">{value}</p></CardContent></Card>
}

function ChartCard({ title, data, isLoading, isError, onRetry }: { title: string; data: { label: string; value: number }[]; isLoading?: boolean; isError?: boolean; onRetry?: () => void }) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center gap-2"><BarChart3 aria-hidden="true" className="h-4 w-4 text-brand" /><h2 className="text-sm font-semibold">{title}</h2></CardHeader>
      <CardContent>
        {isLoading ? <Skeleton className="h-72" /> : isError ? <WidgetError title={`${title} could not be loaded.`} onRetry={onRetry} /> : !data.length ? <EmptyState title="No chart data." /> : (
          <>
            <figure aria-label={title} className="h-72">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data} margin={{ top: 8, right: 16, bottom: 36, left: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" angle={-25} textAnchor="end" interval={0} height={70} tick={{ fontSize: 11 }} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="value" fill="#C91F28" name="Count" />
                </BarChart>
              </ResponsiveContainer>
            </figure>
            <table className="sr-only"><caption>{title}</caption><tbody>{data.map((item) => <tr key={item.label}><th>{item.label}</th><td>{item.value}</td></tr>)}</tbody></table>
          </>
        )}
      </CardContent>
    </Card>
  )
}

function WidgetError({ title, onRetry }: { title: string; onRetry?: () => void }) {
  return <ErrorState title={title} description="This widget can be retried without hiding other analytics." retryAction={onRetry ? <Button variant="outline" size="sm" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={onRetry}>Retry</Button> : undefined} />
}

function labelize(value: string) {
  return value.replace(/[A-Z]/g, (match) => ` ${match.toLowerCase()}`).replace(/^./, (match) => match.toUpperCase()).trim()
}
