# CLAUDE.md — Aplicação interna (plataforma Infra-Aws)

Você está ajudando um colaborador a construir uma **aplicação interna** sobre a plataforma **Infra-Aws**. Responda sempre em **português do Brasil**. O usuário pode não ser desenvolvedor: explique as decisões em linguagem simples.

## O que esta aplicação é
- Ela nasceu de uma solicitação **aprovada no Portal Infra-Aws**. O `app.json` foi gerado pelo Portal e contém os dados de governança (nome, dono, centro de custo, criticidade, classificação dos dados). **Nunca edite o `app.json`.** Qualquer mudança nele (criticidade, dados, orçamento) exige um novo pedido no Portal.
- Padrão: **AppWebDados**, ou seja, tela de entrada de dados + transformação + relatório em tela.
  - `frontend/`: site estático (JavaScript puro, sem framework).
  - `backend/src/App.Backend/`: API em C# (.NET 10) numa Lambda.
  - `infra/`: CDK em C# que **só** usa os padrões da biblioteca `Marutech.InfraAws.Cdk`.

## Regras obrigatórias (normas da plataforma)
1. **Não crie infraestrutura fora da biblioteca.** Nada de `new Bucket`, `new Function`, VPC, EC2, RDS, usuário IAM ou chave de acesso. Precisa de algo que não existe? Oriente o usuário a pedir no Portal (alteração ou exceção, N-07).
2. **Toda API é autenticada.** Não remova o autorizador JWT nem crie rotas públicas.
3. **Deploy em dev/prod só pelo pipeline** (pull request → revisão → merge). Você pode, no máximo, rodar `scripts/implantar-sandbox.ps1`, com confirmação do usuário.
4. **Dados:** respeite a classificação do `app.json`.
   - Não use dados reais de produção para testes.
   - Nunca coloque dados pessoais (CPF, salário, saúde) em logs.
   - Não peça para o usuário colar dados reais na conversa: use dados fictícios.
5. **Segredos:** nunca grave senhas, tokens ou chaves em arquivos. Se precisar, use o SSM Parameter Store via biblioteca e peça orientação à Plataforma.
6. **Front-end seguro:** monte telas com a função `h()` de `js/ui.js` (texto via `textContent`). **Nunca** use `innerHTML` com dados, `eval` ou scripts/estilos inline (a CSP bloqueia).
7. **Bibliotecas:** as Lambdas rodam em **ARM64 (AWS Graviton)**. Antes de adicionar um pacote NuGet com código nativo (PDF, imagem, drivers), confirme que ele suporta `linux-arm64`. Se não suportar, o usuário precisa pedir exceção no Portal para usar x86.
8. **Testes:** toda regra de negócio fica em `Transformacao.cs` (ou em classes puras semelhantes) e precisa de teste em `backend/tests/`.

## Onde colocar cada coisa
| Quero... | Arquivo |
|---|---|
| Mudar campos, validações ou cálculos | `backend/src/App.Backend/Transformacao.cs` + testes |
| Criar uma rota de API | `backend/src/App.Backend/Funcao.cs` (sempre sob `/api/`) |
| Mudar telas | `frontend/js/app.js` |
| Mudar a infraestrutura | Normalmente nada. Se for indispensável, só com padrões da biblioteca, em `infra/src/App.Infra/Program.cs` |

## Antes de concluir qualquer tarefa
```powershell
dotnet test backend/tests/App.Backend.Testes
cd infra; cdk synth -c ambiente=sandbox   # valida tags, cdk-nag e regras da plataforma
```
O hook do projeto roda essas validações automaticamente antes de cada `git commit`.

## Quem procurar
Dúvidas sobre a plataforma, exceções ou custos: abra uma solicitação de **Suporte** no Portal Infra-Aws.
