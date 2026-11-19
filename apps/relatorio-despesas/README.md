# Template de aplicação Infra-Aws (`template-app/`)

Modelo que **toda aplicação nova copia**. Quem copia é o Portal Infra-Aws: ao aprovar um pedido, ele abre um PR criando `apps/<nome>/` com estes arquivos e um `app.json` próprio. Ninguém precisa copiar manualmente.

```
app.json                  governança (gerado pelo Portal; aqui, valores de exemplo)
CLAUDE.md                 regras para o Claude Code de quem desenvolve a app
.claude/settings.json     permissões (bloqueia deploy direto, IAM, Organizations) + hook
.claude/hooks/            validação antes do commit (testes + cdk synth)
.claude/skills/           skill "nova-app-interna"
infra/                    CDK: PilhaAppInterna + AppWebDados (nada além da biblioteca)
backend/src/App.Backend/  API C#: lançamentos, importação CSV e relatório
backend/tests/            testes da transformação
frontend/                 telas: lançamentos, importar CSV, relatório
scripts/implantar-sandbox.ps1
```

O exemplo já implementa o caso pedido pelo piloto: **entrada de dados → transformação → relatório em tela**, com lançamentos por mês, importação de CSV, consolidação por categoria e por dia.

## Comandos
```powershell
dotnet test backend/tests/App.Backend.Testes
cd infra; cdk synth -c ambiente=sandbox
powershell -File scripts/implantar-sandbox.ps1    # só sandbox, com confirmação
```
