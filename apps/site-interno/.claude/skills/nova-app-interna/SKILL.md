---
name: nova-app-interna
description: Use quando o usuário quiser criar uma nova aplicação interna ou começar a desenvolver a que acabou de ser aprovada no Portal Infra-Aws (ex.: "quero um app para controlar pedidos de compra"). Guia desde o pedido no Portal até o primeiro pull request.
---

# Nova aplicação interna (plataforma Infra-Aws)

## 1. A aplicação já existe?
- Verifique se há um `app.json` na pasta atual (ou em `apps/<nome>/`).
- **Não existe:** explique ao usuário que toda aplicação nasce de um pedido no **Portal Infra-Aws** (`https://infra-aws.marutech.com.br`). Ajude a preencher o pedido:
  - **Nome:** kebab-case, curto.
  - **Processo:** o que automatiza, quem usa, com que frequência.
  - **Padrão:** quase sempre `AppWebDados`.
  - **Classificação dos dados:** na dúvida, a mais alta.
  - **Criticidade:** Departamental, se várias pessoas dependem do app.

  Depois da aprovação, o Portal abre um pull request com a pasta da app. O usuário deve fazer checkout dessa branch. **Pare aqui** até isso acontecer.
- **Existe:** siga para o passo 2.

## 2. Entender o processo
Faça no máximo 5 perguntas objetivas:
- Quais dados entram (campos, tipos, obrigatórios)?
- Que transformação ou cálculo é feito?
- Que relatório aparece na tela (agrupamentos, totais, filtros)?
- Haverá importação de planilha (CSV)?
- Quem pode ver os dados?

## 3. Implementar, nesta ordem
1. `backend/src/App.Backend/Transformacao.cs`: modelo, validações e cálculos (código puro).
2. `backend/tests/App.Backend.Testes`: um teste por regra. Rode `dotnet test`.
3. `backend/src/App.Backend/Funcao.cs`: rotas `/api/...` usando a `Transformacao`.
4. `frontend/js/app.js`: telas usando `h()`, `tabela()` e `campo()` de `ui.js`.
5. **Não altere** `infra/`, salvo pedido explícito e só com padrões da biblioteca.

## 4. Validar
```powershell
dotnet test backend/tests/App.Backend.Testes
cd infra; cdk synth -c ambiente=sandbox
```
Corrija qualquer erro `[Infra-Aws]` ou `AwsSolutions-*`. Não suprima regras do cdk-nag.

## 5. Testar no sandbox (opcional, com confirmação)
`powershell -File scripts/implantar-sandbox.ps1`: mostra o `cdk diff` e pede confirmação antes de implantar.

## 6. Entregar
Faça commits pequenos e em português (o hook valida antes de cada commit). Depois, faça push da branch e oriente o usuário a acompanhar o PR: o pipeline mostra o diff, um revisor aprova, o merge implanta em dev, e prod exige aprovação.
