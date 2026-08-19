# Firma Connect — status e continuidade do desenvolvimento

Este documento é o ponto de retomada do projeto. Ele registra o que foi construído, como o piloto está hospedado e quais são os próximos passos.

## 1. Contexto do produto

O Firma Connect nasceu para resolver um problema observado no grupo do Sebrae Supernova: pessoas da mesma comunidade têm dificuldade para descobrir quem estuda na mesma instituição, quem possui determinada competência e quem está procurando ou formando uma equipe.

O WhatsApp é bom para comunicação rápida, mas as mensagens se perdem e não existe um diretório estruturado. O Firma Connect organiza essa descoberta em perfis, filtros e equipes.

O Supernova é o primeiro contexto de validação. O produto não deve usar a marca Sebrae ou a identidade oficial do programa sem autorização.

## 2. Escopo atual

- comunidades isoladas;
- autenticação por convite;
- login com e-mail e senha;
- perfis profissionais por comunidade;
- instituição, curso, projeto, resumo, competências, interesses e formas de contato;
- situação da equipe: procurando equipe, já possui equipe ou não está procurando;
- diretório de pessoas com busca por nome/projeto/necessidade, competência e instituição;
- priorização de pessoas da mesma instituição;
- diretório de equipes abertas;
- criação de equipes;
- equipes limitadas a quatro integrantes;
- regra de mesma instituição na equipe;
- solicitações para entrar em equipe;
- painel do responsável para aceitar ou recusar solicitações;
- PostgreSQL;
- frontend React/Vite;
- API ASP.NET Core 8;
- execução sem Docker na VPS Windows Server 2022;
- Caddy como proxy reverso e servidor de arquivos;
- PM2 para manter Caddy e API ativos;
- deploy automático pelo GitHub Actions.

## 3. Arquitetura atual

```text
Internet
  ├─ InclusivaEdu: Caddy → serviços existentes
  └─ Firma Connect: Caddy :8081
       ├─ arquivos estáticos → C:\opt\firma-connect\deploy\web
       └─ /api/* → Kestrel 127.0.0.1:8080 → PM2 firma-api

PostgreSQL 16 → banco firma_connect
```

O Caddy existente fica em:

```text
C:\Users\Administrator\Desktop\inclusivaedu\caddy_new.exe
C:\Users\Administrator\Desktop\inclusivaedu\Caddyfile
```

O piloto está acessível por IP:

```text
http://151.243.24.208:8081
```

O uso de domínio próprio e HTTPS para o Firma Connect fica para uma etapa posterior.

## 4. Estrutura do repositório

- `src/Firma.Connect.Api`: API, domínio, autenticação, perfis e equipes.
- `src/firma-connect-web`: interface React/Vite.
- `infra/postgres/001_initial.sql`: schema inicial do PostgreSQL.
- `infra/windows/deploy.ps1`: atualização, publicação e reinício pelo PM2.
- `.github/workflows/deploy-windows.yml`: deploy automático via SSH com senha armazenada em Secrets.
- `docs/`: visão do produto, arquitetura, autenticação, roadmap e operação.

## 5. Estado da VPS

Ambiente confirmado:

- Windows Server 2022 Standard;
- Git 2.54;
- Node.js 24 e npm 11;
- PostgreSQL 16 ativo;
- .NET 8 SDK/Runtime;
- Caddy gerenciado pelo PM2;
- API `firma-api` gerenciada pelo PM2;
- Apache do XAMPP deve permanecer parado para não disputar as portas 80/443;
- IIS foi instalado, mas não faz parte da arquitetura final e deve permanecer parado/desabilitado.

Processos PM2 relevantes:

```text
caddy-server
firma-api
```

Comandos úteis:

```powershell
pm2 status
pm2 logs firma-api --lines 50
pm2 logs caddy-server --lines 50
pm2 save
```

## 6. Banco de dados

Banco:

```text
Host: localhost
Porta: 5432
Database: firma_connect
Usuário: firma_connect
```

O schema foi aplicado por `infra/postgres/001_initial.sql`. A tabela `institutions` precisa ser abastecida antes de o usuário preencher o perfil.

Exemplo:

```sql
INSERT INTO institutions (id, name, normalized_name)
VALUES (gen_random_uuid(), 'Nome da instituição', 'nome da instituição')
ON CONFLICT (normalized_name) DO NOTHING;
```

## 7. Primeiro acesso

O primeiro administrador foi criado pelo bootstrap temporário da API, usando as variáveis `Bootstrap__...` no arquivo PM2. Essas variáveis devem ser removidas depois do primeiro login:

```powershell
pm2 delete firma-api
pm2 start C:\opt\firma-connect\ecosystem.config.cjs
pm2 save
```

Não versionar `ecosystem.config.cjs` com credenciais reais. Senhas e chaves devem ficar apenas na VPS ou nos Secrets do GitHub.

## 8. Deploy atual

Secrets configurados no GitHub:

```text
VPS_HOST
VPS_PORT
VPS_USER
VPS_PASSWORD
```

Cada push na `main` aciona o workflow. Ele conecta por SSH, atualiza o clone em `C:\opt\firma-connect`, publica a API, compila o frontend, copia `dist` para `deploy\web` e reinicia `firma-api` no PM2.

O deploy usa senha porque a autenticação por chave SSH da VPS apresentou problemas de resolução do perfil Windows. A alternativa recomendada para produção continua sendo uma chave SSH dedicada.

## 9. Histórico de decisões importantes

- Docker não foi usado porque a produção está em Windows Server 2022 e o Docker Desktop não é suportado nesse sistema.
- IIS não será usado para não conflitar com o Caddy/Apache do InclusivaEdu.
- O frontend do Firma Connect foi colocado na porta 8081 para não disputar a porta 80 do InclusivaEdu.
- A API roda em HTTP local (`127.0.0.1:8080`) e o Caddy faz o proxy.
- O Caddy existente é a entrada principal nas portas 80 e 443.
- O Apache do XAMPP precisa ficar parado enquanto o Caddy estiver ativo.
- O bootstrap inicial foi criado apenas para facilitar o primeiro administrador e deve ser removido após o uso.

## 10. Problemas conhecidos

- A mensagem de erro do frontend ainda é genérica (`Não foi possível concluir a solicitação.`); os próximos incrementos devem melhorar o tratamento e a observabilidade das requisições.
- Ainda não existe tela administrativa para cadastrar instituições, comunidades e convites.
- Ainda não existe fluxo visual para o administrador criar convites.
- O deploy por senha funciona, mas é menos seguro que chave SSH.
- O piloto usa IP e porta 8081, sem domínio e HTTPS próprio.
- Não há monitoramento, backup automatizado do PostgreSQL ou estratégia de rollback formal.
- O projeto ainda precisa de migrations versionadas em vez de depender somente do SQL inicial.

## 11. Próximos passos prioritários

### Produto

1. Criar tela de administração da comunidade.
2. Criar geração e revogação de convites.
3. Cadastrar instituições pela interface.
4. Melhorar mensagens de erro e estados vazios.
5. Adicionar conexões diretas entre participantes.
6. Adicionar notificações de solicitações de equipe.
7. Validar o fluxo com participantes reais do Supernova.

### Engenharia

1. Adicionar testes de integração da API com PostgreSQL.
2. Adicionar testes do frontend para login, perfil e diretório.
3. Criar migrations versionadas.
4. Adicionar health check público e logs estruturados.
5. Configurar backup diário do PostgreSQL.
6. Criar rollback do deploy para o último commit funcional.
7. Migrar o deploy de senha para chave SSH dedicada.
8. Remover o bootstrap da aplicação após o primeiro uso ou protegê-lo por uma ferramenta administrativa offline.

### Operação

1. Definir um domínio para o piloto.
2. Configurar HTTPS próprio pelo Caddy.
3. Documentar recuperação após reinicialização da VPS.
4. Garantir que PM2 restaure os processos após reboot.
5. Não iniciar Apache/XAMPP enquanto Caddy estiver usando 80/443.

## 12. Convenção de commits

Mensagens em português com prefixos técnicos em inglês:

```text
feat: adicionar recurso
fix: corrigir comportamento
test: adicionar cobertura
docs: atualizar documentação
chore: ajustar infraestrutura
```

## 13. Como retomar na faculdade

```powershell
git clone https://github.com/vitordanillo/firma-connect.git
cd firma-connect
```

Leia nesta ordem:

1. `README.md`;
2. `docs/PRODUCT.md`;
3. `docs/ARCHITECTURE.md`;
4. `docs/AUTHENTICATION.md`;
5. `docs/ROADMAP.md`;
6. este arquivo;
7. `infra/windows/deploy.ps1`.

Antes de alterar a produção, desenvolva e valide localmente ou em uma VPS de testes. Nunca versionar senhas, chaves privadas, tokens JWT ou arquivos `.env` reais.
