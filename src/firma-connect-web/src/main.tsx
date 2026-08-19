import { createRoot } from 'react-dom/client'
import './styles.css'

type Member = {
  name: string
  course: string
  institution: string
  headline: string
  skills: string[]
  availableForTeam: boolean
}

const members: Member[] = [
  { name: 'Bianca Iolanda', course: 'Pedagogia', institution: 'Sua instituição', headline: 'Educação inclusiva e validação de problemas', skills: ['Educação', 'Pesquisa'], availableForTeam: true },
  { name: 'Lucas Martins', course: 'Administração', institution: 'Sua instituição', headline: 'Estratégia e modelo de negócio', skills: ['Negócios', 'Marketing'], availableForTeam: true },
  { name: 'Ana Souza', course: 'Design', institution: 'Outra instituição', headline: 'Produto e experiência do usuário', skills: ['UX/UI', 'Branding'], availableForTeam: false }
]

function App() {
  return (
    <main className="page-shell">
      <nav className="nav"><a className="brand" href="#inicio">firma<span>.</span></a><span className="community">Piloto Comunidades</span><button className="ghost">Entrar</button></nav>
      <section className="hero" id="inicio">
        <p className="eyebrow">CONEXÕES QUE MOVEM PROJETOS</p>
        <h1>Encontre as pessoas certas na sua comunidade.</h1>
        <p className="lead">Descubra colegas da sua instituição, competências complementares e pessoas abertas a formar equipe — sem perder a conversa no grupo.</p>
        <div className="actions"><button className="primary">Entrar com convite</button><a href="#diretorio">Ver como funciona</a></div>
      </section>
      <section className="directory" id="diretorio">
        <div><p className="eyebrow">DIRETÓRIO</p><h2>Comece pela sua instituição.</h2></div>
        <div className="filters"><input aria-label="Buscar" placeholder="Busque por nome ou competência" /><select aria-label="Instituição"><option>Todas as instituições</option><option>Sua instituição</option></select><label><input type="checkbox" defaultChecked /> Disponível para equipe</label></div>
        <div className="cards">{members.map(member => <article className="card" key={member.name}><div className="avatar">{member.name.split(' ').map(n => n[0]).slice(0, 2).join('')}</div><div><h3>{member.name}</h3><p className="meta">{member.course} · {member.institution}</p><p>{member.headline}</p><div className="tags">{member.skills.map(skill => <span key={skill}>{skill}</span>)}</div>{member.availableForTeam && <button className="connect">Conectar</button>}</div></article>)}</div>
      </section>
    </main>
  )
}

createRoot(document.getElementById('root')!).render(<App />)
