using System.Security.Claims;
using SupplierOnboarding.Application.Identidad;

namespace SupplierOnboarding.Api.Identidad;

/// <summary>
/// Implementación temporal de <see cref="IUsuarioActual"/> basada en <see cref="ClaimsPrincipal"/>
/// (FR-019 fuera de alcance: no implementa Microsoft Entra ID ni ningún proveedor concreto). Si no
/// hay identidad autenticada (caso esperado sin autenticación real configurada), devuelve un
/// marcador temporal documentado en lugar de un valor vacío o nulo (FR-015).
/// </summary>
public sealed class UsuarioActualHttp : IUsuarioActual
{
    private const string MarcadorSinAutenticacion = "sistema";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActualHttp(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string IdentificadorUsuario
    {
        get
        {
            var usuario = _httpContextAccessor.HttpContext?.User;
            if (usuario?.Identity?.IsAuthenticated != true)
            {
                return MarcadorSinAutenticacion;
            }

            return usuario.Identity.Name ?? MarcadorSinAutenticacion;
        }
    }
}
