using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.Application.Proveedores.RegistrarProveedor;

/// <summary>
/// Validación manual y explícita de entrada (sin FluentValidation, ADR-0007), distinta de los
/// invariantes de <see cref="Proveedor"/>: evalúa FR-002 a FR-006 de forma independiente entre sí
/// y acumula todos los errores encontrados (Caso Límite de múltiples datos inválidos, FR-017).
/// </summary>
public static class RegistrarProveedorValidador
{
    public static IReadOnlyList<string> Validar(RegistrarProveedorComando comando)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(comando.RazonSocial))
        {
            errores.Add("La razón social no puede estar vacía.");
        }

        if (!CatalogoPaisesIso3166.EsValido(comando.Pais))
        {
            errores.Add("El país no es válido.");
        }

        if (string.IsNullOrWhiteSpace(comando.IdentificadorFiscal))
        {
            errores.Add("El identificador fiscal no puede estar vacío.");
        }
        else if (string.IsNullOrEmpty(IdentificadorFiscalNormalizado.Normalizar(comando.IdentificadorFiscal).Valor))
        {
            // FR-010/FR-004 (Clarificación sesión 2026-09-03): mismo tratamiento que un identificador vacío.
            errores.Add("El identificador fiscal no puede estar vacío.");
        }

        if (string.IsNullOrWhiteSpace(comando.NombreContacto))
        {
            errores.Add("El nombre de contacto no puede estar vacío.");
        }

        if (!EsCorreoValido(comando.CorreoContacto))
        {
            errores.Add("El correo electrónico de contacto no tiene un formato válido.");
        }

        return errores;
    }

    private static bool EsCorreoValido(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            return false;
        }

        try
        {
            _ = new System.Net.Mail.MailAddress(correo);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
