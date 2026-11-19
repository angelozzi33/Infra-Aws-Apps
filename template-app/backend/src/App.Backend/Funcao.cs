using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.CamelCaseLambdaJsonSerializer))]

namespace App.Backend
{
    /// <summary>
    /// API da aplicação (rotas /api/*). O API Gateway já validou o login (JWT do Cognito).
    /// Rotas:
    ///   GET    /api/registros?mes=AAAA-MM     lista os lançamentos do mês
    ///   POST   /api/registros                 cria um lançamento
    ///   DELETE /api/registros/{id}            remove um lançamento
    ///   POST   /api/importar                  importa CSV (data;categoria;descricao;valor)
    ///   GET    /api/relatorio?mes=AAAA-MM     relatório consolidado (transformação)
    /// </summary>
    public class Funcao
    {
        private static readonly IAmazonDynamoDB Dynamo = new AmazonDynamoDBClient();
        private static readonly string Tabela = Environment.GetEnvironmentVariable("TABELA") ?? "";
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        private static readonly Regex RegexMes = new Regex("^[0-9]{4}-[0-9]{2}$");
        private static readonly Regex RegexId = new Regex("^[0-9]{4}-[0-9]{2}-[0-9]{2}_[a-f0-9]{12}$");

        static Funcao() { Amazon.AWSConfigs.InitializeCollections = true; }

        public async Task<APIGatewayHttpApiV2ProxyResponse> Handler(APIGatewayHttpApiV2ProxyRequest req, ILambdaContext ctx)
        {
            try
            {
                var claims = req.RequestContext?.Authorizer?.Jwt?.Claims;
                if (claims == null || !claims.TryGetValue("sub", out var usuario)) return Resposta(401, new { erro = "Não autenticado." });

                var metodo = req.RequestContext?.Http?.Method ?? "GET";
                var caminho = (req.RawPath ?? "").TrimEnd('/');
                var query = req.QueryStringParameters ?? new Dictionary<string, string>();
                var mes = query.TryGetValue("mes", out var m) && RegexMes.IsMatch(m) ? m : DateTime.UtcNow.ToString("yyyy-MM");
                var corpo = req.Body ?? "";
                if (req.IsBase64Encoded) corpo = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(corpo));
                if (corpo.Length > 200_000) return Resposta(413, new { erro = "Conteúdo grande demais." });

                if (metodo == "GET" && caminho == "/api/registros") return Resposta(200, await Listar(mes));
                if (metodo == "GET" && caminho == "/api/relatorio") return Resposta(200, Transformacao.Consolidar(mes, await Listar(mes)));
                if (metodo == "POST" && caminho == "/api/registros")
                {
                    var r = JsonSerializer.Deserialize<Registro>(corpo, Json) ?? new Registro();
                    r.Categoria = Transformacao.NormalizarCategoria(r.Categoria);
                    var erros = Transformacao.Validar(r);
                    if (erros.Count > 0) return Resposta(400, new { erro = string.Join(" ", erros) });
                    await Gravar(new[] { r }, usuario);
                    return Resposta(200, r);
                }
                if (metodo == "POST" && caminho == "/api/importar")
                {
                    var (registros, erros) = Transformacao.LerCsv(corpo);
                    await Gravar(registros, usuario);
                    return Resposta(200, new { importados = registros.Count, erros });
                }
                if (metodo == "DELETE" && caminho.StartsWith("/api/registros/", StringComparison.Ordinal))
                {
                    var id = caminho.Substring("/api/registros/".Length);
                    if (!RegexId.IsMatch(id)) return Resposta(400, new { erro = "Identificador inválido." });
                    await Dynamo.DeleteItemAsync(new DeleteItemRequest { TableName = Tabela, Key = Chave(id.Substring(0, 7), id) });
                    ctx.Logger.LogInformation($"Registro {id} removido por {usuario}");
                    return Resposta(200, new { ok = true });
                }
                return Resposta(404, new { erro = "Rota inexistente." });
            }
            catch (JsonException) { return Resposta(400, new { erro = "JSON inválido." }); }
            catch (Exception e)
            {
                ctx.Logger.LogError(e.ToString());
                return Resposta(500, new { erro = "Erro inesperado." });
            }
        }

        // Modelo no DynamoDB: pk = "MES#AAAA-MM", sk = "REG#<id>" — consultar um mês é uma Query simples.
        private static Dictionary<string, AttributeValue> Chave(string mes, string id) => new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = "MES#" + mes },
            ["sk"] = new AttributeValue { S = "REG#" + id }
        };

        private static async Task<List<Registro>> Listar(string mes)
        {
            var itens = new List<Registro>();
            Dictionary<string, AttributeValue>? inicio = null;
            do
            {
                var r = await Dynamo.QueryAsync(new QueryRequest
                {
                    TableName = Tabela,
                    KeyConditionExpression = "pk = :pk",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":pk"] = new AttributeValue { S = "MES#" + mes } },
                    ExclusiveStartKey = inicio
                });
                itens.AddRange(r.Items.Select(i => JsonSerializer.Deserialize<Registro>(i["dados"].S, Json)!));
                inicio = r.LastEvaluatedKey is { Count: > 0 } ? r.LastEvaluatedKey : null;
            } while (inicio != null);
            return itens.OrderBy(i => i.Data).ToList();
        }

        private static async Task Gravar(IEnumerable<Registro> registros, string usuario)
        {
            // BatchWrite grava até 25 itens por chamada.
            foreach (var lote in registros.Chunk(25))
            {
                var requisicoes = lote.Select(r =>
                {
                    r.Id = $"{r.Data}_{Guid.NewGuid():N}".Substring(0, 23);
                    r.CriadoPor = usuario;
                    r.CriadoEm = DateTimeOffset.UtcNow;
                    var item = Chave(r.Data.Substring(0, 7), r.Id);
                    item["dados"] = new AttributeValue { S = JsonSerializer.Serialize(r, Json) };
                    return new WriteRequest { PutRequest = new PutRequest { Item = item } };
                }).ToList();
                var resposta = await Dynamo.BatchWriteItemAsync(new BatchWriteItemRequest
                {
                    RequestItems = new Dictionary<string, List<WriteRequest>> { [Tabela] = requisicoes }
                });
                // Itens não processados (limite de vazão): uma nova tentativa simples.
                if (resposta.UnprocessedItems is { Count: > 0 })
                    await Dynamo.BatchWriteItemAsync(new BatchWriteItemRequest { RequestItems = resposta.UnprocessedItems });
            }
        }

        private static APIGatewayHttpApiV2ProxyResponse Resposta(int status, object corpo) => new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = status,
            Body = JsonSerializer.Serialize(corpo, Json),
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json; charset=utf-8", ["Cache-Control"] = "no-store" }
        };
    }
}
