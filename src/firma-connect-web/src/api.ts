export type CommunityAccess = { id: string; name: string; role: string }
export type AuthSession = { accessToken: string; expiresAt: string; userId: string; displayName: string; communities: CommunityAccess[] }
export type Institution = { id: string; name: string }
export type TeamSituation = 'lookingForTeam' | 'hasTeam' | 'notLooking'
export type DirectoryProfile = { id: string; displayName: string; institution: string | null; course: string | null; headline: string | null; projectName: string | null; canHelpWith: string | null; lookingFor: string | null; teamSituation: TeamSituation; skills: string[]; interests: string[] }
export type OwnProfile = { institutionId: string | null; course: string | null; headline: string | null; bio: string | null; projectName: string | null; projectSummary: string | null; canHelpWith: string | null; lookingFor: string | null; contactUrl: string | null; teamSituation: TeamSituation; skills: string[]; interests: string[]; visibleInDirectory: boolean }
export type Team = { id: string; name: string; institution: string; projectSummary: string | null; isOpen: boolean; memberCount: number; openSpots: number; desiredSkills: string[] }
export type TeamDiscoverySummary = { institutionId: string; institution: string; participants: number; lookingForTeam: number; openTeams: number; alreadyInTeam: number }
export type OwnTeam = { team: Team; role: 'owner' | 'member' }
export type TeamJoinRequest = { id: string; teamId: string; requesterProfileId: string; requesterName: string; note: string | null; status: 'pending' | 'accepted' | 'declined' | 'cancelled'; createdAt: string }

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
