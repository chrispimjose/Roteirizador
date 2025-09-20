using System.Net.Http.Json;
using System.Text.Json;

namespace Roteirizador.Services;

// ViaCEP API service
public class ViaCepService
{
    /// campo somente leitura de um cliente HTTP para fazer requisi��es � API
    private readonly HttpClient _http;

    // construtor que recebe um IHttpClientFactory para criar o cliente HTTP
    public ViaCepService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient();
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    // record para mapear a resposta JSON da API ViaCEP
    // record � um tipo de refer�ncia em C# projetado
    // para armazenar dados de forma imut�vel
    public record ViaCepDto(string? cep, string? logradouro, string? complemento, string? bairro, string? localidade, string? uf, bool? erro);

    // m�todo ass�ncrono que recebe um CEP e retorna o endere�o formatado ou null
    public async Task<string?> GetEnderecoAsync(string cep)
    {
        // remove caracteres n�o num�ricos do CEP
        // cep.Where(char.IsDigit): Usa LINQ para filtrar a string, mantendo apenas os n�meros
        cep = new string(cep.Where(char.IsDigit).ToArray());
        if (cep.Length != 8) return null;

        // faz a requisi��o � API ViaCEP
        var url = $"https://viacep.com.br/ws/{cep}/json/";
        // faz a requisi��o HTTP GET para a URL constru�da
        using var resp = await _http.GetAsync(url);
        // se a resposta n�o for bem-sucedida, retorna null
        if (!resp.IsSuccessStatusCode) return null;

        // desserializa o conte�do JSON da resposta para o objeto ViaCepDto
        var json = await resp.Content.ReadFromJsonAsync<ViaCepDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // se o JSON for nulo ou indicar erro, retorna null
        if (json == null || json.erro == true) return null;

        // constr�i o endere�o formatado com logradouro, bairro, localidade e UF em um vetor
        var partes = new[] { json.logradouro, json.bairro, json.localidade, json.uf }.Where(s => !string.IsNullOrWhiteSpace(s));
        // junta as partes do endere�o com v�rgulas e espa�os
        return string.Join(", ", partes);
    }
}
