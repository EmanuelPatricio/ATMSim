using System.Text.RegularExpressions;

namespace ATMSim;

public class IntentoSobregiroCuentaDeAhorrosException : Exception { }
public enum TipoCuenta
{
	Ahorros,
	Corriente
}

internal class Cuenta
{
	public TipoCuenta Tipo { get; private set; }
	public string Numero { get; private set; }

	private decimal monto = 0;
	public decimal Monto
	{
		get { return monto; }
		set
		{
			if (Tipo == TipoCuenta.Ahorros && value < 0)
				throw new IntentoSobregiroCuentaDeAhorrosException();
			else
				monto = value;
		}
	}

	private decimal limiteSobregiro;
	public decimal LimiteSobregiro
	{
		get
		{
			return Tipo == TipoCuenta.Ahorros ? 0.00M : -Math.Abs(limiteSobregiro);
		}
		set
		{
			limiteSobregiro = Tipo == TipoCuenta.Ahorros ? 0.00M : -Math.Abs(value);
		}
	}

	public Cuenta(string numero, TipoCuenta tipo, decimal monto = 0, decimal limite = 0.00M)
	{
		if (!Regex.Match(numero, @"[0-9]+").Success)
			throw new ArgumentException("Numero de cuenta inválido");

		Numero = numero;
		Tipo = tipo;
		Monto = monto;
		LimiteSobregiro = limite;
	}

}
