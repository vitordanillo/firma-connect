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
    if (Test-Path -LiteralPath '.\package-lock.json') {
        npm ci
    }
    else {
        npm install
    }
    npm run build
}
finally {
    Pop-Location
}

Get-ChildItem -LiteralPath '.\src\firma-connect-web\dist' -Force |
    Copy-Item -Destination $WebPublishPath -Recurse -Force

if (-not (Get-Command pm2 -ErrorAction SilentlyContinue)) {
    throw 'PM2 não foi encontrado na VPS. Instale-o antes do deploy.'
}

pm2 describe firma-api *> $null
if ($LASTEXITCODE -eq 0) {
    pm2 restart firma-api --update-env
}
else {
    pm2 start 'C:\Program Files\dotnet\dotnet.exe' `
        --name firma-api `
        --interpreter none `
        -- 'C:\opt\firma-connect\deploy\api\Firma.Connect.Api.dll' --urls http://127.0.0.1:8080
}
pm2 save

Write-Host 'Deploy concluído com sucesso.'
