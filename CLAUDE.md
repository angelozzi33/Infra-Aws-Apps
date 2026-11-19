# CLAUDE.md — Infra-Aws-Apps (aplicações da plataforma)

- Responda em **português do Brasil**. Comentários, commits e documentação também em português.
- Este repositório é **público**. Nunca grave credenciais, tokens, account IDs, e-mails pessoais ou dados de usuários em arquivos.
- Cada aplicação vive em `apps/<nome>/` e segue o `CLAUDE.md` da própria pasta (copiado de `template-app/`).
- Infraestrutura **só** com os padrões da biblioteca `Marutech.InfraAws.Cdk` (pacote; versão em `Directory.Packages.props`). Nada de recursos AWS soltos.
- **Nunca** rode `cdk deploy`/`cdk destroy` ou comandos que alterem a AWS: o deploy é feito pelo pipeline (dev automático, prod com aprovação).
- Workflows: nunca use `pull_request_target`; mascare IDs de conta nos logs.
- A plataforma (fundação, biblioteca, Portal e documentação: ADRs e normas) fica no repositório `Infra-Aws`.
