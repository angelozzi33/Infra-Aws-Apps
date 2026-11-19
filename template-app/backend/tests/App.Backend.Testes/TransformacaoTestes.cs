using System.Linq;
using App.Backend;
using Xunit;

namespace App.Backend.Testes
{
    public class TransformacaoTestes
    {
        [Fact]
        public void Csv_brasileiro_com_cabecalho_e_importado()
        {
            var csv = "data;categoria;descricao;valor\n18/11/2026; viagem ;Taxi aeroporto;R$ 1.234,50\n2026-11-19;Alimentação;Almoço;45,90\n";
            var (registros, erros) = Transformacao.LerCsv(csv);
            Assert.Empty(erros);
            Assert.Equal(2, registros.Count);
            Assert.Equal("2026-11-18", registros[0].Data);
            Assert.Equal("Viagem", registros[0].Categoria);
            Assert.Equal(1234.50m, registros[0].Valor);
        }

        [Fact]
        public void Linhas_invalidas_viram_erros_sem_parar_a_importacao()
        {
            var (registros, erros) = Transformacao.LerCsv("2026-11-18;Viagem;ok;10\n2026-13-01;Viagem;mes invalido;10\nlinha quebrada\n2026-11-18;Viagem;negativo;-5");
            Assert.Single(registros);
            Assert.Equal(3, erros.Count);
        }

        [Fact]
        public void Relatorio_consolida_por_categoria_e_dia()
        {
            var (registros, _) = Transformacao.LerCsv("2026-11-01;Viagem;a;30\n2026-11-01;Viagem;b;50\n2026-11-02;Material;c;20\n2026-10-31;Viagem;outro mes;999");
            var r = Transformacao.Consolidar("2026-11", registros);
            Assert.Equal(100m, r.Total);
            Assert.Equal(3, r.Quantidade);
            var viagem = r.PorCategoria.First();
            Assert.Equal("Viagem", viagem.Categoria);
            Assert.Equal(80m, viagem.Total);
            Assert.Equal(80.0m, viagem.Percentual);
            Assert.Equal(2, r.PorDia.Count);
        }

        [Fact]
        public void Importacao_tem_limite_de_linhas()
        {
            var csv = string.Join("\n", Enumerable.Range(0, Transformacao.LimiteLinhasImportacao + 5).Select(_ => "2026-11-01;Viagem;x;1"));
            var (registros, erros) = Transformacao.LerCsv(csv);
            Assert.Empty(registros);
            Assert.Contains(erros, e => e.Contains("linhas"));
        }
    }
}
