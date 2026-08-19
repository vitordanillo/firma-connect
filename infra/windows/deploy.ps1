param(
    [string]$RepoPath = 'C:\opt\firma-connect',
    [string]$ApiPublishPath = 'C:\opt\firma-connect\deploy\api',
    [string]$WebPublishPath = 'C:\opt\firma-connect\deploy\web',
    [string]$ApiServiceName = 'FirmaConnectApi'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Set-Location -LiteralPath $RepoPath

git fetch origin main
git reset --hard origin/main

if (-not (Test-Path -LiteralPath '.env')) {
    throw 'O arquivo .env não existe na VPS. Crie-o com as configurações de produção antes do deploy.'
}

New-Item -ItemType Directory -Force -Path $ApiPublishPath, $WebPublishPath | Out-Null

dotnet publish '.\src\Firma.Connect.Api\Firma.Connect.Api.csproj' `
    --configuration Release `
    --output $ApiPublishPath `
    --no-restore

Push-Location '.\src\firma-connect-web'
try {
    npm ci
    npm run build
}
finally {
    Pop-Location
}

Get-ChildItem -LiteralPath '.\src\firma-connect-web\dist' -Force |
    Copy-Item -Destination $WebPublishPath -Recurse -Force

if (-not (Get-Service -Name $ApiServiceName -ErrorAction SilentlyContinue)) {
    throw "O serviço '$ApiServiceName' não existe. Instale o serviço da API antes do primeiro deploy."
}

Restart-Service -Name $ApiServiceName -Force

Write-Host 'Deploy concluído com sucesso.'
