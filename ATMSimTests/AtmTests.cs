using ATMSim;
using ATMSimTests.Fakes;
using FluentAssertions;
//Testing Actions
namespace ATMSimTests;

public class AtmTests
{
	private const string teclasRetiroConRecibo = "AAA";
	private const string teclasRetiroSinRecibo = "AAC";
	private const string teclasConsultaDeBalance = "B";

	private static IATM CrearATM(string nombre, IConsoleWriter consoleWriter, IThreadSleeper threadSleeper)
		=> new ATM(nombre, consoleWriter, threadSleeper);

	private static string CrearCuentaYTarjeta(IAutorizador autorizador, TipoCuenta tipoCuenta, int balanceInicial, decimal limiteSobregiro, string binTarjeta, string pin)
	{
		string numeroCuenta = autorizador.CrearCuenta(tipoCuenta, balanceInicial, limiteSobregiro);
		string numeroTarjeta = autorizador.CrearTarjeta(binTarjeta, numeroCuenta);
		autorizador.AsignarPin(numeroTarjeta, pin);
		return numeroTarjeta;
	}

	private static void RegistrarATMEnSwitch(IATM atm, IATMSwitch atmSwitch, IHSM hsm)
	{
		ComponentesLlave llaveATM = hsm.GenerarLlave();
		atm.InstalarLlave(llaveATM.LlaveEnClaro);
		atmSwitch.RegistrarATM(atm, llaveATM.LlaveEncriptada);
	}

	private static IAutorizador CrearAutorizador(string nombre, IHSM hsm, decimal limite) => new Autorizador(nombre, hsm, limite);

	private static void RegistrarAutorizadorEnSwitch(IAutorizador autorizador, IATMSwitch atmSwitch, IHSM hsm)
	{
		ComponentesLlave llaveAutorizador = hsm.GenerarLlave();
		autorizador.InstalarLlave(llaveAutorizador.LlaveEncriptada);
		atmSwitch.RegistrarAutorizador(autorizador, llaveAutorizador.LlaveEncriptada);
		atmSwitch.AgregarRuta("459413", autorizador.Nombre);
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


	[Fact]
	public void Withdrawal_with_balance_on_account_is_successful()
	{
		// ARRANGE
		FakeConsoleWriter consoleWriter = new();
		FakeThreadSleeper threadSleeper = new();

		IHSM hsm = new HSM();

		IATMSwitch atmSwitch = CrearSwitch(hsm, consoleWriter);

		IATM sut = CrearATM("AJP001", consoleWriter, threadSleeper);
		RegistrarATMEnSwitch(sut, atmSwitch, hsm);

		IAutorizador autorizador = CrearAutorizador("AutDB", hsm, 10_000);
		RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

		string numeroTarjeta = CrearCuentaYTarjeta(autorizador, TipoCuenta.Ahorros, 20_000, 0, "459413", "1234");

		// ACT
		sut.EnviarTransactionRequest("AAA", numeroTarjeta, "1234", 100);

		// ASSERT
		_ = consoleWriter.consoleText.Should().Contain("> Efectivo dispensado: 100");
	}

	[Fact]
	public void BlockedCardWithdrawalNotSuccessful()
	{
		// ARRANGE
		FakeConsoleWriter consoleWriter = new();
		FakeThreadSleeper threadSleeper = new();

		IHSM hsm = new HSM();

		IATMSwitch atmSwitch = CrearSwitch(hsm, consoleWriter);

		IATM sut = CrearATM("AJP001", consoleWriter, threadSleeper);
		RegistrarATMEnSwitch(sut, atmSwitch, hsm);

		IAutorizador autorizador = CrearAutorizador("AutDB", hsm, 10_000);
		string numeroTarjeta = CrearCuentaYTarjeta(autorizador, TipoCuenta.Ahorros, 20_000, 0, "459413", "1234");
		autorizador.BloquearTarjeta(numeroTarjeta);

		RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

		// ACT
		sut.EnviarTransactionRequest("AAA", numeroTarjeta, "1234", 100);

		// ASSERT
		_ = consoleWriter.consoleText.Should().Contain("Al parecer su tarjeta se encuentra bloqueada, por favor comuniquese con el personal pertinente...");
	}
}