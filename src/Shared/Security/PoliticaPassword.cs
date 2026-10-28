namespace FidelixAPI.Shared.Security;

/// <summary>Política mínima de contraseñas (RN-02), compartida por los distintos canales que la definen.</summary>
public static class PoliticaPassword
{
    /// <summary>Largo mínimo exigido (RN-02).</summary>
    public const int LargoMinimo = 8;

    /// <summary>Indica si la contraseña cumple la política mínima: no vacía y de al menos 8 caracteres.</summary>
    public static bool EsValida(string? password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length >= LargoMinimo;
}
