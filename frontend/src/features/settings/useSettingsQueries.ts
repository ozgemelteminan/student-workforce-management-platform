import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { getSettings, updateSetting } from './api/settingsApi'

export function useSettings() {
  return useQuery({ queryKey: queryKeys.settings.all, queryFn: ({ signal }) => getSettings(signal) })
}

export function useSettingMutations() {
  const queryClient = useQueryClient()
  return {
    update: useMutation({
      mutationFn: ({ key, value, concurrencyToken }: { key: string; value: string; concurrencyToken: string }) => updateSetting(key, value, concurrencyToken),
      onSuccess: async () => {
        appToast.success('Setting updated.')
        await queryClient.invalidateQueries({ queryKey: queryKeys.settings.all })
      },
    }),
  }
}
