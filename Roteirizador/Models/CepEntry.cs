using System.Runtime.Intrinsics.X86;

namespace Roteirizador.Models;

// Classe que representa uma entrada de CEP (Código de Endereçamento Postal)
public class CepEntry
{
    // Inicializa as propriedades Cep e Endereco com strings vazias
    public string Cep { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;

    //double? é um atalho para Nullable<double>, uma estrutura que
    //permite que um tipo de valor (como double) seja nulo
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Propriedade que indica se as coordenadas estão presentes e são não nulas
    // A expressão Latitude.HasValue && Longitude.HasValue; retorna:
    //true se ambas as propriedades Latitude e Longitude tiverem valores não nulos.
    //false se qualquer uma delas for null.
    public bool HasCoords => Latitude.HasValue && Longitude.HasValue;

    public override string ToString()
        => $"{Cep} - {Endereco}" + (HasCoords ? $" ({Latitude:F6}, {Longitude:F6})" : "");
}
