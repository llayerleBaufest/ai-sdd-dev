using SupplierOnboarding.Application.Proveedores.RegistrarProveedor;
using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.Api.Proveedores;

/// <summary>
/// Único endpoint <c>POST /api/proveedores</c> (Minimal API): traduce la solicitud HTTP al
/// comando de Application y el resultado del caso de uso a la respuesta HTTP del contrato.
/// </summary>
public static class ProveedorEndpoints
{
    public static void MapProveedorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/proveedores", async (
            RegistrarProveedorSolicitud solicitud,
            RegistrarProveedorCasoDeUso casoDeUso,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            var comando = new RegistrarProveedorComando(
                solicitud.RazonSocial,
                solicitud.Pais,
                solicitud.IdentificadorFiscal,
                solicitud.NombreContacto,
                solicitud.CorreoContacto);

            var resultado = await casoDeUso.EjecutarAsync(comando, cancellationToken);

            switch (resultado)
            {
                case RegistrarProveedorResultado.Exito exito:
                    // Correlacionado automáticamente por Activity.Current (OpenTelemetry, T059); sin datos sensibles.
                    logger.LogInformation("POST /api/proveedores → 201 Created ({ProveedorId}).", exito.Proveedor.Id);
                    return Results.Created($"/api/proveedores/{exito.Proveedor.Id}", AProveedorRespuesta(exito.Proveedor));
                case RegistrarProveedorResultado.ErroresValidacion erroresValidacion:
                    logger.LogInformation(
                        "POST /api/proveedores → 400 BadRequest ({CantidadErrores} error(es)).",
                        erroresValidacion.Errores.Count);
                    return Results.BadRequest(
                        new ErroresValidacion(
                            erroresValidacion.Errores.Select(mensaje => new ErrorValidacionItem(mensaje)).ToList()));
                case RegistrarProveedorResultado.Duplicado:
                    logger.LogInformation("POST /api/proveedores → 409 Conflict (duplicado).");
                    return Results.Conflict(
                        new ErrorDuplicado(
                            "Ya existe un proveedor registrado con esa combinación de país e identificador fiscal."));
                default:
                    throw new InvalidOperationException($"Resultado de caso de uso no soportado: {resultado}.");
            }
        })
        .WithName("RegistrarProveedor")
        .WithSummary("Registra un nuevo proveedor")
        .WithDescription(
            "Registra un proveedor con los cinco datos obligatorios (FR-001); queda en estado " +
            "Pendiente (FR-012) con identidad propia y auditoría (FR-014, FR-015) devueltas en la " +
            "misma respuesta.")
        .WithTags("Proveedores")
        .Produces<ProveedorRespuesta>(StatusCodes.Status201Created)
        .Produces<ErroresValidacion>(StatusCodes.Status400BadRequest)
        .Produces<ErrorDuplicado>(StatusCodes.Status409Conflict);
    }

    private static ProveedorRespuesta AProveedorRespuesta(Proveedor proveedor) =>
        new(
            proveedor.Id,
            proveedor.RazonSocial,
            proveedor.Pais,
            proveedor.IdentificadorFiscal,
            proveedor.NombreContacto,
            proveedor.CorreoContacto,
            proveedor.Estado.ToString(),
            proveedor.RegistradoPor,
            proveedor.RegistradoEn);
}
