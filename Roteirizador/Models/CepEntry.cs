using System.Runtime.Intrinsics.X86;

namespace Roteirizador.Models;

// Classe que representa uma entrada de CEP (C�digo de Endere�amento Postal)
public class CepEntry
{
    // Inicializa as propriedades Cep e Endereco com strings vazias
    public string Cep { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;

    //double? � um atalho para Nullable<double>, uma estrutura que
    //permite que um tipo de valor (como double) seja nulo
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Propriedade que indica se as coordenadas est�o presentes e s�o n�o nulas
    // A express�o Latitude.HasValue && Longitude.HasValue; retorna:
    //true se ambas as propriedades Latitude e Longitude tiverem valores n�o nulos.
    //false se qualquer uma delas for null.
    public bool HasCoords => Latitude.HasValue && Longitude.HasValue;

    public override string ToString()
        => $"{Cep} - {Endereco}" + (HasCoords ? $" ({Latitude:F6}, {Longitude:F6})" : "");
}
