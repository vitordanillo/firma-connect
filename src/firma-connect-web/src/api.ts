export type CommunityAccess = { id: string; name: string; role: string }
export type AuthSession = { accessToken: string; expiresAt: string; userId: string; displayName: string; communities: CommunityAccess[] }
export type Institution = { id: string; name: string }
export type DirectoryProfile = { id: string; displayName: string; institution: string | null; course: string | null; headline: string | null; availableForTeam: boolean }
export type OwnProfile = { institutionId: string | null; course: string | null; headline: string | null; bio: string | null; contactUrl: string | null; availableForTeam: boolean; visibleInDirectory: boolean }

export class ApiError extends Error {
  constructor(public status: number, message: string) { super(message) }
}

export async function api<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  const response = await fetch(path, { ...options, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...options.headers } })
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { error?: string } | null
    throw new ApiError(response.status, body?.error ?? 'Não foi possível concluir a solicitação.')
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
