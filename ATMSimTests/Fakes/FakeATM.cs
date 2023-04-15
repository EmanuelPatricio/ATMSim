using ATMSim;

namespace ATMSimTests.Fakes;

internal class FakeATM : IATM
{
	public byte[] Llave { get; set; }

	public IATMSwitch? Switch { get; set; }
	public string Nombre { get; set; }

	public bool Configurado => throw new NotImplementedException();

	public FakeATM(string nombre) => Nombre = nombre;


	public void EnviarTransactionRequest(string opKeyBuffer, string numeroTarjeta, string pin, int monto = 0)
	{
		throw new NotImplementedException();
	}

	public void InstalarLlave(byte[] llave)
	{
		Llave = llave;
	}

	public void Reestablecer()
	{
		throw new NotImplementedException();
	}
}
