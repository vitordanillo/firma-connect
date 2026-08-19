# Roadmap do MVP

## Concluído

- contexto, escopo e arquitetura;
- modelo PostgreSQL inicial;
- diretório filtrável por comunidade e instituição;
- interface conceitual do diretório;
- autenticação por e-mail e senha a partir de convite;
- autorização de membro e administrador por comunidade.

## Próximos incrementos

1. CRUD do próprio perfil e catálogo de instituições.
2. Integração do frontend com registro, login e diretório da API.
3. Solicitações de conexão com aceite, recusa e liberação de contato.
4. Migrações EF Core e bootstrap seguro do primeiro administrador.
5. Rate limiting, recuperação de senha e serviço de e-mail.
6. Testes de integração com PostgreSQL e testes ponta a ponta.
7. CI no GitHub Actions, proxy HTTPS e rotina de backup para VPS.
8. Exportação/exclusão de dados e materiais de consentimento LGPD.
