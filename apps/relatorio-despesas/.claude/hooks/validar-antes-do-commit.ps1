# Hook do Claude Code (PreToolUse): antes de qualquer "git commit", roda os testes e o cdk synth
# (que inclui cdk-nag e as regras da plataforma). Se falhar, o commit é BLOQUEADO (exit 2)
# e a mensagem volta para o Claude corrigir.
$ErrorActionPreference = 'Stop'
$entrada = [Console]::In.ReadToEnd() | ConvertFrom-Json
$comando = $entrada.tool_input.command
if (-not $comando -or $comando -notmatch '\bgit\s+commit\b') { exit 0 }

$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $raiz
try {
    Write-Host "Infra-Aws: validando antes do commit (testes + cdk synth)..."
    dotnet test backend/tests/App.Backend.Testes --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine("Commit bloqueado: os testes falharam. Corrija antes de commitar.")
        exit 2
    }
    Push-Location infra
    try {
        cdk synth -q -c ambiente=sandbox | Out-Null
        if ($LASTEXITCODE -ne 0) {
            [Console]::Error.WriteLine("Commit bloqueado: cdk synth falhou (regras da plataforma ou cdk-nag). Veja os erros [Infra-Aws] / AwsSolutions-*.")
            exit 2
        }
    } finally { Pop-Location }
} finally { Pop-Location }
exit 0
