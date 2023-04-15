using ATMSim;


namespace ATMSimTests.Fakes;

internal class FakeThreadSleeper : IThreadSleeper
{
	public int sleepedMiliseconds = 0;
	public int numberOfCalls = 0;
	public void Sleep(int miliseconds)
	{
		numberOfCalls++;
		sleepedMiliseconds += miliseconds;
	}
}
