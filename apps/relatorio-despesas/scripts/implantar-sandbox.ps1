<#
.SYNOPSIS
  Implanta esta aplicação no SANDBOX (nunca em dev/prod — esses só pelo pipeline).
.DESCRIPTION
  1) roda testes; 2) mostra o cdk diff; 3) pede confirmação; 4) cdk deploy -c ambiente=sandbox.
  Requer login no sandbox:  aws sso login --profile infra-aws-sandbox
#>
param([string]$Perfil = 'infra-aws-sandbox')
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot

$identidade = aws sts get-caller-identity --profile $Perfil --output json | ConvertFrom-Json
Write-Host "Conta: $($identidade.Account)  Identidade: $($identidade.Arn)"

dotnet test (Join-Path $raiz 'backend/tests/App.Backend.Testes') --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Testes falharam.' }

Push-Location (Join-Path $raiz 'infra')
try {
    cdk diff --profile $Perfil -c ambiente=sandbox
    $resposta = Read-Host 'Implantar no SANDBOX? Isso cria/altera recursos AWS (custo baixo). Digite SIM para continuar'
    if ($resposta -ne 'SIM') { Write-Host 'Cancelado.'; exit 0 }
    cdk deploy --profile $Perfil -c ambiente=sandbox --require-approval broadening
} finally { Pop-Location }
