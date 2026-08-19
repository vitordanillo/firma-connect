# Arquitetura técnica

## Decisões iniciais

| Camada | Escolha | Motivo |
|---|---|---|
| API | ASP.NET Core 8 / C# | Ecossistema sólido, tipagem e afinidade da equipe. |
| Dados | PostgreSQL 16 | Relacional, confiável e adequado a filtros/indexação. |
| ORM | Entity Framework Core + Npgsql | Migrações, mapeamento e testes com SQLite. |
| Web | React 18 + TypeScript + Vite | Interface rápida, simples de evoluir e bem tipada. |
| Deploy | Docker Compose + Nginx/Caddy na VPS | Serviços isolados e publicação reproduzível. |

## Modelo de dados

`Community` agrupa participantes; `Membership` aplica o isolamento de comunidade. `Profile` contém os dados opt-in da pessoa. Instituições, competências e interesses são normalizados para permitir busca e evitar grafias duplicadas. `ConnectionRequest` registra a conexão e sua transição de estado.

O banco usa UUIDs, `timestamptz`, `citext` para e-mails e índices nas colunas de filtro. Contato é separado do perfil público para ser liberado apenas depois do aceite.

## Segurança e privacidade

- JWT de curta duração e senha protegida pelo `PasswordHasher` do ASP.NET Core.
- Cadastro somente por convite de uso único, associado a um e-mail e armazenado apenas como hash SHA-256.
- Criação de convites restrita a administradores da comunidade.
- Senhas nunca entram no banco sem hash (Argon2id ou ASP.NET Identity).
- Rate limit em login, busca e pedidos de conexão.
- Autorizações sempre verificam `community_id`; nunca confiar em IDs vindos do cliente.
- Logs sem e-mail, telefone ou conteúdo de notas.
- LGPD: consentimento claro, exportação/remoção de dados e política de retenção antes do piloto público.

## Contrato HTTP inicial

`GET /health` saúde do serviço.

`POST /api/auth/register` cria a conta a partir de um convite válido e associa o usuário à comunidade.

`POST /api/auth/login` autentica e retorna um JWT com duração configurável.

`POST /api/communities/{communityId}/invitations` cria um convite de três dias; exige administrador autenticado.

`GET /api/communities/{communityId}/profiles?institutionId=&teamSituation=&skill=&interest=&query=` lista perfis visíveis e prioriza a instituição do usuário.

`GET|PUT|DELETE /api/communities/{communityId}/profiles/me` consulta, salva ou remove o próprio perfil.

`GET /api/institutions?query=&page=&pageSize=` pesquisa o catálogo normalizado de instituições.

`GET|POST /api/communities/{communityId}/teams` pesquisa equipes abertas ou cria uma equipe vinculada à instituição do responsável.

`GET /api/communities/{communityId}/team-discovery/summary` resume participantes, pessoas procurando equipe e equipes abertas da instituição do usuário.

`POST /api/communities/{communityId}/teams/{teamId}/requests` solicita entrada em uma equipe.

`GET /api/communities/{communityId}/teams/{teamId}/requests` lista solicitações para o responsável.

`POST /api/communities/{communityId}/team-requests/{requestId}/accept|decline` responde à solicitação.

`POST /api/connection-requests` envia pedido; `POST /api/connection-requests/{id}/accept` aceita; `POST /api/connection-requests/{id}/decline` recusa.

O diretório e a criação de convites verificam a associação à comunidade no banco em cada requisição. O token não concede acesso a uma comunidade por si só.

As regras de equipe verificam no backend: mesma instituição, máximo de quatro integrantes, vaga aberta, responsável autorizado e ausência de outra equipe na mesma comunidade.

## Estratégia de testes

- Unitários: regras de conexão, validações e normalização de busca.
- Integração: API + PostgreSQL em container efêmero no CI.
- Frontend: componentes e fluxos com Vitest/Testing Library.
- E2E: Playwright para cadastro, filtro por instituição e aceite de conexão.
- Segurança: teste de isolamento entre comunidades em cada endpoint novo.

## Execução posterior

Nada foi executado neste ambiente por decisão do projeto. Em uma máquina de desenvolvimento, instalar .NET SDK 8, Node 20+ e Docker; restaurar dependências, aplicar uma migração EF gerada a partir do modelo e então subir o Compose. Na VPS, colocar API e web atrás de Caddy ou Nginx com TLS, limitar as portas publicamente expostas a 80/443 e fazer backup diário do volume PostgreSQL.
