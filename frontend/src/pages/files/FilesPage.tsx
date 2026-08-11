import { Download, FileText, Folder, FolderPlus, Pencil, Trash2, Upload, X } from 'lucide-react'
import { useMemo, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  EmptyState,
  ErrorState,
  Input,
  PageHeader,
  SearchInput,
  Skeleton,
} from '../../components/ui'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDateTime } from '../../lib/date-time'
import { cn } from '../../lib/utils/cn'
import { formatFileSize, validateUploadFile } from '../../features/files/filePresentation'
import { useDepartmentFiles, useFileFolders, useFileMutations } from '../../features/files/useFileQueries'
import type { DepartmentFile, FileFolder } from '../../features/files/types'

type FolderCrumb = { id: string; name: string }
type UploadState = { file: File; status: 'waiting' | 'uploading' | 'finalizing' | 'complete' | 'failed' | 'cancelled'; progress: number; error?: string }

export function FilesPage() {
  const { user } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const folderId = searchParams.get('folder')
  const search = searchParams.get('search') ?? ''
  const [crumbs, setCrumbs] = useState<FolderCrumb[]>([])
  const canManage = user?.roles.some((role) => role === 'ADMIN' || role === 'TASK_MANAGER') ?? false
  const filesQuery = useDepartmentFiles({ page: 1, pageSize: 50, search, folderId })
  const foldersQuery = useFileFolders(folderId)
  const mutations = useFileMutations(folderId)
  const [folderDialog, setFolderDialog] = useState<{ mode: 'create' | 'rename'; folder?: FileFolder } | null>(null)
  const [folderName, setFolderName] = useState('')
  const [deleteFolder, setDeleteFolder] = useState<FileFolder | null>(null)
  const [deleteFile, setDeleteFile] = useState<DepartmentFile | null>(null)
  const [upload, setUpload] = useState<UploadState | null>(null)
  const uploadAbort = useRef<AbortController | null>(null)

  const visibleCrumbs = useMemo(() => crumbs.filter((crumb) => crumb.id !== folderId || folderId), [crumbs, folderId])
  const isLoading = filesQuery.isLoading || foldersQuery.isLoading
  const isEmpty = !isLoading && !filesQuery.data?.items.length && !foldersQuery.data?.length

  const setFolder = (folder: FileFolder | null) => {
    const params = new URLSearchParams(searchParams)
    if (folder) {
      params.set('folder', folder.id)
      setCrumbs((current) => [...current.filter((crumb) => crumb.id !== folder.id), { id: folder.id, name: folder.name }])
    } else {
      params.delete('folder')
      setCrumbs([])
    }
    setSearchParams(params)
  }

  const setSearch = (value: string) => {
    const params = new URLSearchParams(searchParams)
    if (value) params.set('search', value)
    else params.delete('search')
    setSearchParams(params, { replace: true })
  }

  const openFolderDialog = (mode: 'create' | 'rename', folder?: FileFolder) => {
    setFolderDialog({ mode, folder })
    setFolderName(folder?.name ?? '')
  }

  const submitFolder = async () => {
    const name = folderName.trim()
    if (!name) return
    if (folderDialog?.mode === 'rename' && folderDialog.folder) {
      await mutations.renameFolder.mutateAsync({ id: folderDialog.folder.id, name })
    } else {
      await mutations.createFolder.mutateAsync(name)
    }
    setFolderDialog(null)
    setFolderName('')
  }

  const selectUpload = (file: File | null) => {
    if (!file) return
    const validation = validateUploadFile(file)
    setUpload(validation ? { file, status: 'failed', progress: 0, error: validation } : { file, status: 'waiting', progress: 0 })
  }

  const startUpload = async () => {
    if (!upload) return
    const controller = new AbortController()
    uploadAbort.current = controller
    setUpload({ ...upload, status: 'uploading', progress: 0, error: undefined })
    try {
      await mutations.uploadFile.mutateAsync({
        file: upload.file,
        signal: controller.signal,
        onProgress: (progress) => setUpload((current) => current ? { ...current, progress, status: progress >= 100 ? 'finalizing' : 'uploading' } : current),
      })
      setUpload((current) => current ? { ...current, status: 'complete', progress: 100 } : current)
    } catch (error) {
      const cancelled = error instanceof DOMException && error.name === 'AbortError'
      setUpload((current) => current ? { ...current, status: cancelled ? 'cancelled' : 'failed', error: cancelled ? 'Upload cancelled.' : 'Upload failed. Try again.' } : current)
    } finally {
      uploadAbort.current = null
    }
  }

  return (
    <div className="space-y-5">
      <PageHeader
        title="Department Files"
        description="Browse department assets, forms, templates, and documents."
        breadcrumbs={[
          { label: 'Department Files', href: '/files' },
          ...visibleCrumbs.map((crumb) => ({ label: crumb.name, href: `/files?folder=${crumb.id}` })),
        ]}
        secondaryActions={canManage ? <Button variant="outline" iconBefore={<FolderPlus aria-hidden="true" className="h-4 w-4" />} onClick={() => openFolderDialog('create')}>New folder</Button> : null}
        primaryAction={canManage ? <FilePicker onSelect={selectUpload} /> : null}
      />

      <Card>
        <CardHeader className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div className="flex flex-wrap items-center gap-2">
            {folderId ? <Button variant="ghost" size="sm" onClick={() => setFolder(null)}>Root</Button> : <Badge>Root</Badge>}
            {upload ? <UploadRow upload={upload} onStart={startUpload} onCancel={() => uploadAbort.current?.abort()} onClear={() => setUpload(null)} /> : null}
          </div>
          <SearchInput className="md:w-72" label="Search files" placeholder="Search files" value={search} onChange={(event) => setSearch(event.target.value)} onClear={() => setSearch('')} />
        </CardHeader>
        <CardContent className="space-y-3">
          {filesQuery.isError || foldersQuery.isError ? (
            <ErrorState title="Files could not be loaded." description="Refresh the workspace or try again after checking your access." retryAction={<Button variant="outline" onClick={() => void Promise.all([filesQuery.refetch(), foldersQuery.refetch()])}>Retry</Button>} />
          ) : isLoading ? (
            <FileSkeleton />
          ) : isEmpty ? (
            <EmptyState title={folderId ? 'This folder is empty.' : 'No department files yet.'} description={canManage ? 'Add a folder or upload a file when you are ready.' : 'Files published by staff will appear here.'} className="min-h-48" />
          ) : (
            <div className="divide-y divide-border overflow-hidden rounded-lg border border-border">
              {foldersQuery.data?.map((folder) => (
                <div key={folder.id} className="grid gap-3 bg-surface px-4 py-3 md:grid-cols-[1fr_auto] md:items-center">
                  <button type="button" className="flex min-w-0 items-center gap-3 text-left focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand" onClick={() => setFolder(folder)}>
                    <Folder aria-hidden="true" className="h-5 w-5 shrink-0 text-brand" />
                    <span className="truncate text-sm font-medium text-text-primary">{folder.name}</span>
                  </button>
                  {canManage ? <FolderActions onRename={() => openFolderDialog('rename', folder)} onDelete={() => setDeleteFolder(folder)} /> : null}
                </div>
              ))}
              {filesQuery.data?.items.map((file) => (
                <div key={file.id} className="grid gap-3 bg-surface px-4 py-3 md:grid-cols-[minmax(0,1fr)_auto] md:items-center">
                  <div className="flex min-w-0 items-start gap-3">
                    <FileText aria-hidden="true" className="mt-0.5 h-5 w-5 shrink-0 text-text-muted" />
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium text-text-primary">{file.fileName}</p>
                      <p className="mt-1 text-xs text-text-secondary">{formatFileSize(file.fileSize)} · {file.mimeType} · {formatIstanbulDateTime(file.confirmedAt ?? file.createdAt)}</p>
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button variant="outline" size="sm" iconBefore={<Download aria-hidden="true" className="h-4 w-4" />} isLoading={mutations.downloadFile.isPending} onClick={() => mutations.downloadFile.mutate(file)}>Download</Button>
                    {canManage ? <Button variant="ghost" size="sm" iconBefore={<Trash2 aria-hidden="true" className="h-4 w-4" />} onClick={() => setDeleteFile(file)}>Delete</Button> : null}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={Boolean(folderDialog)} onOpenChange={(open) => !open && setFolderDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{folderDialog?.mode === 'rename' ? 'Rename folder' : 'New folder'}</DialogTitle>
            <DialogDescription>{folderDialog?.mode === 'rename' ? 'Update the folder name.' : 'Create a folder in the current location.'}</DialogDescription>
          </DialogHeader>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">
            Folder name
            <Input value={folderName} maxLength={160} onChange={(event) => setFolderName(event.target.value)} />
          </label>
          <DialogFooter>
            <Button variant="outline" onClick={() => setFolderDialog(null)}>Cancel</Button>
            <Button onClick={() => void submitFolder()} isLoading={mutations.createFolder.isPending || mutations.renameFolder.isPending}>Save</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDelete title="Delete folder?" description="Only empty folders can be deleted. The server will reject non-empty folders." open={Boolean(deleteFolder)} onOpenChange={(open) => !open && setDeleteFolder(null)} onConfirm={() => deleteFolder && mutations.deleteFolder.mutate(deleteFolder.id, { onSettled: () => setDeleteFolder(null) })} />
      <ConfirmDelete title="Delete file?" description="This removes the file from the department file workspace." open={Boolean(deleteFile)} onOpenChange={(open) => !open && setDeleteFile(null)} onConfirm={() => deleteFile && mutations.deleteFile.mutate(deleteFile.id, { onSettled: () => setDeleteFile(null) })} />
    </div>
  )
}

function FilePicker({ onSelect }: { onSelect: (file: File | null) => void }) {
  return (
    <>
      <Button iconBefore={<Upload aria-hidden="true" className="h-4 w-4" />} onClick={() => document.getElementById('department-file-picker')?.click()}>
        Upload
      </Button>
      <input id="department-file-picker" type="file" className="sr-only" onChange={(event) => onSelect(event.target.files?.[0] ?? null)} />
    </>
  )
}

function UploadRow({ upload, onStart, onCancel, onClear }: { upload: UploadState; onStart: () => void; onCancel: () => void; onClear: () => void }) {
  return (
    <div className={cn('flex flex-wrap items-center gap-2 rounded-md border border-border bg-surface-secondary px-3 py-2 text-xs text-text-secondary', upload.status === 'failed' && 'border-destructive/30 bg-destructive/5')}>
      <span className="max-w-48 truncate font-medium text-text-primary">{upload.file.name}</span>
      <span>{upload.status === 'uploading' || upload.status === 'finalizing' ? `${upload.status === 'finalizing' ? 'Finalizing' : 'Uploading'} ${upload.progress}%` : upload.status}</span>
      {upload.error ? <span className="text-destructive">{upload.error}</span> : null}
      {upload.status === 'waiting' || upload.status === 'failed' || upload.status === 'cancelled' ? <Button size="sm" onClick={onStart}>Start</Button> : null}
      {upload.status === 'uploading' ? <Button variant="outline" size="sm" onClick={onCancel}>Cancel</Button> : null}
      {upload.status === 'complete' || upload.status === 'failed' || upload.status === 'cancelled' ? <Button variant="ghost" size="icon" aria-label="Clear upload" onClick={onClear}><X aria-hidden="true" className="h-4 w-4" /></Button> : null}
    </div>
  )
}

function FolderActions({ onRename, onDelete }: { onRename: () => void; onDelete: () => void }) {
  return (
    <div className="flex gap-2">
      <Button variant="outline" size="sm" iconBefore={<Pencil aria-hidden="true" className="h-4 w-4" />} onClick={onRename}>Rename</Button>
      <Button variant="ghost" size="sm" iconBefore={<Trash2 aria-hidden="true" className="h-4 w-4" />} onClick={onDelete}>Delete</Button>
    </div>
  )
}

function ConfirmDelete({ title, description, open, onOpenChange, onConfirm }: { title: string; description: string; open: boolean; onOpenChange: (open: boolean) => void; onConfirm: () => void }) {
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction asChild><Button variant="destructive" onClick={onConfirm}>Delete</Button></AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function FileSkeleton() {
  return (
    <div className="space-y-2">
      {Array.from({ length: 5 }).map((_, index) => <Skeleton key={index} className="h-14" />)}
    </div>
  )
}
