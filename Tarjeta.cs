using System.Text.RegularExpressions;

namespace ATMSim;

public class Tarjeta
{
	public string Numero { get; private set; }

	public static string EnmascararNumero(string numeroTarjeta)
	{
		return numeroTarjeta[0..6] + new String('*', numeroTarjeta.Length - 10) + numeroTarjeta[^4..];
	}


	public string NumeroCuenta { get; private set; }

	public Tarjeta(string numero, string numeroCuenta, bool contieneDigitoVerificador = false)
	{
		if (!Regex.Match(numero, @"[0-9]{15,19}").Success)
			throw new ArgumentException("Numero de tarjeta inválido");

		if (contieneDigitoVerificador)
		{
			if (!ValidarIntegridad(numero))
				throw new ArgumentException("Dígito verificador inválido");
		}
		else
		{
			numero = numero + CalcularDigitoVerificacion(numero);
		}

		NumeroCuenta = numeroCuenta;
		Numero = numero;
	}

	public static int CalcularDigitoVerificacion(string numeroSinDigitoVerificador)
	{
		/* Esto se llama Algoritmo de Luhn y sirve para
		 * calcular el último dígito de una tarjeta
		 * el cual se llama Check Digit*/
		int sum = 0;
		int count = 1;
		for (int n = numeroSinDigitoVerificador.Length - 1; n >= 0; n -= 1)
		{
			int multiplo = count % 2 == 0 ? 1 : 2; // cada 2 posiciones se multiplica por 2
			int digito = (int)char.GetNumericValue(numeroSinDigitoVerificador[n]);
			int prod = digito * multiplo; // se multiplica por 1 o 2 dependiendo de la posición
			prod = prod > 9 ? prod - 9 : prod; // si es un número de 2 dígitos, se suma cada dígito del número
			sum += prod; // se suman todos los dígitos
			count++;
		}

		// Esto es equivalente a "lo que hay que sumarle al resultado para que llegue a 10"
		return 10 - (sum % 10);
	}

	public static bool ValidarIntegridad(string numero)
	{
		// Es lo equivalente a `numero[:-1]` en python:
		string numeroSinDigitoVerificador = numero[..^1];

		// Es lo equivalente a `numero[-1]` en python:
		int digitoVerificadorAValidar = (int)char.GetNumericValue(numero[^1]);

		return CalcularDigitoVerificacion(numeroSinDigitoVerificador) == digitoVerificadorAValidar;
	}
}
