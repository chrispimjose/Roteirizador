using Roteirizador.Models;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Roteirizador;

/// <summary>
/// Página do mapa.
/// - Carrega o HTML local (Leaflet) em um WebView
/// - Ao abrir, tenta obter a localização do dispositivo (GPS) para centralizar o mapa e marcar "Início"
/// - Desenha pinos e a rota para os CEPs cadastrados na ordem da lista
/// - Permite apagar rotas e pinos e recentralizar
/// </summary>
public partial class MapPage : ContentPage
{
    private readonly List<CepEntry> _points;   // Pontos com coordenadas provenientes da MainPage
    private bool _isReady;                     // HTML/JS carregado?
    private (double lat, double lon)? _deviceStart; // Localização de início (GPS)

    public MapPage(List<CepEntry> points)
    {
        InitializeComponent();
        _points = points.Where(p => p.HasCoords).ToList();

        // Carrega o HTML local (Resources/Raw/map.html) no WebView
        MapWebView.Source = new HtmlWebViewSource
        {
            // Carrega o HTML empacotado no app
            Html = LoadEmbeddedHtml()
        };
    }

    /// <summary>
    /// Lê o arquivo HTML empacotado no app (map.html).
    /// </summary>
    private string LoadEmbeddedHtml()
    {
        // Abre o arquivo HTML como stream e lê todo o conteúdo
        using var stream = FileSystem.OpenAppPackageFileAsync("map.html").Result;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Evento disparado quando o WebView terminou a navegação.
    /// Aqui marcamos _isReady para poder executar JS.
    /// </summary>
    private void OnWebNavigated(object sender, WebNavigatedEventArgs e)
    {
        // Marca que o HTML/JS está pronto
        _isReady = true;
    }

    /// <summary>
    /// Aguarda o HTML/JS estar pronto antes de chamar funções JS.
    /// </summary>
    private async Task EnsureReadyAsync()
    {
        // Espera até 6 segundos (40 x 150ms) para o HTML/JS estar pronto
        int tries = 0;
        // Se já estiver pronto, não espera
        while (!_isReady && tries < 40) // ~6s
        {
            // Aguarda 150ms antes de tentar novamente
            await Task.Delay(150);
            tries++;
        }
    }

    /// <summary>
    /// Ao aparecer, solicita permissão e tenta pegar a localização do dispositivo.
    /// Em seguida, centraliza o mapa nessa posição e marca "Início".
    /// </summary>
    protected override async void OnAppearing()
    {
        // Chama o base (OnAppearing) e tenta obter a localização
        base.OnAppearing();
        // Garante que o HTML/JS esteja pronto
        await EnsureReadyAsync();
        // Tenta obter a localização do dispositivo (GPS)
        _deviceStart = await TryGetDeviceLocationAsync();

        // Se obteve a localização, centraliza e marca o ponto inicial
        if (_deviceStart is (double lat, double lon))
        {
            // Chama JS para centralizar e marcar o ponto inicial
            await MapWebView.EvaluateJavaScriptAsync($"setStart({lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {lon.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
        }
    }

    /// <summary>
    /// Pede permissão de localização e tenta obter a posição atual.
    /// </summary>
    private async Task<(double lat, double lon)?> TryGetDeviceLocationAsync()
    {
        // Pede permissão em tempo de execução
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        // Se negada, retorna nulo
        if (status != PermissionStatus.Granted)
            return null;

        try
        {
            // Tenta primeiro a última localização conhecida (mais rápida)
            var last = await Geolocation.GetLastKnownLocationAsync();
            if (last != null)
                return (last.Latitude, last.Longitude);

            // Se não houver, solicita uma nova leitura
            var req = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
            var loc = await Geolocation.GetLocationAsync(req);
            if (loc != null)
                return (loc.Latitude, loc.Longitude);
        }
        catch
        {
            // Ignora erros de GPS; o mapa usará o fallback do HTML
        }
        return null;
    }

    /// <summary>
    /// Botão "Desenhar Rotas": envia os pontos (lat/lon/label) ao HTML para pinar e traçar a rota.
    /// </summary>
    private async void OnDesenharClicked(object sender, EventArgs e)
    {
        // Garante que o HTML/JS esteja pronto
        await EnsureReadyAsync();
        // Prepara o payload JSON com os pontos
        var payload = _points.Select(p => new { lat = p.Latitude, lon = p.Longitude, label = $"{p.Cep} - {p.Endereco}" });
        var json = JsonSerializer.Serialize(payload);
        // Chama a função JS com o JSON
        await MapWebView.EvaluateJavaScriptAsync($"plotPointsAndRoute({json})");
    }

    /// <summary>
    /// Botão "Apagar Rotas": remove pinos e linhas do mapa.
    /// </summary>
    private async void OnApagarClicked(object sender, EventArgs e)
    {
        // Garante que o HTML/JS esteja pronto
        await EnsureReadyAsync();
        // Chama a função JS para limpar
        await MapWebView.EvaluateJavaScriptAsync($"clearRoutesAndMarkers()");
    }

    /// <summary>
    /// Botão "Centralizar": ajusta o mapa para caber nos pinos atuais.
    /// </summary>
    private async void OnCentralizarClicked(object sender, EventArgs e)
    {
        // Garante que o HTML/JS esteja pronto
        await EnsureReadyAsync();
        // Chama a função JS para ajustar o zoom
        await MapWebView.EvaluateJavaScriptAsync($"fitToData()");
    }
}
