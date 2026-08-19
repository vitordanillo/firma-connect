# Firma Connect

> Plataforma open source de networking e formação de equipes para comunidades, criada inicialmente para validar o contexto do Sebrae Supernova.

Para retomar o projeto, leia [`docs/STATUS_E_DESENVOLVIMENTO.md`](docs/STATUS_E_DESENVOLVIMENTO.md), que registra o escopo, a arquitetura em produção, o histórico de decisões e os próximos passos.

Plataforma de descoberta profissional para comunidades. Este repositório inicia o piloto que poderá ser apresentado aos administradores de uma comunidade de estudantes; não é um produto oficial do Sebrae ou do Supernova.

## O problema

Em uma comunidade grande de WhatsApp, mensagens como “quem é da minha faculdade?” ou “alguém entende de marketing?” desaparecem rapidamente. Pessoas que poderiam formar equipes válidas ou colaborar nunca se encontram.

O Firma Connect transforma essas perguntas em perfis pesquisáveis e pedidos de conexão: instituição, curso, competências, interesses, projeto e disponibilidade para equipe.

## Escopo do MVP

- Perfil profissional enxuto e opt-in.
- Busca e filtros por instituição, curso, competências e interesses.
- Diretório de pessoas disponíveis para formar equipe.
- Pedido de conexão com aceite/recusa.
- Administração de uma comunidade por convite.

Mensagens privadas, recomendações por IA, feed, pagamento e integração com WhatsApp estão explicitamente fora do MVP.

## Estrutura

```
src/Firma.Connect.Api       API ASP.NET Core 8 + EF Core + JWT
src/firma-connect-web       React + TypeScript + Vite
tests/Firma.Connect.Api.Tests testes unitários da API
infra/postgres              esquema inicial PostgreSQL
docs                        decisões de produto e arquitetura
```

## Desenvolvimento e VPS

O projeto foi apenas estruturado neste ambiente; nenhum serviço, teste ou build foi executado. Consulte [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) para a arquitetura, [docs/AUTHENTICATION.md](docs/AUTHENTICATION.md) para o fluxo de acesso, [docs/ROADMAP.md](docs/ROADMAP.md) para o andamento e [docs/PRODUCT.md](docs/PRODUCT.md) para preservar o contexto do produto.

Antes de publicar, configure segredos fora do Git com base em `.env.example`, use HTTPS por proxy reverso e substitua a autenticação de desenvolvimento por um provedor de e-mail/OTP real.
