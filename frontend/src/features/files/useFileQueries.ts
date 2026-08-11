import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { createFileFolder, deleteDepartmentFile, deleteFileFolder, downloadDepartmentFile, getDepartmentFiles, getFileFolders, renameFileFolder, uploadDepartmentFile } from './api/filesApi'
import type { DepartmentFile, FileListFilters } from './types'

export function useDepartmentFiles(filters: FileListFilters) {
  return useQuery({
    queryKey: queryKeys.files.list(stableFilters(filters)),
    queryFn: ({ signal }) => getDepartmentFiles(filters, signal),
  })
}

export function useFileFolders(parentFolderId: string | null) {
  return useQuery({
    queryKey: queryKeys.files.folders(parentFolderId),
    queryFn: ({ signal }) => getFileFolders(parentFolderId, signal),
  })
}

export function useFileMutations(folderId: string | null) {
  const queryClient = useQueryClient()
  const invalidateFiles = async () => {
    await queryClient.invalidateQueries({ queryKey: queryKeys.files.all })
  }

  return {
    createFolder: useMutation({
      mutationFn: (name: string) => createFileFolder(folderId, name),
      onSuccess: async () => {
        appToast.success('Folder created.')
        await invalidateFiles()
      },
    }),
    renameFolder: useMutation({
      mutationFn: ({ id, name }: { id: string; name: string }) => renameFileFolder(id, name),
      onSuccess: async () => {
        appToast.success('Folder renamed.')
        await invalidateFiles()
      },
    }),
    deleteFolder: useMutation({
      mutationFn: (id: string) => deleteFileFolder(id),
      onSuccess: async () => {
        appToast.success('Folder deleted.')
        await invalidateFiles()
      },
    }),
    uploadFile: useMutation({
      mutationFn: ({ file, signal, onProgress }: { file: File; signal?: AbortSignal; onProgress?: (progress: number) => void }) => uploadDepartmentFile(folderId, file, { signal, onProgress }),
      onSuccess: async () => {
        appToast.success('File uploaded.')
        await invalidateFiles()
      },
    }),
    downloadFile: useMutation({ mutationFn: (file: DepartmentFile) => downloadDepartmentFile(file) }),
    deleteFile: useMutation({
      mutationFn: (id: string) => deleteDepartmentFile(id),
      onSuccess: async () => {
        appToast.success('File deleted.')
        await invalidateFiles()
      },
    }),
  }
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
