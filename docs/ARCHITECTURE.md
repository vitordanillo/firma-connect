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

- JWT de curta duração; refresh token armazenado com hash quando a autenticação for implementada.
- Senhas nunca entram no banco sem hash (Argon2id ou ASP.NET Identity).
- Rate limit em login, busca e pedidos de conexão.
- Autorizações sempre verificam `community_id`; nunca confiar em IDs vindos do cliente.
- Logs sem e-mail, telefone ou conteúdo de notas.
- LGPD: consentimento claro, exportação/remoção de dados e política de retenção antes do piloto público.

## Contrato HTTP inicial

`GET /health` saúde do serviço.

`GET /api/communities/{communityId}/profiles?institutionId=&availableForTeam=&query=&page=&pageSize=` lista perfis visíveis no diretório. O primeiro endpoint foi implementado, mas ainda precisa de autenticação e verificação de associação à comunidade antes de produção.

`POST /api/communities/{communityId}/profiles` cria ou atualiza o próprio perfil.

`POST /api/connection-requests` envia pedido; `POST /api/connection-requests/{id}/accept` aceita; `POST /api/connection-requests/{id}/decline` recusa.

Os endpoints de perfil e conexão são o primeiro recorte. Autorização e fluxo de convite devem ser implementados antes da exposição pública.

## Estratégia de testes

- Unitários: regras de conexão, validações e normalização de busca.
- Integração: API + PostgreSQL em container efêmero no CI.
- Frontend: componentes e fluxos com Vitest/Testing Library.
- E2E: Playwright para cadastro, filtro por instituição e aceite de conexão.
- Segurança: teste de isolamento entre comunidades em cada endpoint novo.

## Execução posterior

Nada foi executado neste ambiente por decisão do projeto. Em uma máquina de desenvolvimento, instalar .NET SDK 8, Node 20+ e Docker; restaurar dependências, aplicar uma migração EF gerada a partir do modelo e então subir o Compose. Na VPS, colocar API e web atrás de Caddy ou Nginx com TLS, limitar as portas publicamente expostas a 80/443 e fazer backup diário do volume PostgreSQL.
