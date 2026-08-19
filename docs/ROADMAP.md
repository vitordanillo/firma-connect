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

## Próximos incrementos

1. Solicitações de conexão com aceite, recusa e liberação de contato.
2. Migrações EF Core e bootstrap seguro do primeiro administrador.
3. Rate limiting, recuperação de senha e serviço de e-mail.
4. Testes de integração com PostgreSQL e testes ponta a ponta.
5. CI no GitHub Actions, proxy HTTPS e rotina de backup para VPS.
6. Exportação/exclusão de dados e materiais de consentimento LGPD.
