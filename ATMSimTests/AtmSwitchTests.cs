using ATMSim;
using ATMSimTests.Fakes;
using FluentAssertions;
using System.Security.Cryptography;

namespace ATMSimTests;

public class AtmSwitchTests
{
	private const string teclasRetiroConRecibo = "AAA";
	private const string teclasRetiroSinRecibo = "AAC";
	private const string teclasConsultaDeBalance = "B";
	private const string binTarjeta = "459413";

	private static IATM CrearATMFalso(string nombre) => new FakeATM(nombre);

	private static string CrearCuentaYTarjeta(IAutorizador autorizador, TipoCuenta tipoCuenta, int balanceInicial, string binTarjeta, string pin)
	{
		string numeroCuenta = autorizador.CrearCuenta(tipoCuenta, balanceInicial);
		string numeroTarjeta = autorizador.CrearTarjeta(binTarjeta, numeroCuenta);
		autorizador.AsignarPin(numeroTarjeta, pin);
		return numeroTarjeta;
	}

	private static IAutorizador CrearAutorizador(string nombre, IHSM hsm, decimal limite) => new Autorizador(nombre, hsm, limite);

	private static void RegistrarATMEnSwitch(IATM atm, IATMSwitch atmSwitch, IHSM hsm)
	{
		ComponentesLlave llaveATM = hsm.GenerarLlave();
		atm.InstalarLlave(llaveATM.LlaveEnClaro);
		atmSwitch.RegistrarATM(atm, llaveATM.LlaveEncriptada);
	}

	private static void RegistrarAutorizadorEnSwitch(IAutorizador autorizador, IATMSwitch atmSwitch, IHSM hsm)
	{
		ComponentesLlave llaveAutorizador = hsm.GenerarLlave();
		autorizador.InstalarLlave(llaveAutorizador.LlaveEncriptada);
		atmSwitch.RegistrarAutorizador(autorizador, llaveAutorizador.LlaveEncriptada);
		atmSwitch.AgregarRuta(binTarjeta, autorizador.Nombre);
	}

	private static IATMSwitch CrearSwitch(IHSM hsm, IConsoleWriter consoleWriter)
	{
		IATMSwitch atmSwitch = new ATMSwitch(hsm, consoleWriter);
		atmSwitch.AgregarConfiguracionOpKey(new ConfiguracionOpKey()
		{
			Teclas = teclasRetiroConRecibo,
			TipoTransaccion = TipoTransaccion.Retiro,
			Recibo = true
		});
		atmSwitch.AgregarConfiguracionOpKey(new ConfiguracionOpKey()
		{
			Teclas = teclasRetiroSinRecibo,
			TipoTransaccion = TipoTransaccion.Retiro,
			Recibo = false
		});
		atmSwitch.AgregarConfiguracionOpKey(new ConfiguracionOpKey()
		{
			Teclas = teclasConsultaDeBalance,
			TipoTransaccion = TipoTransaccion.Consulta,
			Recibo = false
		});
		return atmSwitch;
	}

	public byte[] Encriptar(string textoPlano, byte[] llaveEnClaro)
	{
		const int TAMANO_LLAVE = 32;

		byte[] llave = llaveEnClaro.Skip(0).Take(TAMANO_LLAVE).ToArray();
		byte[] iv = llaveEnClaro.Skip(TAMANO_LLAVE).ToArray();
		using Aes llaveAes = Aes.Create();
		llaveAes.Key = llave;
		llaveAes.IV = iv;

		ICryptoTransform encriptador = llaveAes.CreateEncryptor();

		using MemoryStream ms = new();
		using CryptoStream cs = new(ms, encriptador, CryptoStreamMode.Write);
		using (StreamWriter sw = new(cs))
		{
			sw.Write(textoPlano);
		}
		return ms.ToArray();


	}


	[Fact]
	public void Withdrawal_with_balance_on_account_is_successful()
	{
		// ARRANGE
		FakeConsoleWriter consoleWriter = new();

		IHSM hsm = new HSM();

		IATMSwitch sut = CrearSwitch(hsm, consoleWriter);

		IATM atm = CrearATMFalso("AJP001");
		RegistrarATMEnSwitch(atm, sut, hsm);

		IAutorizador autorizador = CrearAutorizador("AutDB", hsm, 10_000);
		RegistrarAutorizadorEnSwitch(autorizador, sut, hsm);

		string numeroTarjeta = CrearCuentaYTarjeta(autorizador, TipoCuenta.Ahorros, 20_000, binTarjeta, "1234");

		byte[] llaveEnClaro = ((FakeATM)atm).Llave;
		byte[] criptogramaPin = Encriptar("1234", llaveEnClaro);

		// ACT
		List<Comando> comandosRespuesta = sut.Autorizar(atm, teclasRetiroConRecibo, numeroTarjeta, 200, criptogramaPin);

		// ASSERT
		_ = comandosRespuesta.Should().HaveCountGreaterThanOrEqualTo(1);
		_ = comandosRespuesta.Should().Contain(x => x.GetType() == typeof(ComandoDispensarEfectivo));

		ComandoDispensarEfectivo comando = (ComandoDispensarEfectivo)comandosRespuesta
			.Where(x => x.GetType() == typeof(ComandoDispensarEfectivo)).Single();
		_ = comando.Monto.Should().Be(200);

	}

	[Fact]//validar si devuelve error generico
	public void Withdrawal_without_balance_on_account_fail()
	{
		// ARRANGE
		FakeConsoleWriter consoleWriter = new();
		IHSM hsm = new HSM();
		IATMSwitch sut = CrearSwitch(hsm, consoleWriter);
		IATM atm = CrearATMFalso("AJP001");
		RegistrarATMEnSwitch(atm, sut, hsm);
		IAutorizador autorizador = CrearAutorizador("AutDB", hsm, 10_000);
		RegistrarAutorizadorEnSwitch(autorizador, sut, hsm);

		string numeroTarjeta = CrearCuentaYTarjeta(autorizador, TipoCuenta.Ahorros, 0, binTarjeta, "1234");

		byte[] llaveEnClaro = ((FakeATM)atm).Llave;
		byte[] criptogramaPin = Encriptar("1234", llaveEnClaro);

		// ACT
		List<Comando> comandosRespuesta = sut.Autorizar(atm, teclasRetiroConRecibo, numeroTarjeta, 200, criptogramaPin);

		// ASSERT
		_ = comandosRespuesta.Should().HaveCountGreaterThanOrEqualTo(1);
		_ = comandosRespuesta.Should().Contain(x => x.GetType() == typeof(ComandoMostrarInfoEnPantalla));

		ComandoMostrarInfoEnPantalla comando = (ComandoMostrarInfoEnPantalla)comandosRespuesta
			.Where(x => x.GetType() == typeof(ComandoMostrarInfoEnPantalla)).Single();
		_ = comando.Error.Equals(true);

	}

	[Fact]
	public void Withdrawal_with_a_wrong_operation_Key_fail()
	{
		// ARRANGE
		FakeConsoleWriter consoleWriter = new();
		IHSM hsm = new HSM();
		IATMSwitch sut = CrearSwitch(hsm, consoleWriter);
		IATM atm = CrearATMFalso("AJP001");
		RegistrarATMEnSwitch(atm, sut, hsm);
		IAutorizador autorizador = CrearAutorizador("AutDB", hsm, 10_000);
		RegistrarAutorizadorEnSwitch(autorizador, sut, hsm);

		string numeroTarjeta = CrearCuentaYTarjeta(autorizador, TipoCuenta.Ahorros, 0, binTarjeta, "1234");

		byte[] llaveEnClaro = ((FakeATM)atm).Llave;
		byte[] criptogramaPin = Encriptar("1234", llaveEnClaro);

		// ACT
		List<Comando> comandosRespuesta = sut.Autorizar(atm, "XXI", numeroTarjeta, 200, criptogramaPin);

		// ASSERT
		_ = comandosRespuesta.Should().HaveCountGreaterThanOrEqualTo(1);
		_ = comandosRespuesta.Should().Contain(x => x.GetType() == typeof(ComandoMostrarInfoEnPantalla));

		ComandoMostrarInfoEnPantalla comando = (ComandoMostrarInfoEnPantalla)comandosRespuesta.Where(x => x.GetType() == typeof(ComandoMostrarInfoEnPantalla)).Single();
		_ = comando.Error.Equals(true);
		_ = comando.TextoPantalla.Equals("Lo Sentimos. En este momento no podemos procesar su transacción.\\n\\n\" +\r\n\t\t\t\t\t   \"Por favor intente más tarde...");

	}

	[Fact]
	public void Withdrawal_with_a_wrong_ping_fail()
	{
		// ARRANGE
		FakeConsoleWriter consoleWriter = new();
		IHSM hsm = new HSM();
		IATMSwitch sut = CrearSwitch(hsm, consoleWriter);
		IATM atm = CrearATMFalso("AJP001");
		RegistrarATMEnSwitch(atm, sut, hsm);
		IAutorizador autorizador = CrearAutorizador("AutDB", hsm, 10_000);
		RegistrarAutorizadorEnSwitch(autorizador, sut, hsm);

		string numeroTarjeta = CrearCuentaYTarjeta(autorizador, TipoCuenta.Ahorros, 20000, binTarjeta, "1234");

		byte[] llaveEnClaro = ((FakeATM)atm).Llave;
		byte[] criptogramaPin = Encriptar("3874", llaveEnClaro);

		// ACT
		List<Comando> comandosRespuesta = sut.Autorizar(atm, teclasRetiroConRecibo, numeroTarjeta, 200, criptogramaPin);

		// ASSERT
		ComandoMostrarInfoEnPantalla comando = (ComandoMostrarInfoEnPantalla)comandosRespuesta.Where(x => x.GetType() == typeof(ComandoMostrarInfoEnPantalla)).Single();

		_ = comandosRespuesta.Should().HaveCountGreaterThanOrEqualTo(1);
		_ = comandosRespuesta.Should().Contain(x => x.GetType() == typeof(ComandoMostrarInfoEnPantalla));
		_ = comando.TextoPantalla.Equals("Pin incorrecto");

	}

}
