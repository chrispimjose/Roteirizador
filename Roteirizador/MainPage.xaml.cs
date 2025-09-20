using Roteirizador.Models;
using Roteirizador.Services;
using System.Collections.ObjectModel;

namespace Roteirizador;

/// <summary>
/// Tela inicial para cadastrar e organizar CEPs.
/// Funcionalidades:
///  - Entrada de CEP numérico (sem máscara)
///  - Consulta ViaCEP para obter endereço
///  - Geocoding via Nominatim/OSM para obter coordenadas
///  - Lista com swipe para apagar itens
///  - Reordenação manual com botões ↑ (subir) e ↓ (descer)
///  - Abrir mapa para desenhar pinos e rota na ordem da lista
///  - Mostre as configurações do manifesto do Android: internet, ACCESS_NETWORK_STATE, 
///    ACCESS_FINE_LOCATION e ACCESS_COARSE_LOCATION
///  - Verificar a MauiProgram.cs para ver a injeção de dependência dos serviços  
/// </summary>
public partial class MainPage : ContentPage
{
    // Serviços injetados via construtor
    private readonly ViaCepService _viaCep;
    private readonly GeocodingService _geo;

    /// <summary>
    /// Coleção observável vinculada à UI. Mantém a ordem de roteirização.
    /// </summary>
    // ObservableCollection notifica automaticamente a UI sobre adições, remoções e movimentações.
    // <CepEntry> é um objeto na coleção baseada na classe de dados CepEntry.
    // Isso mantém a ListView sincronizada sem código extra.
    public ObservableCollection<CepEntry> CepItems { get; } = new();

    // Construtor que recebe os serviços via injeção de dependência
    // A injeção de dependência serve para fornecer as dependências
    // (neste caso, os serviços ViaCepService e GeocodingService)
    // que melhoram a modularidade e testabilidade do código.
    public MainPage(ViaCepService viaCep, GeocodingService geo)
    {
        InitializeComponent();
        _viaCep = viaCep;
        _geo = geo;
        BindingContext = this;
    }

    /// <summary>
    /// Botão "Cadastrar CEP":
    /// - Valida o valor digitado
    /// - Evita duplicados
    /// - Consulta o endereço pelo ViaCEP
    /// - Geocodifica o endereço (coordenadas)
    /// - Adiciona o item na lista vinculada à UI
    /// </summary>
    private async void OnCadastrarCepClicked(object sender, EventArgs e)
    {

        // CepEntry.Text: valor digitado pelo usuário no Entry
        // Trim(): remove espaços em branco no início/fim
        var cepRaw = CepEntry.Text?.Trim() ?? string.Empty;

        // LINQ: Where(char.IsDigit) filtra apenas os caracteres numéricos (0-9)
        // new string(...): cria uma string contendo somente os dígitos
        // Ex.: "59.064-320" -> "59064320"
        var cepDigits = new string(cepRaw.Where(char.IsDigit).ToArray());  // mantém apenas números

        // =======================
        // Checagem para evitar item "fantasma"
        // =======================
        if (string.IsNullOrWhiteSpace(cepDigits))
            return; // não faz nada se o usuário não digitou nada


        // Um CEP válido no Brasil contém exatamente 8 dígitos.
        if (cepDigits.Length != 8)
        {
            // Exibe um alerta para o usuário e aborta a execução do método
            await DisplayAlert("CEP inválido", "Digite um CEP com 8 dígitos (ex.: 59064320).", "OK");
            return;
        }

        // CepItems: ObservableCollection<CepEntry> vinculada ao CollectionView na UI.
        // Any(...): verifica se já existe um item na lista com o mesmo CEP.
        if (CepItems.Any(c => c.Cep == cepDigits))
        {
            await DisplayAlert("Duplicado", "Este CEP já está na lista.", "OK");
            return;
        }

        try
        {
            // _viaCep: serviço injetado para consultar a API ViaCEP
            // Retorna o endereço no formato "logradouro, bairro, cidade - UF".
            // Caso a API não retorne dados, usamos "(endereço não encontrado)" como fallback.
            var endereco = await _viaCep.GetEnderecoAsync(cepDigits) ?? "(endereço não encontrado)";
            if (string.IsNullOrWhiteSpace(endereco))
            {
                await DisplayAlert("Atenção", "Não foi possível obter o endereço deste CEP.", "OK");
                return;
            }

            // _geo: serviço injetado que consulta a API de geocodificação Nominatim.
            // Recebe uma string com o endereço + "Brasil".
            // Retorna um objeto com latitude e longitude aproximadas.
            var coords = await _geo.GeocodeAsync($"{endereco}, Brasil");

    
            // CepEntry: modelo contendo CEP, Endereço e coordenadas.
            // Este objeto será exibido na CollectionView e usado no MapPage.
            var item = new CepEntry
            {
                Cep = cepDigits,
                Endereco = endereco,
                Latitude = coords?.lat,
                Longitude = coords?.lon
            };

            // Como CepItems é uma ObservableCollection, a UI (CollectionView)
            // é automaticamente atualizada e mostra o novo item.
            CepItems.Add(item);

            // Limpa a caixa de entrada (Entry) para facilitar o próximo cadastro.
            CepEntry.Text = string.Empty;
        }
        catch (Exception ex)
        {
            // Captura qualquer erro (ex.: falha de rede, erro de parsing, timeout).
            // Mostra a mensagem para o usuário em forma de alerta.
            await DisplayAlert("Erro", $"Falha ao consultar o CEP: {ex.Message}", "OK");
        }
    }


    /// <summary>
    /// Swipe "Apagar": remove item específico.
    /// </summary>
    private void OnApagarItemSwipe(object sender, EventArgs e)
    {
        // Verifica se o remetente é um SwipeItem e obtém o item associado
        if (sender is SwipeItem swipe && swipe.BindingContext is CepEntry item)
        {
            // Remove o item da coleção observável
            CepItems.Remove(item);
        }
    }

    /// <summary>
    /// Botão global "Limpar Lista": remove todos os CEPs.
    /// </summary>
    private void OnLimparClicked(object sender, EventArgs e)
    {
        // Limpa todos os itens da coleção observável
        CepItems.Clear();
    }

    /// <summary>
    /// Abre a MapPage com a lista atual (somente itens que possuem coordenadas).
    /// </summary>
    private async void OnAbrirMapaClicked(object sender, EventArgs e)
    {
        // Verifica se há pelo menos um CEP com coordenadas válidas
        if (!CepItems.Any(c => c.HasCoords))
        {
            // Exibe alerta se nenhum CEP tiver coordenadas
            await DisplayAlert("Sem coordenadas", "Cadastre ao menos um CEP com coordenadas válidas.", "OK");
            return;
        }
        // Abra a página do mapa e passe a lista de CEPs como parâmetro
        await Navigation.PushAsync(new MapPage(CepItems.ToList()));
    }

    /// <summary>
    /// Move um item uma posição acima na lista.
    /// </summary>
    private void OnMoveUpClicked(object sender, EventArgs e)
    {
        // Verifica se o remetente é um Button e obtém o item associado
        if (sender is Button btn && btn.CommandParameter is CepEntry item)
        {
            // Obtém o índice atual do item na coleção
            var index = CepItems.IndexOf(item);
            // Move o item para cima se não estiver no topo
            if (index > 0)
            {
                CepItems.Move(index, index - 1);
            }
        }
    }

    /// <summary>
    /// Move um item uma posição abaixo na lista.
    /// </summary>
    private void OnMoveDownClicked(object sender, EventArgs e)
    {
        // Verifica se o remetente é um Button e obtém o item associado
        if (sender is Button btn && btn.CommandParameter is CepEntry item)
        {
            // Obtém o índice atual do item na coleção
            var index = CepItems.IndexOf(item);
            // Move o item para baixo se não estiver na última posição
            if (index >= 0 && index < CepItems.Count - 1)
            {
                CepItems.Move(index, index + 1);
            }
        }
    }
}
