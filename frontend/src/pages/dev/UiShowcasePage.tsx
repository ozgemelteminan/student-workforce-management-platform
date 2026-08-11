import { Bell, CheckCircle2, MoreHorizontal, PanelRight, Trash2 } from 'lucide-react'
import { useState } from 'react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  Checkbox,
  DataTable,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  EmptyState,
  ErrorState,
  FormField,
  IconButton,
  Input,
  PageHeader,
  Pagination,
  Popover,
  PopoverContent,
  PopoverTrigger,
  SearchInput,
  Sheet,
  SheetBody,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
  SkeletonText,
  StatusBadge,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  Textarea,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '../../components/ui'

const exampleRows = [
  { id: 'text', name: 'Text state', state: 'Ready' },
  { id: 'overlay', name: 'Overlay state', state: 'Interactive' },
]

export function UiShowcasePage() {
  const [search, setSearch] = useState('')

  return (
    <div className="space-y-6">
      <PageHeader title="UI Showcase" description="Development-only shared component surface for Phase 2 verification." />

      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold">Buttons and Metadata</h2>
        </CardHeader>
        <CardContent className="flex flex-wrap items-center gap-2">
          <Button>Primary</Button>
          <Button variant="secondary">Secondary</Button>
          <Button variant="outline">Outline</Button>
          <Button variant="ghost">Ghost</Button>
          <Button variant="destructive" iconBefore={<Trash2 aria-hidden="true" className="h-4 w-4" />}>Destructive</Button>
          <IconButton label="Notifications" tooltip="Notifications" icon={<Bell aria-hidden="true" className="h-4 w-4" />} />
          <Badge variant="brand">Brand</Badge>
          <Badge variant="success">Success</Badge>
          <StatusBadge status="IN_PROGRESS" />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold">Forms</h2>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-2">
          <FormField label="Text input" helperText="Helper text remains close to the field.">
            {({ id, describedBy, invalid }) => <Input id={id} aria-describedby={describedBy} invalid={invalid} placeholder="Enter value" />}
          </FormField>
          <FormField label="Required field" required error="This field needs a value.">
            {({ id, describedBy, invalid }) => <Input id={id} aria-describedby={describedBy} invalid={invalid} />}
          </FormField>
          <FormField label="Search">
            {({ id }) => <SearchInput id={id} label="Search showcase" value={search} onChange={(event) => setSearch(event.target.value)} onClear={() => setSearch('')} />}
          </FormField>
          <FormField label="Notes">
            {({ id }) => <Textarea id={id} placeholder="Textarea" />}
          </FormField>
          <label className="flex items-center gap-2 text-sm">
            <Checkbox />
            Checkbox option
          </label>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold">Overlays</h2>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-2">
          <Dialog>
            <DialogTrigger asChild><Button variant="outline">Open Dialog</Button></DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Dialog title</DialogTitle>
                <DialogDescription>Focused interactions will use this accessible dialog foundation.</DialogDescription>
              </DialogHeader>
              <DialogFooter><Button>Done</Button></DialogFooter>
            </DialogContent>
          </Dialog>
          <AlertDialog>
            <AlertDialogTrigger asChild><Button variant="outline">Open AlertDialog</Button></AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Confirm this high-impact action?</AlertDialogTitle>
                <AlertDialogDescription>The consequence must be explained before a destructive action continues.</AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel asChild><Button variant="outline">Cancel</Button></AlertDialogCancel>
                <AlertDialogAction asChild><Button variant="destructive">Confirm</Button></AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
          <Sheet>
            <SheetTrigger asChild><Button variant="outline" iconBefore={<PanelRight aria-hidden="true" className="h-4 w-4" />}>Open Drawer</Button></SheetTrigger>
            <SheetContent>
              <SheetHeader>
                <SheetTitle className="text-base font-semibold">Drawer title</SheetTitle>
                <SheetDescription className="text-sm text-text-secondary">Drawer content scrolls independently.</SheetDescription>
              </SheetHeader>
              <SheetBody><SkeletonText /></SheetBody>
            </SheetContent>
          </Sheet>
          <DropdownMenu>
            <DropdownMenuTrigger asChild><IconButton label="Open actions" icon={<MoreHorizontal aria-hidden="true" className="h-4 w-4" />} /></DropdownMenuTrigger>
            <DropdownMenuContent>
              <DropdownMenuItem><CheckCircle2 aria-hidden="true" className="h-4 w-4" />Normal action</DropdownMenuItem>
              <DropdownMenuSeparator className="my-1 h-px bg-border" />
              <DropdownMenuItem destructive><Trash2 aria-hidden="true" className="h-4 w-4" />Destructive action</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          <Popover>
            <PopoverTrigger asChild><Button variant="outline">Open Popover</Button></PopoverTrigger>
            <PopoverContent>Popover foundation for filters and compact controls.</PopoverContent>
          </Popover>
          <Tooltip>
            <TooltipTrigger asChild><Button variant="outline">Tooltip</Button></TooltipTrigger>
            <TooltipContent>Helpful, non-critical information.</TooltipContent>
          </Tooltip>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold">Tabs, Table, and States</h2>
        </CardHeader>
        <CardContent className="space-y-4">
          <Tabs defaultValue="table">
            <TabsList>
              <TabsTrigger value="table">Table</TabsTrigger>
              <TabsTrigger value="states">States</TabsTrigger>
            </TabsList>
            <TabsContent value="table">
              <DataTable
                rows={exampleRows}
                getRowKey={(row) => row.id}
                columns={[
                  { key: 'name', header: 'Name', cell: (row) => row.name, sortable: true },
                  { key: 'state', header: 'State', cell: (row) => row.state },
                ]}
                pagination={<Pagination page={1} pageSize={10} totalCount={2} totalPages={1} hasNextPage={false} hasPreviousPage={false} onPageChange={() => undefined} />}
              />
            </TabsContent>
            <TabsContent value="states" className="grid gap-3 md:grid-cols-2">
              <EmptyState title="Empty state" description="This is the reusable empty-state pattern." />
              <ErrorState title="Something went wrong" description="Safe, concise error states avoid raw internal details." retryAction={<Button variant="outline">Retry</Button>} />
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>
    </div>
  )
}
