# Infra-Aws-Apps

Aplicações internas criadas pelo **Portal Infra-Aws** sobre a infraestrutura padronizada da plataforma.
A plataforma (fundação, biblioteca de constructs e Portal) fica no repositório `Infra-Aws` (privado); aqui ficam só as aplicações (ADR-026).

```
template-app/        modelo que o Portal copia para cada aplicação aprovada
apps/<nome>/         uma pasta por aplicação (gerada por PR do Portal)
.github/workflows/   CI dos PRs (testes, synth, cdk diff) e deploy (dev automático; prod com aprovação)
nuget.config         fonte da biblioteca Marutech.InfraAws.Cdk (GitHub Packages)
Directory.Packages.props  versões fixadas de todos os pacotes, inclusive a biblioteca
```

## Como uma aplicação nasce
1. Alguém pede a aplicação no Portal e o gestor aprova.
2. O Portal abre um PR criando `apps/<nome>/` a partir de `template-app/`.
3. O CI roda testes, `cdk synth` (com cdk-nag e as regras da plataforma) e comenta o `cdk diff`.
4. Merge na `main`: deploy em **dev** automático; **prod** espera a aprovação de um revisor.

## Repositório público: cuidados
- **Nunca** coloque senhas, tokens, chaves ou dados pessoais nos arquivos. Segredos ficam no SSM Parameter Store (N-03).
- PRs de fork rodam testes, mas não recebem acesso à AWS. Contribuições externas precisam de aprovação para rodar o Actions.
- Os IDs das contas AWS são mascarados nos logs.

## Compilar no seu computador
A biblioteca vem do GitHub Packages, que exige um token até para leitura. Crie um token **classic** com só **`read:packages`** (GitHub → Settings → Developer settings → Personal access tokens) e rode uma vez, na raiz do repositório:
```powershell
dotnet nuget update source infra-aws --username <seu-usuario-github> --password <token> --configfile nuget.config
```
O comando grava a credencial no `nuget.config` local. **Não faça commit desse arquivo depois disso**; para desfazer, use `git checkout nuget.config`.
