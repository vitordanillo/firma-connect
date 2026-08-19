# Deploy na VPS Windows sem Docker

O deploy de produção pode ser executado na VPS Windows Server 2022 sem Docker. O GitHub Actions conecta por SSH usando o usuário restrito `firma` e executa `infra/windows/deploy.ps1`.

## Segredos do GitHub

Configure os seguintes secrets no repositório:

- `VPS_HOST`: IP ou domínio da VPS.
- `VPS_PORT`: porta SSH, normalmente `22`.
- `VPS_USER`: `firma`.
- `VPS_PASSWORD`: senha do usuário `firma`.

A senha nunca deve ser adicionada ao repositório ou enviada em mensagens.

## Pré-requisitos da VPS

Antes do primeiro deploy, a VPS precisa ter:

- .NET 8 SDK;
- Node.js LTS e npm;
- PostgreSQL;
- Caddy configurado para servir `C:\opt\firma-connect\deploy\web` e encaminhar a API para o Kestrel;
- o repositório clonado em `C:\opt\firma-connect`;
- PM2 gerenciando um processo chamado `firma-api` para executar a API publicada;
- um arquivo `.env` de produção em `C:\opt\firma-connect`.

O workflow não cria serviços, instala dependências nem cria credenciais. Essas etapas são deliberadamente separadas para evitar que uma execução automática altere a infraestrutura sem revisão.

## Fluxo

Cada push na branch `main` executa:

1. conexão SSH autenticada por senha;
2. atualização do repositório para `origin/main`;
3. publicação da API com `dotnet publish`;
4. instalação das dependências e build do frontend;
5. cópia do frontend para o diretório servido pelo Caddy;
6. reinício do processo `firma-api` no PM2.

O uso de senha é compatível com a VPS atual, mas uma chave SSH dedicada continua sendo a alternativa recomendada para reduzir exposição da porta SSH.
