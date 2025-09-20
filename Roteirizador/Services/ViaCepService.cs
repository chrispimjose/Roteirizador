using System.Net.Http.Json;
using System.Text.Json;

namespace Roteirizador.Services;

// ViaCEP API service
public class ViaCepService
{
    /// campo somente leitura de um cliente HTTP para fazer requisições à API
    private readonly HttpClient _http;

    // construtor que recebe um IHttpClientFactory para criar o cliente HTTP
    public ViaCepService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient();
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    // record para mapear a resposta JSON da API ViaCEP
    // record é um tipo de referência em C# projetado
    // para armazenar dados de forma imutável
    public record ViaCepDto(string? cep, string? logradouro, string? complemento, string? bairro, string? localidade, string? uf, bool? erro);

    // método assíncrono que recebe um CEP e retorna o endereço formatado ou null
    public async Task<string?> GetEnderecoAsync(string cep)
    {
        // remove caracteres não numéricos do CEP
        // cep.Where(char.IsDigit): Usa LINQ para filtrar a string, mantendo apenas os números
        cep = new string(cep.Where(char.IsDigit).ToArray());
        if (cep.Length != 8) return null;

        // faz a requisição à API ViaCEP
        var url = $"https://viacep.com.br/ws/{cep}/json/";
        // faz a requisição HTTP GET para a URL construída
        using var resp = await _http.GetAsync(url);
        // se a resposta não for bem-sucedida, retorna null
        if (!resp.IsSuccessStatusCode) return null;

        // desserializa o conteúdo JSON da resposta para o objeto ViaCepDto
        var json = await resp.Content.ReadFromJsonAsync<ViaCepDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // se o JSON for nulo ou indicar erro, retorna null
        if (json == null || json.erro == true) return null;

        // constrói o endereço formatado com logradouro, bairro, localidade e UF em um vetor
        var partes = new[] { json.logradouro, json.bairro, json.localidade, json.uf }.Where(s => !string.IsNullOrWhiteSpace(s));
        // junta as partes do endereço com vírgulas e espaços
        return string.Join(", ", partes);
    }
}
