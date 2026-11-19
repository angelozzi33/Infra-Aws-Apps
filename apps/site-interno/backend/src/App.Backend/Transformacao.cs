using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace App.Backend
{
    public sealed class Registro
    {
        public string Id { get; set; } = "";
        public string Data { get; set; } = "";       // yyyy-MM-dd
        public string Categoria { get; set; } = "";
        public string Descricao { get; set; } = "";
        public decimal Valor { get; set; }
        public string CriadoPor { get; set; } = "";
        public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class LinhaRelatorio
    {
        public string Categoria { get; set; } = "";
        public int Quantidade { get; set; }
        public decimal Total { get; set; }
        public decimal Percentual { get; set; }
    }

    public sealed class Relatorio
    {
        public string Mes { get; set; } = "";
        public decimal Total { get; set; }
        public int Quantidade { get; set; }
        public decimal MaiorLancamento { get; set; }
        public List<LinhaRelatorio> PorCategoria { get; set; } = new List<LinhaRelatorio>();
        public List<LinhaRelatorio> PorDia { get; set; } = new List<LinhaRelatorio>();
    }

    /// <summary>
    /// Regras de negócio (entrada, validação e transformação dos dados) — código puro, sem AWS,
    /// coberto por testes. É AQUI que o Claude Code deve concentrar a lógica do processo.
    /// </summary>
    public static class Transformacao
    {
        private static readonly Regex RegexData = new Regex("^[0-9]{4}-[0-9]{2}-[0-9]{2}$");
        public const int LimiteLinhasImportacao = 500;

        public static List<string> Validar(Registro r)
        {
            var erros = new List<string>();
            if (!RegexData.IsMatch(r.Data ?? "") || !DateTime.TryParseExact(r.Data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                erros.Add("Data inválida (use AAAA-MM-DD).");
            if (string.IsNullOrWhiteSpace(r.Categoria) || r.Categoria.Length > 60) erros.Add("Categoria obrigatória (até 60 caracteres).");
            if ((r.Descricao ?? "").Length > 200) erros.Add("Descrição até 200 caracteres.");
            if (r.Valor <= 0 || r.Valor > 1_000_000) erros.Add("Valor deve ser maior que zero e até 1.000.000.");
            return erros;
        }

        /// <summary>Normaliza a categoria: espaços extras e capitalização ("  viagem " → "Viagem").</summary>
        public static string NormalizarCategoria(string categoria)
        {
            var limpo = Regex.Replace((categoria ?? "").Trim(), "\\s+", " ");
            return limpo.Length == 0 ? limpo : char.ToUpper(limpo[0], new CultureInfo("pt-BR")) + limpo.Substring(1).ToLower(new CultureInfo("pt-BR"));
        }

        /// <summary>
        /// Lê CSV no formato "data;categoria;descricao;valor" (separador ; ou ,; valor com vírgula decimal
        /// ou ponto). A primeira linha pode ser cabeçalho. Retorna registros válidos e erros por linha.
        /// </summary>
        public static (List<Registro> Registros, List<string> Erros) LerCsv(string conteudo)
        {
            var registros = new List<Registro>();
            var erros = new List<string>();
            var linhas = (conteudo ?? "").Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (linhas.Length > LimiteLinhasImportacao + 1)
            {
                erros.Add($"Arquivo com mais de {LimiteLinhasImportacao} linhas. Divida em partes.");
                return (registros, erros);
            }
            for (var i = 0; i < linhas.Length; i++)
            {
                var linha = linhas[i].Trim();
                if (i == 0 && linha.ToLowerInvariant().StartsWith("data")) continue; // cabeçalho
                var separador = linha.Contains(';') ? ';' : ',';
                var campos = linha.Split(separador);
                if (campos.Length < 4) { erros.Add($"Linha {i + 1}: esperado data;categoria;descricao;valor."); continue; }
                var textoValor = campos[^1].Trim().Replace("R$", "").Trim();
                if (separador == ';' && textoValor.Contains(',')) textoValor = textoValor.Replace(".", "").Replace(",", ".");
                if (!decimal.TryParse(textoValor, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor))
                {
                    erros.Add($"Linha {i + 1}: valor '{campos[^1]}' inválido.");
                    continue;
                }
                var registro = new Registro
                {
                    Data = ConverterData(campos[0].Trim()),
                    Categoria = NormalizarCategoria(campos[1]),
                    Descricao = string.Join(separador.ToString(), campos.Skip(2).Take(campos.Length - 3)).Trim(),
                    Valor = valor
                };
                var errosRegistro = Validar(registro);
                if (errosRegistro.Count > 0) erros.Add($"Linha {i + 1}: {string.Join(" ", errosRegistro)}");
                else registros.Add(registro);
            }
            return (registros, erros);
        }

        /// <summary>Aceita AAAA-MM-DD ou DD/MM/AAAA.</summary>
        public static string ConverterData(string texto) =>
            DateTime.TryParseExact(texto, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.ToString("yyyy-MM-dd") : texto;

        /// <summary>Consolida os registros do mês: totais por categoria e por dia, com percentuais.</summary>
        public static Relatorio Consolidar(string mes, IEnumerable<Registro> registros)
        {
            var lista = registros.Where(r => r.Data.StartsWith(mes, StringComparison.Ordinal)).ToList();
            var total = lista.Sum(r => r.Valor);
            decimal Pct(decimal v) => total == 0 ? 0 : Math.Round(v * 100 / total, 1);
            return new Relatorio
            {
                Mes = mes,
                Total = total,
                Quantidade = lista.Count,
                MaiorLancamento = lista.Count == 0 ? 0 : lista.Max(r => r.Valor),
                PorCategoria = lista.GroupBy(r => r.Categoria)
                    .Select(g => new LinhaRelatorio { Categoria = g.Key, Quantidade = g.Count(), Total = g.Sum(r => r.Valor), Percentual = Pct(g.Sum(r => r.Valor)) })
                    .OrderByDescending(l => l.Total).ToList(),
                PorDia = lista.GroupBy(r => r.Data)
                    .Select(g => new LinhaRelatorio { Categoria = g.Key, Quantidade = g.Count(), Total = g.Sum(r => r.Valor), Percentual = Pct(g.Sum(r => r.Valor)) })
                    .OrderBy(l => l.Categoria).ToList()
            };
        }
    }
}
