using ATMSim;

public class TarjetaTests
{

	[Fact]
	public void Mask_creditCardNumber_should_be_correct()
	{
		// Arrange
		string numeroTarjeta = "1234567890123456";

		// Act
		string resultado = Tarjeta.EnmascararNumero(numeroTarjeta);

		// Assert
		Assert.Equal("123456******3456", resultado);
	}

	[Fact]
	public void Invalid_creditCardNumber_should_throw_an_exception()
	{
		// Arrange
		string numeroTarjeta = "1234567890"; // número inválido

		// Act & Assert
		_ = Assert.Throws<ArgumentException>(() => new Tarjeta(numeroTarjeta));
	}

	[Fact]
	public void Calculate_verification_number_should_be_the_proper_value()
	{
		// Arrange
		string numeroSinDigitoVerificador = "123456789012345";

		// Act
		int resultado = Tarjeta.CalcularDigitoVerificacion(numeroSinDigitoVerificador);

		// Assert
		Assert.Equal(2, resultado);
	}

	[Fact]
	public void Integrity_validation_should_be_true_with_the_proper_creditCardNumber()
	{
		// Arrange
		string numeroConDigitoVerificadorCorrecto = "1234567890123452";

		// Act
		bool resultado = Tarjeta.ValidarIntegridad(numeroConDigitoVerificadorCorrecto);

		// Assert
		Assert.True(resultado);
	}

	[Fact]
	public void Integrity_validation_should_be_False_with_the_proper_creditCardNumber()
	{
		// Arrange
		string numeroConDigitoVerificadorIncorrecto = "123456789012345";

		// Act
		bool resultado = Tarjeta.ValidarIntegridad(numeroConDigitoVerificadorIncorrecto);

		// Assert
		Assert.False(resultado);
	}
}