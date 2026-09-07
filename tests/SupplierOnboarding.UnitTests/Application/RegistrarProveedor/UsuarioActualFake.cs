using SupplierOnboarding.Application.Identidad;

namespace SupplierOnboarding.UnitTests.Application.RegistrarProveedor;

/// <summary>
/// Fake de <see cref="IUsuarioActual"/> que permite fijar explícitamente el identificador del
/// usuario actual en pruebas unitarias, sin biblioteca de mocking (corrección U1).
/// </summary>
public sealed class UsuarioActualFake(string identificadorUsuario) : IUsuarioActual
{
    public string IdentificadorUsuario { get; } = identificadorUsuario;
}
