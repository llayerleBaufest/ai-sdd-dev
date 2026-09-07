using SupplierOnboarding.Application.Proveedores.RegistrarProveedor;

namespace SupplierOnboarding.UnitTests.Application.RegistrarProveedor;

public class RegistrarProveedorValidadorTests
{
    private static RegistrarProveedorComando ComandoValido(
        string razonSocial = "Acme S.A.",
        string pais = "AR",
        string identificadorFiscal = "30-12345678-9",
        string nombreContacto = "Jane Doe",
        string correoContacto = "jane.doe@acme.com") =>
        new(razonSocial, pais, identificadorFiscal, nombreContacto, correoContacto);

    [Fact]
    public void Validar_ConDatosValidos_NoDevuelveErrores()
    {
        var errores = RegistrarProveedorValidador.Validar(ComandoValido());

        Assert.Empty(errores);
    }

    [Fact]
    public void Validar_ConVariosDatosInvalidosSimultaneamente_AcumulaTodosLosErrores()
    {
        // Caso Límite de spec.md (FR-017): la validación no se detiene en el primer error.
        var comando = ComandoValido(
            razonSocial: "   ",
            pais: "ZZ",
            identificadorFiscal: "",
            nombreContacto: "",
            correoContacto: "no-es-un-correo");

        var errores = RegistrarProveedorValidador.Validar(comando);

        Assert.Equal(5, errores.Count);
    }

    [Fact]
    public void Validar_ConRazonSocialVaciaOSoloEspacios_DevuelveError()
    {
        Assert.Single(RegistrarProveedorValidador.Validar(ComandoValido(razonSocial: "")));
        Assert.Single(RegistrarProveedorValidador.Validar(ComandoValido(razonSocial: "   ")));
    }

    [Fact]
    public void Validar_ConPaisInvalido_DevuelveError()
    {
        Assert.Single(RegistrarProveedorValidador.Validar(ComandoValido(pais: "ZZ")));
    }

    [Fact]
    public void Validar_ConIdentificadorFiscalVacio_DevuelveError()
    {
        Assert.Single(RegistrarProveedorValidador.Validar(ComandoValido(identificadorFiscal: "")));
    }

    [Theory]
    [InlineData("---")]
    [InlineData("   ")]
    public void Validar_ConIdentificadorFiscalQueNormalizaAVacio_DevuelveError(string identificadorFiscal)
    {
        // FR-010/FR-004 (Clarificación sesión 2026-09-03): mismo tratamiento que un identificador
        // fiscal vacío cuando la normalización deja un texto vacío.
        Assert.Single(RegistrarProveedorValidador.Validar(ComandoValido(identificadorFiscal: identificadorFiscal)));
    }

    [Fact]
    public void Validar_ConNombreContactoVacio_DevuelveError()
    {
        Assert.Single(RegistrarProveedorValidador.Validar(ComandoValido(nombreContacto: "")));
    }

    [Fact]
    public void Validar_ConCorreoContactoConFormatoInvalido_DevuelveError()
    {
        Assert.Single(RegistrarProveedorValidador.Validar(ComandoValido(correoContacto: "no-es-un-correo")));
    }
}
