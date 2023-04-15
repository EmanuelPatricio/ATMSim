using ATMSim;

////////////////////////////////// CONSTANTS //////////////////////////////////


const string pin = "1234";
const string pinIncorrecto = "9999";
const string binTarjeta = "459413";

const TipoCuenta tipoDeCuenta = TipoCuenta.Ahorros;
const int balanceInicialCuenta = 20_000;

const string teclasRetiroConRecibo = "AAA";
const string teclasRetiroSinRecibo = "AAC";
const string teclasConsultaDeBalance = "B";

/////////////////////////////////////// MAIN //////////////////////////////////


IConsoleWriter consoleWriter = new ConsoleWriter();
IThreadSleeper threadSleeper = new ThreadSleeper();

IHSM hsm = new HSM();
IATMSwitch atmSwitch = CrearSwitch(hsm, consoleWriter);

IATM atm = CrearATM("AJP001", consoleWriter, threadSleeper);
RegistrarATMEnSwitch(atm, atmSwitch, hsm);

IAutorizador autorizador = CrearAutorizador("AutDB", hsm);
RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

string numeroTarjeta = CrearCuentaYTarjeta(autorizador, tipoDeCuenta, balanceInicialCuenta, binTarjeta, pin);

SecuenciaDeTransaccionesDeEjemplo(atm, numeroTarjeta);


//////////////////////////////// SETUP HELPER METHODS /////////////////////////
static IATM CrearATM(string nombre, IConsoleWriter consoleWriter, IThreadSleeper threadSleeper)
	=> new ATM(nombre, consoleWriter, threadSleeper);


static string CrearCuentaYTarjeta(IAutorizador autorizador, TipoCuenta tipoCuenta, int balanceInicial, string binTarjeta, string pin)
{
	string numeroCuenta = autorizador.CrearCuenta(tipoCuenta, balanceInicial);
	string numeroTarjeta = autorizador.CrearTarjeta(binTarjeta, numeroCuenta);
	autorizador.AsignarPin(numeroTarjeta, pin);
	return numeroTarjeta;
}


static void RegistrarATMEnSwitch(IATM atm, IATMSwitch atmSwitch, IHSM hsm)
{
	ComponentesLlave llaveATM = hsm.GenerarLlave();
	atm.InstalarLlave(llaveATM.LlaveEnClaro);
	atmSwitch.RegistrarATM(atm, llaveATM.LlaveEncriptada);
}

static IAutorizador CrearAutorizador(string nombre, IHSM hsm) => new Autorizador(nombre, hsm);
static void RegistrarAutorizadorEnSwitch(IAutorizador autorizador, IATMSwitch atmSwitch, IHSM hsm)
{
	ComponentesLlave llaveAutorizador = hsm.GenerarLlave();
	autorizador.InstalarLlave(llaveAutorizador.LlaveEncriptada);
	atmSwitch.RegistrarAutorizador(autorizador, llaveAutorizador.LlaveEncriptada);
	atmSwitch.AgregarRuta("459413", autorizador.Nombre);
}


static IATMSwitch CrearSwitch(IHSM hsm, IConsoleWriter consoleWriter)
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

//////////////////////////////// DEMO SEQUENCE /////////////////////////

static void SecuenciaDeTransaccionesDeEjemplo(IATM atm, string numeroTarjeta)
{
	EsperarTeclaEnter("Presione ENTER para realizar una consulta de balance");
	atm.EnviarTransactionRequest(teclasConsultaDeBalance, numeroTarjeta, pin);

	EsperarTeclaEnter("Presione ENTER para realizar un retiro de 12,000 sin impresión de recibo");
	atm.EnviarTransactionRequest(teclasRetiroSinRecibo, numeroTarjeta, pin, 12_000);

	EsperarTeclaEnter("Presione ENTER para realizar un intento retiro de 6,000 pero con pin incorrecto");
	atm.EnviarTransactionRequest(teclasRetiroConRecibo, numeroTarjeta, pinIncorrecto, 6_000);

	EsperarTeclaEnter("Presione ENTER para realizar una consulta de balance");
	atm.EnviarTransactionRequest(teclasConsultaDeBalance, numeroTarjeta, pin);

	EsperarTeclaEnter("Presione ENTER para realizar un retiro de 6,500 con recibo");
	atm.EnviarTransactionRequest(teclasRetiroConRecibo, numeroTarjeta, pin, 6_500);

	EsperarTeclaEnter("Presione ENTER para realizar un intento de retiro de 4_000 que declinará por fondos insuficientes");
	atm.EnviarTransactionRequest(teclasRetiroConRecibo, numeroTarjeta, pin, 4_000);

	EsperarTeclaEnter("Presione ENTER para realizar un retiro de 12,000 sin impresión de recibo");
	atm.EnviarTransactionRequest(teclasConsultaDeBalance, numeroTarjeta, pin);

	EsperarTeclaEnter("Presione ENTER para finalizar");
}

static void EsperarTeclaEnter(string mensaje)
{
	Console.ForegroundColor = ConsoleColor.DarkGray;
	Console.WriteLine(mensaje);
	_ = Console.ReadKey();
	Console.ResetColor();
}
