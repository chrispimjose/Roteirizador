using System.Net.Http;
using System.Text.Json;

namespace Roteirizador.Services;

// Define uma classe de servi�o para geocodifica��o usando a API Nominatim do OpenStreetMap
public class GeocodingService
{
    // campo somente leitura de um cliente HTTP para fazer requisi��es � API
    private readonly HttpClient _http;

    // construtor que recebe um IHttpClientFactory para criar o cliente HTTP
    // o factory n�o tem rela��o com o padr�o de projeto Factory Method
    public GeocodingService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient();
        _http.Timeout = TimeSpan.FromSeconds(15);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Roteirizador/1.0 (+https://example.com)");
    }

    // � uma classe interna usada para representar o resultado de uma chamada � API de geocodifica��o
    private class NominatimResult
    {
        public string? display_name { get; set; }
        public string? lat { get; set; }
        public string? lon { get; set; }
    }

    // Define um m�todo ass�ncrono que converte uma string de endere�o
    // em coordenadas geogr�ficas (latitude e longitude) ou retorna null se falhar.
    public async Task<(double lat, double lon)?> GeocodeAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(query)}&limit=1&countrycodes=br";
        
        // � uma chamada ass�ncrona a uma requisi��o HTTP GET
        // declarada como um um recurso descart�vel (using)
        using var resp = await _http.GetAsync(url);
        // retonar null se a resposta n�o for bem-sucedida
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        // desserializa o JSON retornado em uma lista de objetos NominatimResult
        var arr = JsonSerializer.Deserialize<List<NominatimResult>>(json);
        // obt�m o primeiro resultado da lista (ou null se a lista estiver vazia)
        var first = arr?.FirstOrDefault();
        // verifica se o resultado � v�lido e tenta converter as strings de latitude e longitude em doubles
        if (first == null || string.IsNullOrWhiteSpace(first.lat) || string.IsNullOrWhiteSpace(first.lon)) return null;
        if (!double.TryParse(first.lat, System.Globalization.CultureInfo.InvariantCulture, out var lat)) return null;
        if (!double.TryParse(first.lon, System.Globalization.CultureInfo.InvariantCulture, out var lon)) return null;
        return (lat, lon);
    }
}
