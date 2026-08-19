# Roadmap do MVP

## Concluído

- contexto, escopo e arquitetura;
- modelo PostgreSQL inicial;
- diretório filtrável por comunidade e instituição;
- interface conceitual do diretório;
- autenticação por e-mail e senha a partir de convite;
- autorização de membro e administrador por comunidade.
- CRUD do próprio perfil e catálogo pesquisável de instituições;
- frontend integrado com registro, login, perfil e diretório da API.
- perfil estruturado com projeto, competências, interesses, oferta e necessidade;
- descoberta que prioriza participantes da mesma instituição;
- equipes abertas com limite de quatro integrantes;
- solicitação de entrada com aceite e recusa pelo responsável.

## Próximos incrementos

1. Painel do responsável para visualizar e responder solicitações de equipe no frontend.
2. Solicitações de conexão para networking além das equipes.
3. Migrações EF Core e bootstrap seguro do primeiro administrador.
4. Rate limiting, recuperação de senha e serviço de e-mail.
5. Testes de integração com PostgreSQL e testes ponta a ponta.
6. CI no GitHub Actions, proxy HTTPS e rotina de backup para VPS.
7. Exportação/exclusão de dados e materiais de consentimento LGPD.
