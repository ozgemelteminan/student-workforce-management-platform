export type SignedDownloadTarget = {
  downloadUrl: string
  expiresAt?: string
}

export async function openSignedDownload(target: SignedDownloadTarget): Promise<void> {
  window.location.assign(target.downloadUrl)
}

