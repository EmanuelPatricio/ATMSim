using ATMSim;
using FluentAssertions;
using System.Security.Cryptography;

namespace ATMSimTests;

public class AuthorizerTests
{
	private static string CrearCuentaYTarjeta(IAutorizador autorizador, TipoCuenta tipoCuenta, decimal balanceInicial, decimal limiteSobregiro, string binTarjeta, string pin)
	{
		string numeroCuenta = autorizador.CrearCuenta(tipoCuenta, balanceInicial, limiteSobregiro);
		string numeroTarjeta = autorizador.CrearTarjeta(binTarjeta, numeroCuenta);
		autorizador.AsignarPin(numeroTarjeta, pin);
		return numeroTarjeta;
	}

	private static IAutorizador CrearAutorizador(string nombre, IHSM hsm, decimal limite) => new Autorizador(nombre, hsm, limite);

	private static (IAutorizador SUT, string NumeroTarjeta, byte[] CriptogramaPin) Arrange(decimal limiteTransaccion, TipoCuenta tipoCuenta, decimal balanceInicial, decimal limiteSobregiro, string binTarjeta, string pinTarjeta, string textoPlano)
	{
		IHSM hsm = new HSM();
		IAutorizador sut = CrearAutorizador("Autorizador", hsm, limiteTransaccion);
		ComponentesLlave llave = hsm.GenerarLlave();
		sut.InstalarLlave(llave.LlaveEncriptada);
		string numeroTarjeta = CrearCuentaYTarjeta(sut, tipoCuenta, balanceInicial, limiteSobregiro, binTarjeta, pinTarjeta);
		byte[] criptogramaPin = Encriptar(textoPlano, llave.LlaveEnClaro);

		return (sut, numeroTarjeta, criptogramaPin);
	}

	private static byte[] Encriptar(string textoPlano, byte[] llaveEnClaro)
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
	public void Accounts_of_type_checking_allow_overdraft()
	{
		// ARRANGE
		var (sut, numeroTarjeta, criptogramaPin) = Arrange(20_000, TipoCuenta.Corriente, 10_000, 0, "455555", "1234", "1234");

		// ACT
		RespuestaRetiro respuesta = sut.AutorizarRetiro(numeroTarjeta, 15_500, criptogramaPin);

		// ASSERT
		_ = respuesta.MontoAutorizado.Should().Be(15_500);
		_ = respuesta.BalanceLuegoDelRetiro.Should().Be(-5_500);
		_ = respuesta.CodigoRespuesta.Should().Be(RespuestaOperacion.Exito);
	}

	[Fact]
	public void Balance_Inquiry_with_incorrect_pin_return_respcode_55()
	{
		// ARRANGE
		var (sut, numeroTarjeta, criptogramaPinIncorrecto) = Arrange(10_000, TipoCuenta.Corriente, 10_000, 0, "455555", "1234", "9999");

		// ACT
		RespuestaConsultaDeBalance respuesta = sut.ConsultarBalance(numeroTarjeta, criptogramaPinIncorrecto);

		// ASSERT
		_ = respuesta.CodigoRespuesta.Should().Be(RespuestaOperacion.PinIncorrecto);
		_ = respuesta.BalanceActual.Should().BeNull();
	}

	// Mis pruebas
	[Fact]
	public void AccountOfTypeSavingsDoesntAllowOverdraftAndReturns51()
	{
		// ARRANGE
		var (sut, numeroTarjeta, criptogramaPin) = Arrange(10_000, TipoCuenta.Ahorros, 10_000, 0, "455555", "1234", "1234");

		// ACT
		RespuestaRetiro respuesta = sut.AutorizarRetiro(numeroTarjeta, 15_500, criptogramaPin);

		// ASSERT
		_ = respuesta.CodigoRespuesta.Should().Be(RespuestaOperacion.FondosInsuficientes);
	}

	[Fact]
	public void BalanceInquiryWithCorrectPinReturnInitialBalance()
	{
		// ARRANGE
		var (sut, numeroTarjeta, criptogramaPinIncorrecto) = Arrange(10_000, TipoCuenta.Corriente, 10_000, 0, "455555", "1234", "1234");

		// ACT
		RespuestaConsultaDeBalance respuesta = sut.ConsultarBalance(numeroTarjeta, criptogramaPinIncorrecto);

		// ASSERT
		_ = respuesta.CodigoRespuesta.Should().Be(RespuestaOperacion.Exito);
		_ = respuesta.BalanceActual.Should().Be(10_000);
	}

	[Fact]
	public void CrearCuentaMethodShouldReturnAccountNumber()
	{
		// ARRANGE
		IHSM hsm = new HSM();
		IAutorizador sut = CrearAutorizador("Autorizador", hsm, 10_000);

		// ACT
		var numeroCuenta = sut.CrearCuenta(TipoCuenta.Corriente);

		// ASSERT
		_ = numeroCuenta.Should().NotBeNull();
	}

	[Fact]
	public void CrearTarjetaMethodShouldReturnCardNumber()
	{
		// ARRANGE
		IHSM hsm = new HSM();
		IAutorizador sut = CrearAutorizador("Autorizador", hsm, 10_000);
		var numeroCuenta = sut.CrearCuenta(TipoCuenta.Corriente);

		// ACT
		var numeroTarjeta = sut.CrearTarjeta("455555", numeroCuenta);

		// ASSERT
		_ = numeroTarjeta.Should().NotBeNull();
	}

	[Fact]
	public void ATMTransactionLimitReachedShouldReturnStatus50()
	{
		// ARRANGE
		var (sut, numeroTarjeta, criptogramaPin) = Arrange(10_000, TipoCuenta.Ahorros, 20_000, 0, "455555", "1234", "1234");

		// ACT
		RespuestaRetiro respuesta = sut.AutorizarRetiro(numeroTarjeta, 15_500, criptogramaPin);

		// ASSERT
		_ = respuesta.CodigoRespuesta.Should().Be(RespuestaOperacion.LimiteTransaccion);
	}

	[Fact]
	public void AccountOfTypeCheckingReachedOverdraftLimitShouldReturnStatus51()
	{
		// ARRANGE
		var (sut, numeroTarjeta, criptogramaPin) = Arrange(50_000, TipoCuenta.Corriente, 20_000, 10_000, "455555", "1234", "1234");

		// ACT
		RespuestaRetiro respuesta = sut.AutorizarRetiro(numeroTarjeta, 35_000, criptogramaPin);

		// ASSERT
		_ = respuesta.CodigoRespuesta.Should().Be(RespuestaOperacion.FondosInsuficientes);
	}

	[Fact]
	public void BlockedCardPreventsTransactionsShouldReturnStatus49()
	{
		// ARRANGE
		var (sut, numeroTarjeta, criptogramaPin) = Arrange(50_000, TipoCuenta.Corriente, 20_000, 10_000, "455555", "1234", "1234");
		sut.BloquearTarjeta(numeroTarjeta);

		// ACT
		RespuestaRetiro respuesta = sut.AutorizarRetiro(numeroTarjeta, 1_000, criptogramaPin);

		// ASSERT
		_ = respuesta.CodigoRespuesta.Should().Be(RespuestaOperacion.TarjetaBloqueada);
	}

	[Fact]
	public void AccountWithdrawAllowsTwoDecimalPlacesShouldReturnStatus0()
	{
		// ARRANGE
		var (sut, numeroTarjeta, criptogramaPin) = Arrange(50_000, TipoCuenta.Corriente, 20_000.74M, 10_000, "455555", "1234", "1234");

		// ACT
		RespuestaRetiro respuesta = sut.AutorizarRetiro(numeroTarjeta, 99.53M, criptogramaPin);

		// ASSERT
		_ = respuesta.CodigoRespuesta.Should().Be(RespuestaOperacion.Exito);
		_ = respuesta.BalanceLuegoDelRetiro.Should().Be(19_901.21M);
	}

	[Fact]
	public void AuthorizerStablishCardAndAccountRelashionshipShouldBeTrue()
	{
		// ARRANGE
		IHSM hsm = new HSM();
		IAutorizador sut = CrearAutorizador("Autorizador", hsm, 10_000);
		var numeroCuenta = sut.CrearCuenta(TipoCuenta.Corriente);
		var numeroTarjeta = sut.CrearTarjeta("455555", numeroCuenta);

		// ACT
		var esTarjetaAsignadaACuenta = sut.EsTarjetaConCuenta(numeroTarjeta);

		// ASSERT
		Assert.True(esTarjetaAsignadaACuenta);
	}
}