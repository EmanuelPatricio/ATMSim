using System.Text.RegularExpressions;

namespace ATMSim;

public interface IAutorizador
{
	public RespuestaConsultaDeBalance ConsultarBalance(string numeroTarjeta, byte[] criptogramaPin);
	public RespuestaRetiro AutorizarRetiro(string numeroTarjeta, int montoRetiro, byte[] criptogramaPin);
	public string CrearTarjeta(string bin, string numeroCuenta);
	public string CrearCuenta(TipoCuenta tipo, int montoDeApertura = 0, decimal limiteSobregiro = 0.00M);
	public string Nombre { get; }
	public void AsignarPin(string numeroTarjeta, string pin);
	public void InstalarLlave(byte[] criptogramaLlaveAutorizador);
	void BloquearTarjeta(string numeroTarjeta);
	bool TarjetaBloqueada(string numeroTarjeta);
}

public class Respuesta
{
	public int CodigoRespuesta { get; private set; }
	public Respuesta(int codigoRespuesta) => CodigoRespuesta = codigoRespuesta;
}

public class RespuestaConsultaDeBalance : Respuesta
{
	public int? BalanceActual { get; private set; }
	public RespuestaConsultaDeBalance(int codigoRespuesta, int? balanceActual = null) : base(codigoRespuesta)
		=> BalanceActual = balanceActual;
}

public class RespuestaRetiro : Respuesta
{
	public int? MontoAutorizado { get; private set; }
	public int? BalanceLuegoDelRetiro { get; private set; }
	public RespuestaRetiro(int codigoRespuesta, int? montoAutorizado = null, int? balanceLuegoDelRetiro = null) : base(codigoRespuesta)
		=> (MontoAutorizado, BalanceLuegoDelRetiro) = (montoAutorizado, balanceLuegoDelRetiro);
}

public class Autorizador : IAutorizador
{
	private const int tamanoNumeroTarjeta = 16;
	private const int tamanoNumeroCuenta = 9;
	private const string prefijoDeCuenta = "7";

	private Random random = new();
	public string Nombre { get; private set; }

	private IHSM hsm;
	private List<Tarjeta> tarjetas = new();
	private List<Cuenta> cuentas = new();
	private Dictionary<string, byte[]> pinesTarjetas = new();
	private Dictionary<string, bool> bloqueoTarjetas = new();

	private byte[]? criptogramaLlaveAutorizador;
	private decimal limiteTransaccion;

	public Autorizador(string nombre, IHSM hsm, decimal limiteTransaccion)
	{
		Nombre = nombre;
		this.hsm = hsm;
		this.limiteTransaccion = limiteTransaccion;
	}

	public void AsignarPin(string numeroTarjeta, string pin)
	{
		if (!Regex.Match(pin, @"[0-9]{4}").Success)
			throw new ArgumentException("El Pin debe ser numérico, de 4 dígitos");

		if (!TarjetaExiste(numeroTarjeta))
			throw new ArgumentException("Número de tarjeta no reconocido");

		byte[] criptogramaPin = hsm.EncriptarPinConLlaveMaestra(pin);

		pinesTarjetas[numeroTarjeta] = criptogramaPin;
	}

	public void BloquearTarjeta(string numeroTarjeta) => bloqueoTarjetas[numeroTarjeta] = true;
	public bool TarjetaBloqueada(string numeroTarjeta) => bloqueoTarjetas[numeroTarjeta];

	private bool TarjetaExiste(string numeroTarjeta) => tarjetas.Where(x => x.Numero == numeroTarjeta).Any();

	private Tarjeta ObtenerTarjeta(string numeroTarjeta) => tarjetas.Where(x => x.Numero == numeroTarjeta).Single();

	private byte[] ObtenerCriptogramaPinTarjeta(string numeroTarjeta) => pinesTarjetas[numeroTarjeta];

	private bool TarjetaTienePin(string numeroTarjeta) => pinesTarjetas.ContainsKey(numeroTarjeta);

	private bool CuentaExiste(string numeroCuenta) => cuentas.Where(x => x.Numero == numeroCuenta).Any();

	private Cuenta ObtenerCuenta(string numeroCuenta) => cuentas.Where(x => x.Numero == numeroCuenta).Single();

	public RespuestaConsultaDeBalance ConsultarBalance(string numeroTarjeta, byte[] criptogramaPin)
	{
		var (consultaValida, codigoError) = EsConsultaValida(numeroTarjeta, criptogramaPin);

		if (!consultaValida)
		{
			return new RespuestaConsultaDeBalance(codigoError);
		}

		Tarjeta tarjeta = ObtenerTarjeta(numeroTarjeta);
		Cuenta cuenta = ObtenerCuenta(tarjeta.NumeroCuenta);

		return new RespuestaConsultaDeBalance(0, cuenta.Monto); // Autorizado
	}

	public RespuestaRetiro AutorizarRetiro(string numeroTarjeta, int montoRetiro, byte[] criptogramaPin)
	{
		var (consultaValida, codigoError) = EsConsultaValida(numeroTarjeta, criptogramaPin);

		if (!consultaValida)
		{
			return new RespuestaRetiro(codigoError);
		}

		if (bloqueoTarjetas[numeroTarjeta])
		{
			return new RespuestaRetiro(49); // Tarjeta bloqueada
		}

		Tarjeta tarjeta = ObtenerTarjeta(numeroTarjeta);
		Cuenta cuenta = ObtenerCuenta(tarjeta.NumeroCuenta);

		if (cuenta.Tipo == TipoCuenta.Ahorros && cuenta.Monto < montoRetiro)
			return new RespuestaRetiro(51); // Fondos Insuficientes

		if (limiteTransaccion < montoRetiro)
			return new RespuestaRetiro(50); // Limite de transaccion alcanzado

		if (cuenta.Monto - montoRetiro < cuenta.LimiteSobregiro && cuenta.LimiteSobregiro != 0)
			return new RespuestaRetiro(51); // Fondos Insuficientes

		cuenta.Monto -= montoRetiro;
		return new RespuestaRetiro(0, montoRetiro, cuenta.Monto); // Autorizado

	}

	public void InstalarLlave(byte[] criptogramaLlaveAutorizador)
	{
		this.criptogramaLlaveAutorizador = criptogramaLlaveAutorizador;
	}

	public string CrearTarjeta(string bin, string numeroCuenta)
	{
		if (!Regex.Match(bin, @"[0-9]{6}").Success)
			throw new ArgumentException("El Bin debe ser numérico, de 6 dígitos");

		if (bin[0] != '4')
			throw new NotImplementedException("Sólo se soportan tarjertas VISA, que inician con 4");

		if (!cuentas.Where(x => x.Numero == numeroCuenta).Any())
			throw new NotImplementedException("Número de cuenta no encontrado");

		string numeroSinDigitoVerificador;
		do
		{
			// repetir hasta encontrar un número único (sin tomar en cuenta el digito verificador)
			numeroSinDigitoVerificador = GenerarNumeroAleatorio(tamanoNumeroTarjeta - 1, bin);
		}
		while (tarjetas.Where(x => x.Numero[..^1] == numeroSinDigitoVerificador).Any());

		Tarjeta tarjeta = new(numeroSinDigitoVerificador, numeroCuenta);
		tarjetas.Add(tarjeta);
		bloqueoTarjetas[tarjeta.Numero] = false;

		return tarjeta.Numero;
	}

	public string CrearCuenta(TipoCuenta tipo, int montoDeApertura = 0, decimal limiteSobregiro = 0.00M)
	{
		string numero;
		do
		{
			// repetir hasta encontrar un número único
			numero = GenerarNumeroAleatorio(tamanoNumeroCuenta, prefijoDeCuenta);
		}
		while (CuentaExiste(numero));

		Cuenta cuenta = new(numero, tipo, montoDeApertura, limiteSobregiro);
		cuentas.Add(cuenta);

		return cuenta.Numero;
	}

	private string GenerarNumeroAleatorio(int cantidadPosiciones, string prefijo = "", string sufijo = "")
	{
		const string digitos = "0123456789";

		if (!Regex.Match(prefijo + sufijo, @"[0-9]+").Success)
			throw new ArgumentException("El Sufijo y el Prefijo deben ser caracteres numéricos");

		if (cantidadPosiciones <= prefijo.Length + sufijo.Length)
			throw new ArgumentException("Debe haber al menos una posición que no sean parte del prefijo/sufijo");

		// Arreglar el length
		string numero = new(Enumerable.Repeat(digitos, cantidadPosiciones - prefijo.Length - sufijo.Length)
											  .Select(s => s[random.Next(s.Length)])
											  .ToArray());
		return prefijo + numero + sufijo;

	}

	private Tuple<bool, int> EsConsultaValida(string numeroTarjeta, byte[] criptogramaPin)
	{
		if (!TarjetaExiste(numeroTarjeta))
			return new(false, 56); // Esta tarjeta no se reconoce

		if (!TarjetaTienePin(numeroTarjeta))
			return new(false, 55); // Esta tarjeta no tiene pin asignado

		byte[] criptogramaPinReal = ObtenerCriptogramaPinTarjeta(numeroTarjeta);

		if (!hsm.ValidarPin(criptogramaPin, criptogramaLlaveAutorizador, criptogramaPinReal))
			return new(false, 55); // Pin incorrecto

		return new(true, 0);
	}
}
