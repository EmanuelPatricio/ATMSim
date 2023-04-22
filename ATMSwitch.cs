using System.Text.RegularExpressions;

namespace ATMSim;

public class EntidadYaRegistradaException : Exception
{
	public EntidadYaRegistradaException() { }
	public EntidadYaRegistradaException(string mensaje) : base(mensaje) { }
	public EntidadYaRegistradaException(string mensaje, Exception innerException) : base(mensaje, innerException) { }
}
public class EntidadNoRegistradaException : Exception
{
	public EntidadNoRegistradaException() { }
	public EntidadNoRegistradaException(string mensaje) : base(mensaje) { }
	public EntidadNoRegistradaException(string mensaje, Exception innerException) : base(mensaje, innerException) { }
}

public class SecuenciaDeTeclasNoReconocidas : Exception
{
	public SecuenciaDeTeclasNoReconocidas() { }
	public SecuenciaDeTeclasNoReconocidas(string mensaje) : base(mensaje) { }
	public SecuenciaDeTeclasNoReconocidas(string mensaje, Exception innerException) : base(mensaje, innerException) { }
}

public class RutaNoDisponibleException : Exception
{
	public RutaNoDisponibleException() { }
	public RutaNoDisponibleException(string mensaje) : base(mensaje) { }
	public RutaNoDisponibleException(string mensaje, Exception innerException) : base(mensaje, innerException) { }
}

public enum TipoTransaccion
{
	Retiro,
	Consulta
}

public struct ConfiguracionOpKey
{
	public string Teclas { get; set; }
	public TipoTransaccion TipoTransaccion { get; set; }
	public bool Recibo { get; set; }
	public int? Monto { get; set; }
}

public struct Ruta
{
	string bin;
	public string Bin
	{
		get { return bin; }
		set
		{
			if (Regex.Match(value, @"[0-9]{1,18}").Success)
				bin = value;
			else
				throw new ArgumentException("Debe contener solo numeros");
		}
	}

	public string Destino { get; set; }
}

public interface IATMSwitch
{
	public void RegistrarATM(IATM atm, byte[] criptogramaLlave);
	public void AgregarConfiguracionOpKey(ConfiguracionOpKey configuracionOpKey);
	public void EliminarATM(IATM atm);
	public void RegistrarAutorizador(IAutorizador autorizador, byte[] criptogramaLlaveAutorizador);
	public void EliminarAutorizador(string nombreAutorizador);
	public void AgregarRuta(string bin, string nombreAutorizador);
	public List<Comando> Autorizar(IATM atm, string opKeyBuffer, string numeroTarjeta, int monto, byte[] criptogramaPin);


}

public class ATMSwitch : IATMSwitch
{
	private IHSM hsm;
	private Dictionary<string, byte[]> LlavesDeAtm { get; set; } = new Dictionary<string, byte[]>();
	private Dictionary<string, byte[]> LlavesDeAutorizador { get; set; } = new Dictionary<string, byte[]>();
	private Dictionary<string, IAutorizador> Autorizadores { get; set; } = new Dictionary<string, IAutorizador>();

	private List<Ruta> tablaRuteo = new();
	private List<ConfiguracionOpKey> tablaOpKeys = new();

	private IConsoleWriter consoleWriter;

	public ATMSwitch(IHSM hsm, IConsoleWriter consoleWriter)
	{
		this.hsm = hsm;
		this.consoleWriter = consoleWriter;
	}

	public bool CheckIfAtmExist(IATM atm)
	{
		return LlavesDeAtm.ContainsKey(atm.Nombre);
	}

	public void RegistrarATM(IATM atm, byte[] criptogramaLlave)
	{
		if (CheckIfAtmExist(atm))
			throw new EntidadYaRegistradaException($"El ATM {atm.Nombre} ya se encuentra registrado");

		LlavesDeAtm[atm.Nombre] = criptogramaLlave;
		atm.Switch = this;
	}

	public void AgregarConfiguracionOpKey(ConfiguracionOpKey configuracionOpKey)
	{
		// Si ya existe un ConfiguracionOpKey config con la misma combinación de teclas, reemplazarlo
		IEnumerable<ConfiguracionOpKey> queryOpKeyExistente = tablaOpKeys.Where(x => x.Teclas == configuracionOpKey.Teclas);
		if (queryOpKeyExistente.Any())
			_ = tablaOpKeys.Remove(queryOpKeyExistente.Single());

		tablaOpKeys.Add(configuracionOpKey);
	}

	public void EliminarATM(IATM atm)
	{
		if (!CheckIfAtmExist(atm))
			throw new EntidadNoRegistradaException($"El ATM {atm.Nombre} no se encuentra registrado");

		atm.Reestablecer();
		_ = LlavesDeAtm.Remove(atm.Nombre);
	}

	public List<Comando> Autorizar(IATM atm, string opKeyBuffer, string numeroTarjeta, int monto, byte[] criptogramaPin)
	{
		string MensageErrGenerico = "Lo Sentimos. En este momento no podemos procesar su transacción.\n\n" +
					   "Por favor intente más tarde...";

		ConfiguracionOpKey opKeyConfig;
		IAutorizador autorizador;
		try
		{
			opKeyConfig = DeterminarTipoDeTransaccion(opKeyBuffer);
			autorizador = DeterminarAutorizadorDestino(numeroTarjeta);
		}
		catch (Exception e)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(e.ToString());
			Console.ResetColor();
			return MostrarErrorGenerico(MensageErrGenerico);
		}

		if (autorizador.TarjetaBloqueada(numeroTarjeta))
		{
			return MostrarErrorGenerico("Al parecer su tarjeta se encuentra bloqueada, por favor comuniquese con el personal pertinente...");
		}

		if (!LlavesDeAtm.ContainsKey(atm.Nombre) || !LlavesDeAutorizador.ContainsKey(autorizador.Nombre)) // Extract Method
			return MostrarErrorGenerico(MensageErrGenerico);

		byte[] criptogramaLlaveOrigen = LlavesDeAtm[atm.Nombre];
		byte[] criptogramaLlaveDestino = LlavesDeAutorizador[autorizador.Nombre];

		byte[] criptogramaTraducidoPin = hsm.TraducirPin(criptogramaPin, criptogramaLlaveOrigen, criptogramaLlaveDestino);


		switch (opKeyConfig.TipoTransaccion)
		{
			case TipoTransaccion.Retiro:
				return AutorizarRetiro(atm, numeroTarjeta, monto, criptogramaTraducidoPin, autorizador, opKeyConfig);
			case TipoTransaccion.Consulta:
				return AutorizarConsulta(atm, numeroTarjeta, criptogramaTraducidoPin, autorizador, opKeyConfig);
			default:
				return MostrarErrorGenerico(MensageErrGenerico);

		}

	}	

	//remove MostrarErrorTaretaBloqueada
	private List<Comando> MostrarErrorGenerico(string errMessage)
	{
		List<Comando> comandos = new()
		{
			new ComandoMostrarInfoEnPantalla(errMessage, true)
		};
		return comandos;
	}

	private List<Comando> AutorizarRetiro(IATM atm, string numeroTarjeta, int monto, byte[] criptogramaPin, IAutorizador autorizador, ConfiguracionOpKey opKeyConfig)
	{
		List<Comando> comandos = new();

		monto = monto == 0 ? opKeyConfig.Monto ?? 0 : monto;

		if (monto == 0)
		{
			comandos.Add(new ComandoMostrarInfoEnPantalla("Monto inválido para retiro", true));
			return comandos;
		}


		RespuestaRetiro respuesta = autorizador.AutorizarRetiro(numeroTarjeta, monto, criptogramaPin);


		switch (respuesta.CodigoRespuesta)
		{
			case 0:
				comandos.Add(new ComandoMostrarInfoEnPantalla("Espere mientras se dispensa su dinero", false));
				comandos.Add(new ComandoDispensarEfectivo(respuesta.MontoAutorizado ?? 0));
				comandos.Add(new ComandoMostrarInfoEnPantalla("Favor de retirar su tarjeta", false));
				comandos.Add(new ComandoDevolverTarjeta());
				if (opKeyConfig.Recibo)
				{
					comandos.Add(new ComandoMostrarInfoEnPantalla("Imprimiento su recibo", false));

					comandos.Add(new ComandoImprimirRecibo($"Fecha: {DateTime.Today: g}\n" +
														   $"ATM: {atm.Nombre}\n" +
														   $"Monto Retirado: {respuesta.MontoAutorizado}\n" +
														   $"Balance Actual: {respuesta.BalanceLuegoDelRetiro}"));
				}
				break;
			case 51:
				comandos.Add(new ComandoMostrarInfoEnPantalla("Su cuenta no posee balance suficiente para realizar el retiro", true));
				break;
			case 55:
				comandos.Add(new ComandoMostrarInfoEnPantalla("Pin incorrecto", true));
				break;
			case 56:
				comandos.Add(new ComandoMostrarInfoEnPantalla("Tarjeta no reconocida", true));
				break;
			default:
				comandos.Add(new ComandoMostrarInfoEnPantalla("Su transacción no puede ser procesada. Por favor intente más tarde.", true));
				break;
		}

		return comandos;
	}

	private List<Comando> AutorizarConsulta(IATM atm, string numeroTarjeta, byte[] criptogramaPin, IAutorizador autorizador, ConfiguracionOpKey opKeyConfig)
	{
		List<Comando> comandos = new();

		RespuestaConsultaDeBalance respuesta = autorizador.ConsultarBalance(numeroTarjeta, criptogramaPin);

		switch (respuesta.CodigoRespuesta)
		{
			case 0:
				comandos.Add(new ComandoMostrarInfoEnPantalla($"Su balance actual es de: {respuesta.BalanceActual}", false));
				break;
			case 55:
				comandos.Add(new ComandoMostrarInfoEnPantalla("Pin incorrecto", true));
				break;
			case 56:
				comandos.Add(new ComandoMostrarInfoEnPantalla("Tarjeta no reconocida", true));
				break;
			default:
				comandos.Add(new ComandoMostrarInfoEnPantalla("Su transacción no puede ser procesada. Por favor intente más tarde.", true));
				break;
		}

		return comandos;
	}

	public void RegistrarAutorizador(IAutorizador autorizador, byte[] criptogramaLlaveAutorizador)
	{
		if (Autorizadores.ContainsKey(autorizador.Nombre)) // mejorar 
			throw new EntidadYaRegistradaException($"El Autorizador {autorizador.Nombre} ya se encuentra registrado");


		Autorizadores[autorizador.Nombre] = autorizador;
		LlavesDeAutorizador[autorizador.Nombre] = criptogramaLlaveAutorizador;
	}

	public void EliminarAutorizador(string nombreAutorizador)
	{
		if (!Autorizadores.ContainsKey(nombreAutorizador)) // mejorar
			throw new EntidadNoRegistradaException($"El Autorizador {nombreAutorizador} no se encuentra registrado");

		_ = Autorizadores.Remove(nombreAutorizador);
		_ = LlavesDeAutorizador.Remove(nombreAutorizador);
	}

	private IAutorizador DeterminarAutorizadorDestino(string numeroTarjeta) // mejorar
	{
		string nombreAutorizador;
		try
		{
			nombreAutorizador = tablaRuteo.Where(x => numeroTarjeta.StartsWith(x.Bin))
										  .OrderByDescending(x => x.Bin.Length)
										  .ThenBy(x => x.Destino)
										  .First()
										  .Destino;
		}
		catch (InvalidOperationException e) // si no se encuentra ninguna
		{
			throw new RutaNoDisponibleException($"No se encontró una ruta para el emisor de la tarjeta {Tarjeta.EnmascararNumero(numeroTarjeta)}", e);
		}
		try
		{
			return Autorizadores[nombreAutorizador];
		}
		catch (KeyNotFoundException e) // si no se encuentra ninguna
		{
			throw new EntidadNoRegistradaException($"El autorizador {nombreAutorizador} no se encuentra correctamente registrado", e);
		}
	}

	private ConfiguracionOpKey DeterminarTipoDeTransaccion(string opKeyBuffer)
	{
		try
		{
			return tablaOpKeys.Where(x => x.Teclas == opKeyBuffer).Single(); // validar, creo que esto se ejecuta demasiado 
		}
		catch (InvalidOperationException e) // si no se encuentra ninguna
		{
			throw new SecuenciaDeTeclasNoReconocidas($"No se reconoce la secuencia de letras {opKeyBuffer}", e);
		}
	}

	public void AgregarRuta(string bin, string nombreAutorizador)
	{
		if (!Autorizadores.ContainsKey(nombreAutorizador)) //mejorar
			throw new EntidadNoRegistradaException($"El Autorizador {nombreAutorizador} no se encuentra registrado");

		// Si existe una ruta con el mismo bin, reemplazar destino
		IEnumerable<Ruta> rutaExistentes = tablaRuteo.Where(x => x.Bin == bin);
		if (rutaExistentes.Any())
		{
			Ruta rutaExistente = rutaExistentes.Single();
			rutaExistente.Destino = "nombreAutorizador";
		}
		else
			// Si no existe el bin en la tabla de bines, agregarlo
			tablaRuteo.Add(new Ruta { Bin = bin, Destino = nombreAutorizador });

	}

}
