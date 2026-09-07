using Microsoft.Extensions.Logging;
using SupplierOnboarding.Application.Identidad;
using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.Application.Proveedores.RegistrarProveedor;

/// <summary>
/// Orquesta el registro de un proveedor: validación de entrada, obtención del usuario actual e
/// instante de registro, verificación de unicidad y persistencia (FR-001 a FR-020).
/// </summary>
public sealed class RegistrarProveedorCasoDeUso
{
    private readonly IProveedorRepository _repositorio;
    private readonly IUsuarioActual _usuarioActual;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RegistrarProveedorCasoDeUso> _logger;

    public RegistrarProveedorCasoDeUso(
        IProveedorRepository repositorio,
        IUsuarioActual usuarioActual,
        TimeProvider timeProvider,
        ILogger<RegistrarProveedorCasoDeUso> logger)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<RegistrarProveedorResultado> EjecutarAsync(
        RegistrarProveedorComando comando, CancellationToken cancellationToken)
    {
        var errores = RegistrarProveedorValidador.Validar(comando);
        if (errores.Count > 0)
        {
            // Sin datos sensibles: solo la cantidad de errores acumulados (FR-017).
            _logger.LogInformation(
                "Registro de proveedor rechazado por {CantidadErrores} error(es) de validación.", errores.Count);
            return new RegistrarProveedorResultado.ErroresValidacion(errores);
        }

        var identificadorFiscalNormalizado = IdentificadorFiscalNormalizado.Normalizar(comando.IdentificadorFiscal).Valor;

        var existe = await _repositorio.ExisteAsync(comando.Pais, identificadorFiscalNormalizado, cancellationToken);
        if (existe)
        {
            _logger.LogInformation(
                "Registro de proveedor rechazado por duplicado en país {Pais} (verificación previa).", comando.Pais);
            return RegistrarProveedorResultado.Duplicado.Instancia;
        }

        var instante = _timeProvider.GetUtcNow();
        var registradoPor = _usuarioActual.IdentificadorUsuario;

        _logger.LogInformation("Intentando registrar proveedor en país {Pais}.", comando.Pais);

        var proveedor = new Proveedor(
            comando.RazonSocial,
            comando.Pais,
            comando.IdentificadorFiscal,
            comando.NombreContacto,
            comando.CorreoContacto,
            registradoPor,
            instante);

        var resultadoAlmacenamiento = await _repositorio.AgregarAsync(proveedor, cancellationToken);

        switch (resultadoAlmacenamiento)
        {
            case ResultadoAlmacenamientoProveedor.Agregado:
                _logger.LogInformation("Proveedor {ProveedorId} registrado exitosamente.", proveedor.Id);
                return new RegistrarProveedorResultado.Exito(proveedor);
            case ResultadoAlmacenamientoProveedor.ConflictoDuplicado:
                _logger.LogInformation(
                    "Registro de proveedor rechazado por duplicado en país {Pais} (condición de carrera).",
                    comando.Pais);
                return RegistrarProveedorResultado.Duplicado.Instancia;
            default:
                throw new InvalidOperationException(
                    $"Resultado de almacenamiento no soportado: {resultadoAlmacenamiento}.");
        }
    }
}
