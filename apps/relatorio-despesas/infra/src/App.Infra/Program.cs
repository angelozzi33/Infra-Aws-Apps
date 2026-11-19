using Amazon.CDK;
using Marutech.InfraAws.Cdk;
using Marutech.InfraAws.Cdk.Padroes;

namespace App.Infra
{
    /// <summary>
    /// Infraestrutura da aplicação. NÃO crie recursos AWS soltos aqui: use os padrões da biblioteca
    /// Marutech.InfraAws.Cdk. Os dados de governança vêm do app.json (gerado pelo Portal Infra-Aws).
    ///
    ///   cdk synth  -c ambiente=sandbox     (valida tudo: tags, cdk-nag, regras da plataforma)
    ///   cdk deploy -c ambiente=sandbox     (só no sandbox; dev/prod apenas pelo pipeline)
    /// </summary>
    internal static class Program
    {
        public static void Main()
        {
            var app = new Amazon.CDK.App();
            Plataforma.AplicarGuardrails(app);

            var config = ConfiguracaoApp.CarregarArquivo("../app.json");
            config.EmailAlerta = app.Node.TryGetContext("emailAlerta") as string;
            var ambiente = ConfiguracaoApp.LerAmbiente(app.Node.TryGetContext("ambiente") as string);

            var pilha = new PilhaAppInterna(app, $"infra-aws-{config.Nome}-{ambiente.ToString().ToLowerInvariant()}", new PilhaAppInternaProps
            {
                Configuracao = config,
                Ambiente = ambiente,
                Env = new Environment { Region = "us-east-1" }
            });

            new AppWebDados(pilha, "App", new AppWebDadosProps
            {
                PastaFrontend = "../frontend",
                PastaBackend = "../backend/src/App.Backend",
                Handler = "App.Backend::App.Backend.Funcao::Handler"
            });

            app.Synth();
        }
    }
}
