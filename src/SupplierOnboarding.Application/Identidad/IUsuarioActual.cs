namespace SupplierOnboarding.Application.Identidad;

/// <summary>
/// Puerto que expone el identificador del usuario autenticado, sin ninguna dependencia de ASP.NET
/// Core, <c>ClaimsPrincipal</c> ni <c>HttpContext</c> (Principio VI de la Constitution). La
/// autenticación/autorización concretas continúan fuera de alcance de esta funcionalidad (FR-019);
/// esta interfaz solo prepara el punto de extensión.
/// </summary>
public interface IUsuarioActual
{
    string IdentificadorUsuario { get; }
}
