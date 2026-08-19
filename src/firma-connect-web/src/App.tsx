import { FormEvent, useEffect, useState } from 'react'
import { ApiError, api, AuthSession, DirectoryProfile, Institution, OwnProfile, Team } from './api'

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
  const authenticate = (value: AuthSession) => { sessionStorage.setItem(SESSION_KEY, JSON.stringify(value)); setSession(value) }
  const logout = () => { sessionStorage.removeItem(SESSION_KEY); setSession(null) }
  return <main className="page-shell"><nav className="nav"><a className="brand" href="#inicio">firma<span>.</span></a><span className="community">Piloto Comunidades</span>{session && <button className="ghost" onClick={logout}>Sair</button>}</nav>{session ? <CommunityArea session={session} /> : <AccessView onAuthenticated={authenticate} />}</main>
}

function AccessView({ onAuthenticated }: { onAuthenticated: (session: AuthSession) => void }) {
  const invitationToken = new URLSearchParams(location.search).get('invite') ?? ''
  const [registering, setRegistering] = useState(Boolean(invitationToken))
  const [email, setEmail] = useState(''); const [displayName, setDisplayName] = useState(''); const [password, setPassword] = useState('')
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
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

type View = 'people' | 'teams' | 'profile'

function CommunityArea({ session }: { session: AuthSession }) {
  const community = session.communities[0]
  const [view, setView] = useState<View>('people')
  const [institutions, setInstitutions] = useState<Institution[]>([])
  useEffect(() => { api<{ items: Institution[] }>('/api/institutions?page=1&pageSize=50', {}, session.accessToken).then(result => setInstitutions(result.items)) }, [session.accessToken])
  if (!community) return <section className="empty"><h1>Nenhuma comunidade vinculada.</h1><p>Solicite um novo convite ao administrador.</p></section>
  return <><header className="workspace-header"><div><p className="eyebrow">{community.name}</p><h1>Olá, {session.displayName}.</h1></div><div className="view-tabs"><button className={view === 'people' ? 'active' : ''} onClick={() => setView('people')}>Pessoas</button><button className={view === 'teams' ? 'active' : ''} onClick={() => setView('teams')}>Equipes</button><button className={view === 'profile' ? 'active' : ''} onClick={() => setView('profile')}>Meu perfil</button></div></header>{view === 'people' && <PeopleDirectory session={session} communityId={community.id} institutions={institutions} />}{view === 'teams' && <TeamsDirectory session={session} communityId={community.id} />}{view === 'profile' && <ProfileEditor session={session} communityId={community.id} institutions={institutions} />}</>
}

function PeopleDirectory({ session, communityId, institutions }: { session: AuthSession; communityId: string; institutions: Institution[] }) {
  const [profiles, setProfiles] = useState<DirectoryProfile[]>([]); const [query, setQuery] = useState(''); const [institutionId, setInstitutionId] = useState(''); const [skill, setSkill] = useState(''); const [teamOnly, setTeamOnly] = useState(true); const [error, setError] = useState('')
  useEffect(() => {
    const params = new URLSearchParams({ page: '1', pageSize: '50', sameInstitutionFirst: 'true' })
    if (query) params.set('query', query); if (institutionId) params.set('institutionId', institutionId); if (skill) params.set('skill', skill); if (teamOnly) params.set('teamSituation', 'lookingForTeam')
    api<{ items: DirectoryProfile[] }>(`/api/communities/${communityId}/profiles?${params}`, {}, session.accessToken).then(result => { setProfiles(result.items); setError('') }).catch(exception => setError(exception.message))
  }, [communityId, institutionId, query, session.accessToken, skill, teamOnly])
  return <section className="directory"><div><p className="eyebrow">ENCONTRAR MINHA EQUIPE</p><h2>Pessoas da sua instituição aparecem primeiro.</h2></div><div className="filters"><input aria-label="Buscar" placeholder="Nome, projeto ou necessidade" value={query} onChange={event => setQuery(event.target.value)} /><input aria-label="Competência" placeholder="Competência" value={skill} onChange={event => setSkill(event.target.value)} /><select aria-label="Instituição" value={institutionId} onChange={event => setInstitutionId(event.target.value)}><option value="">Todas as instituições</option>{institutions.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select><label><input type="checkbox" checked={teamOnly} onChange={event => setTeamOnly(event.target.checked)} /> Procurando equipe</label></div>{error && <p className="error">{error}</p>}<div className="cards">{profiles.map(member => <article className="card profile-card" key={member.id}><div className="avatar">{initials(member.displayName)}</div><div><h3>{member.displayName}</h3><p className="meta">{[member.course, member.institution].filter(Boolean).join(' · ')}</p>{member.projectName && <p><strong>Projeto:</strong> {member.projectName}</p>}{member.lookingFor && <p><strong>Procura:</strong> {member.lookingFor}</p>}{member.canHelpWith && <p><strong>Pode ajudar:</strong> {member.canHelpWith}</p>}<div className="tags">{member.skills.map(item => <span key={item}>{item}</span>)}</div>{member.teamSituation === 'lookingForTeam' && <span className="availability">Procurando equipe</span>}</div></article>)}</div>{!profiles.length && !error && <p className="empty-result">Nenhum participante corresponde aos filtros.</p>}</section>
}

function TeamsDirectory({ session, communityId }: { session: AuthSession; communityId: string }) {
  const [teams, setTeams] = useState<Team[]>([]); const [skill, setSkill] = useState(''); const [error, setError] = useState(''); const [creating, setCreating] = useState(false)
  const load = () => { const params = new URLSearchParams({ openOnly: 'true', sameInstitutionFirst: 'true' }); if (skill) params.set('skill', skill); api<{ items: Team[] }>(`/api/communities/${communityId}/teams?${params}`, {}, session.accessToken).then(result => { setTeams(result.items); setError('') }).catch(exception => setError(exception.message)) }
  useEffect(load, [communityId, session.accessToken, skill])
  async function request(teamId: string) { try { await api(`/api/communities/${communityId}/teams/${teamId}/requests`, { method: 'POST', body: JSON.stringify({ note: 'Tenho interesse em conhecer o projeto e contribuir com a equipe.' }) }, session.accessToken); setError('Solicitação enviada.') } catch (exception) { setError(exception instanceof ApiError ? exception.message : 'Não foi possível enviar.') } }
  return <section className="directory"><div className="section-heading"><div><p className="eyebrow">EQUIPES ABERTAS</p><h2>Projetos da sua instituição aparecem primeiro.</h2></div><button className="primary" onClick={() => setCreating(value => !value)}>Criar equipe</button></div>{creating && <CreateTeamForm session={session} communityId={communityId} onCreated={() => { setCreating(false); load() }} />}<div className="filters"><input placeholder="Competência procurada" value={skill} onChange={event => setSkill(event.target.value)} /></div>{error && <p className="form-message">{error}</p>}<div className="cards">{teams.map(team => <article className="card" key={team.id}><div><h3>{team.name}</h3><p className="meta">{team.institution} · {team.memberCount}/4 integrantes</p><p>{team.projectSummary}</p><div className="tags">{team.desiredSkills.map(item => <span key={item}>{item}</span>)}</div><button className="connect" onClick={() => request(team.id)}>Solicitar entrada</button></div></article>)}</div></section>
}

function CreateTeamForm({ session, communityId, onCreated }: { session: AuthSession; communityId: string; onCreated: () => void }) {
  const [name, setName] = useState(''); const [summary, setSummary] = useState(''); const [skills, setSkills] = useState(''); const [error, setError] = useState('')
  async function submit(event: FormEvent) { event.preventDefault(); try { await api(`/api/communities/${communityId}/teams`, { method: 'POST', body: JSON.stringify({ name, projectSummary: summary, desiredSkills: tags(skills) }) }, session.accessToken); onCreated() } catch (exception) { setError(exception instanceof ApiError ? exception.message : 'Não foi possível criar a equipe.') } }
  return <form className="inline-form" onSubmit={submit}><label>Nome da equipe<input value={name} maxLength={120} required onChange={event => setName(event.target.value)} /></label><label>Resumo do projeto<textarea value={summary} maxLength={500} onChange={event => setSummary(event.target.value)} /></label><label>Competências procuradas<input value={skills} placeholder="Design, marketing, tecnologia" onChange={event => setSkills(event.target.value)} /></label>{error && <p className="error">{error}</p>}<button className="primary">Salvar equipe</button></form>
}

function ProfileEditor({ session, communityId, institutions }: { session: AuthSession; communityId: string; institutions: Institution[] }) {
  const empty: OwnProfile = { institutionId: null, course: '', headline: '', bio: '', projectName: '', projectSummary: '', canHelpWith: '', lookingFor: '', contactUrl: '', teamSituation: 'lookingForTeam', skills: [], interests: [], visibleInDirectory: true }
  const [profile, setProfile] = useState<OwnProfile>(empty); const [skillsText, setSkillsText] = useState(''); const [interestsText, setInterestsText] = useState(''); const [message, setMessage] = useState('')
  useEffect(() => { api<OwnProfile>(`/api/communities/${communityId}/profiles/me`, {}, session.accessToken).then(value => { setProfile(value); setSkillsText(value.skills.join(', ')); setInterestsText(value.interests.join(', ')) }).catch(exception => { if (!(exception instanceof ApiError) || exception.status !== 404) setMessage(exception.message) }) }, [communityId, session.accessToken])
  async function save(event: FormEvent) { event.preventDefault(); setMessage(''); try { const payload = { ...profile, skills: tags(skillsText), interests: tags(interestsText) }; const saved = await api<OwnProfile>(`/api/communities/${communityId}/profiles/me`, { method: 'PUT', body: JSON.stringify(payload) }, session.accessToken); setProfile(saved); setMessage('Perfil salvo com sucesso.') } catch (exception) { setMessage(exception instanceof ApiError ? exception.message : 'Não foi possível salvar.') } }
  return <section className="profile-section"><form className="profile-form" onSubmit={save}><p className="eyebrow">MEU PERFIL</p><h2>Como você quer ser encontrado?</h2><label>Instituição<select value={profile.institutionId ?? ''} onChange={event => setProfile({ ...profile, institutionId: event.target.value || null })}><option value="">Selecione</option>{institutions.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select></label><label>Curso<input value={profile.course ?? ''} maxLength={120} onChange={event => setProfile({ ...profile, course: event.target.value })} /></label><label>Situação da equipe<select value={profile.teamSituation} onChange={event => setProfile({ ...profile, teamSituation: event.target.value as OwnProfile['teamSituation'] })}><option value="lookingForTeam">Estou procurando equipe</option><option value="hasTeam">Já tenho equipe</option><option value="notLooking">Não estou procurando agora</option></select></label><label>Projeto<input value={profile.projectName ?? ''} maxLength={120} onChange={event => setProfile({ ...profile, projectName: event.target.value })} /></label><label>Resumo do projeto<textarea value={profile.projectSummary ?? ''} maxLength={500} onChange={event => setProfile({ ...profile, projectSummary: event.target.value })} /></label><label>Competências, separadas por vírgula<input value={skillsText} onChange={event => setSkillsText(event.target.value)} /></label><label>Interesses, separados por vírgula<input value={interestsText} onChange={event => setInterestsText(event.target.value)} /></label><label>Posso ajudar com<textarea value={profile.canHelpWith ?? ''} maxLength={300} onChange={event => setProfile({ ...profile, canHelpWith: event.target.value })} /></label><label>Estou procurando<textarea value={profile.lookingFor ?? ''} maxLength={300} onChange={event => setProfile({ ...profile, lookingFor: event.target.value })} /></label><label>Sobre você<textarea value={profile.bio ?? ''} maxLength={800} onChange={event => setProfile({ ...profile, bio: event.target.value })} /></label><label>Link de contato<input type="url" value={profile.contactUrl ?? ''} maxLength={300} onChange={event => setProfile({ ...profile, contactUrl: event.target.value })} /></label><label className="check"><input type="checkbox" checked={profile.visibleInDirectory} onChange={event => setProfile({ ...profile, visibleInDirectory: event.target.checked })} /> Mostrar meu perfil no diretório</label>{message && <p className="form-message">{message}</p>}<button className="primary">Salvar perfil</button></form></section>
}

const tags = (value: string) => value.split(',').map(item => item.trim()).filter(Boolean).slice(0, 10)
const initials = (name: string) => name.split(' ').map(item => item[0]).slice(0, 2).join('')
