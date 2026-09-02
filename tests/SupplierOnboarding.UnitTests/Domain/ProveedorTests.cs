using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.UnitTests.Domain;

public class ProveedorTests
{
    private static readonly DateTimeOffset InstanteFijo = new(2026, 9, 2, 10, 30, 0, TimeSpan.Zero);

    private static Proveedor CrearProveedorValido(
        string razonSocial = "Acme S.A.",
        string pais = "AR",
        string identificadorFiscal = "30-12345678-9",
        string nombreContacto = "Jane Doe",
        string correoContacto = "jane.doe@acme.com",
        string registradoPor = "usuario.autorizado",
        DateTimeOffset? instante = null) =>
        new(razonSocial, pais, identificadorFiscal, nombreContacto, correoContacto, registradoPor, instante ?? InstanteFijo);

    [Fact]
    public void Constructor_ConDatosValidos_ExponeLosCamposIngresadosYQuedaPendiente()
    {
        var proveedor = CrearProveedorValido();

        Assert.Equal("Acme S.A.", proveedor.RazonSocial);
        Assert.Equal("AR", proveedor.Pais);
        Assert.Equal("30-12345678-9", proveedor.IdentificadorFiscal);
        Assert.Equal("Jane Doe", proveedor.NombreContacto);
        Assert.Equal("jane.doe@acme.com", proveedor.CorreoContacto);
        Assert.Equal("usuario.autorizado", proveedor.RegistradoPor);
        Assert.Equal(InstanteFijo, proveedor.RegistradoEn);
        Assert.Equal(EstadoProveedor.Pendiente, proveedor.Estado);
    }

    [Fact]
    public void EstadoProveedor_SoloAdmiteElValorPendiente()
    {
        // FR-013: ningún comportamiento del dominio permite construir un proveedor en un estado
        // inicial distinto de "Pendiente". Al ser el único valor del enum, es imposible por
        // construcción representar otro estado inicial.
        var valores = Enum.GetValues<EstadoProveedor>();

        var unicoValor = Assert.Single(valores);
        Assert.Equal(EstadoProveedor.Pendiente, unicoValor);
    }

    [Fact]
    public void Constructor_ConRazonSocialVaciaOSoloEspacios_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearProveedorValido(razonSocial: ""));
        Assert.Throws<ArgumentException>(() => CrearProveedorValido(razonSocial: "   "));
    }

    [Fact]
    public void Constructor_ConPaisInvalido_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearProveedorValido(pais: "ZZ"));
    }

    [Fact]
    public void Constructor_ConIdentificadorFiscalVacio_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearProveedorValido(identificadorFiscal: ""));
    }

    [Fact]
    public void Constructor_ConNombreContactoVacio_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearProveedorValido(nombreContacto: ""));
    }

    [Fact]
    public void Constructor_ConCorreoContactoConFormatoInvalido_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearProveedorValido(correoContacto: "no-es-un-correo"));
    }

    [Fact]
    public void Constructor_GeneraIdComoGuidVersion7QueIncorporaElInstanteRecibidoComoParametro()
    {
        // Guid.CreateVersion7 solo es "determinístico" en su segmento de marca de tiempo (48 bits
        // iniciales, RFC 9562); el resto del GUID es aleatorio por diseño. Lo relevante para FR-014
        // y para no depender de un reloj global es que el instante recibido como parámetro queda
        // incorporado en el Id, no que el Id completo sea reproducible byte a byte.
        var proveedor = CrearProveedorValido(instante: InstanteFijo);

        var bytes = proveedor.Id.ToByteArray(bigEndian: true);
        var marcaDeTiempoMs =
            (long)bytes[0] << 40 | (long)bytes[1] << 32 | (long)bytes[2] << 24 |
            (long)bytes[3] << 16 | (long)bytes[4] << 8 | bytes[5];

        Assert.Equal(InstanteFijo.ToUnixTimeMilliseconds(), marcaDeTiempoMs);
        Assert.Equal(7, bytes[6] >> 4); // nibble de versión = 7 (UUIDv7)
    }

    [Fact]
    public void Constructor_DosProveedoresConElMismoInstante_ComparteLaMismaMarcaDeTiempoEnElId()
    {
        var proveedor1 = CrearProveedorValido(instante: InstanteFijo);
        var proveedor2 = CrearProveedorValido(instante: InstanteFijo);

        var marcaDeTiempo1 = proveedor1.Id.ToByteArray(bigEndian: true)[..6];
        var marcaDeTiempo2 = proveedor2.Id.ToByteArray(bigEndian: true)[..6];

        Assert.Equal(marcaDeTiempo1, marcaDeTiempo2);
    }
}
