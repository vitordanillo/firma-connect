import { FormEvent, useEffect, useState } from 'react'
import { ApiError, api, AuthSession, DirectoryProfile, Institution, OwnProfile } from './api'

const SESSION_KEY = 'firma-connect-session'

function readSession(): AuthSession | null {
  try {
    const value = sessionStorage.getItem(SESSION_KEY)
    if (!value) return null
    const session = JSON.parse(value) as AuthSession
    return new Date(session.expiresAt) > new Date() ? session : null
  } catch { return null }
}

export function App() {
  const [session, setSession] = useState<AuthSession | null>(readSession)
  function authenticate(value: AuthSession) { sessionStorage.setItem(SESSION_KEY, JSON.stringify(value)); setSession(value) }
  function logout() { sessionStorage.removeItem(SESSION_KEY); setSession(null) }

  return <main className="page-shell">
    <nav className="nav"><a className="brand" href="#inicio">firma<span>.</span></a><span className="community">Piloto Comunidades</span>{session && <button className="ghost" onClick={logout}>Sair</button>}</nav>
    {session ? <CommunityArea session={session} /> : <AccessView onAuthenticated={authenticate} />}
  </main>
}

function AccessView({ onAuthenticated }: { onAuthenticated: (session: AuthSession) => void }) {
  const invitationToken = new URLSearchParams(location.search).get('invite') ?? ''
  const [registering, setRegistering] = useState(Boolean(invitationToken))
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError('')
    try {
      const session = registering
        ? await api<AuthSession>('/api/auth/register', { method: 'POST', body: JSON.stringify({ invitationToken, displayName, password }) })
        : await api<AuthSession>('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
      onAuthenticated(session)
    } catch (exception) { setError(exception instanceof ApiError ? exception.message : 'Falha inesperada ao entrar.') }
    finally { setBusy(false) }
  }

  return <section className="access-layout" id="inicio"><div className="hero compact"><p className="eyebrow">CONEXÕES QUE MOVEM PROJETOS</p><h1>Encontre as pessoas certas na sua comunidade.</h1><p className="lead">Descubra colegas da sua instituição e competências complementares sem perder a conversa no grupo.</p></div><form className="access-card" onSubmit={submit}><p className="eyebrow">{registering ? 'ACEITAR CONVITE' : 'ACESSAR'}</p><h2>{registering ? 'Crie seu perfil' : 'Entre na comunidade'}</h2>{registering ? <label>Nome completo<input value={displayName} onChange={event => setDisplayName(event.target.value)} required maxLength={100} /></label> : <label>E-mail<input type="email" value={email} onChange={event => setEmail(event.target.value)} required /></label>}<label>Senha<input type="password" value={password} onChange={event => setPassword(event.target.value)} required minLength={10} /></label>{error && <p className="error" role="alert">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Aguarde…' : registering ? 'Criar conta' : 'Entrar'}</button>{invitationToken && <button type="button" className="text-button" onClick={() => setRegistering(value => !value)}>{registering ? 'Já tenho uma conta' : 'Usar meu convite'}</button>}</form></section>
}

function CommunityArea({ session }: { session: AuthSession }) {
  const community = session.communities[0]
  const [profiles, setProfiles] = useState<DirectoryProfile[]>([])
  const [institutions, setInstitutions] = useState<Institution[]>([])
  const [query, setQuery] = useState('')
  const [institutionId, setInstitutionId] = useState('')
  const [teamOnly, setTeamOnly] = useState(true)
  const [editing, setEditing] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!community) return
    const params = new URLSearchParams({ page: '1', pageSize: '50' })
    if (query) params.set('query', query)
    if (institutionId) params.set('institutionId', institutionId)
    if (teamOnly) params.set('availableForTeam', 'true')
    api<{ items: DirectoryProfile[] }>(`/api/communities/${community.id}/profiles?${params}`, {}, session.accessToken).then(result => setProfiles(result.items)).catch(exception => setError(exception.message))
  }, [community, institutionId, query, session.accessToken, teamOnly])

  useEffect(() => { api<{ items: Institution[] }>('/api/institutions?page=1&pageSize=50', {}, session.accessToken).then(result => setInstitutions(result.items)).catch(exception => setError(exception.message)) }, [session.accessToken])

  if (!community) return <section className="empty"><h1>Nenhuma comunidade vinculada.</h1><p>Solicite um novo convite ao administrador.</p></section>

  return <><header className="workspace-header"><div><p className="eyebrow">{community.name}</p><h1>Olá, {session.displayName}.</h1></div><button className="ghost" onClick={() => setEditing(value => !value)}>{editing ? 'Voltar ao diretório' : 'Editar meu perfil'}</button></header>{editing ? <ProfileEditor session={session} communityId={community.id} institutions={institutions} /> : <section className="directory" id="diretorio"><div><p className="eyebrow">DIRETÓRIO</p><h2>Comece pela sua instituição.</h2></div><div className="filters"><input aria-label="Buscar" placeholder="Nome, curso ou área" value={query} onChange={event => setQuery(event.target.value)} /><select aria-label="Instituição" value={institutionId} onChange={event => setInstitutionId(event.target.value)}><option value="">Todas as instituições</option>{institutions.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select><label><input type="checkbox" checked={teamOnly} onChange={event => setTeamOnly(event.target.checked)} /> Disponível para equipe</label></div>{error && <p className="error" role="alert">{error}</p>}<div className="cards">{profiles.map(member => <article className="card" key={member.id}><div className="avatar">{member.displayName.split(' ').map(name => name[0]).slice(0, 2).join('')}</div><div><h3>{member.displayName}</h3><p className="meta">{[member.course, member.institution].filter(Boolean).join(' · ')}</p><p>{member.headline}</p>{member.availableForTeam && <span className="availability">Disponível para equipe</span>}</div></article>)}</div>{!profiles.length && !error && <p className="empty-result">Nenhum perfil corresponde aos filtros.</p>}</section>}</>
}

function ProfileEditor({ session, communityId, institutions }: { session: AuthSession; communityId: string; institutions: Institution[] }) {
  const empty: OwnProfile = { institutionId: null, course: '', headline: '', bio: '', contactUrl: '', availableForTeam: false, visibleInDirectory: true }
  const [profile, setProfile] = useState<OwnProfile>(empty)
  const [message, setMessage] = useState('')

  useEffect(() => { api<OwnProfile>(`/api/communities/${communityId}/profiles/me`, {}, session.accessToken).then(setProfile).catch(exception => { if (!(exception instanceof ApiError) || exception.status !== 404) setMessage(exception.message) }) }, [communityId, session.accessToken])
  async function save(event: FormEvent) { event.preventDefault(); setMessage(''); try { const saved = await api<OwnProfile>(`/api/communities/${communityId}/profiles/me`, { method: 'PUT', body: JSON.stringify(profile) }, session.accessToken); setProfile(saved); setMessage('Perfil salvo com sucesso.') } catch (exception) { setMessage(exception instanceof ApiError ? exception.message : 'Não foi possível salvar.') } }

  return <section className="profile-section"><form className="profile-form" onSubmit={save}><p className="eyebrow">MEU PERFIL</p><h2>Como você quer ser encontrado?</h2><label>Instituição<select value={profile.institutionId ?? ''} onChange={event => setProfile({ ...profile, institutionId: event.target.value || null })}><option value="">Selecione</option>{institutions.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select></label><label>Curso<input value={profile.course ?? ''} maxLength={120} onChange={event => setProfile({ ...profile, course: event.target.value })} /></label><label>Título profissional<input value={profile.headline ?? ''} maxLength={140} onChange={event => setProfile({ ...profile, headline: event.target.value })} /></label><label>Sobre você<textarea value={profile.bio ?? ''} maxLength={800} onChange={event => setProfile({ ...profile, bio: event.target.value })} /></label><label>Link de contato<input type="url" value={profile.contactUrl ?? ''} maxLength={300} onChange={event => setProfile({ ...profile, contactUrl: event.target.value })} /></label><label className="check"><input type="checkbox" checked={profile.availableForTeam} onChange={event => setProfile({ ...profile, availableForTeam: event.target.checked })} /> Estou disponível para formar equipe</label><label className="check"><input type="checkbox" checked={profile.visibleInDirectory} onChange={event => setProfile({ ...profile, visibleInDirectory: event.target.checked })} /> Mostrar meu perfil no diretório</label>{message && <p className="form-message" role="status">{message}</p>}<button className="primary">Salvar perfil</button></form></section>
}
