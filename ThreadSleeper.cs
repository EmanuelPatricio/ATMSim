namespace ATMSim;

public interface IThreadSleeper
{
	public void Sleep(int miliseconds);
}

public class ThreadSleeper : IThreadSleeper
{
	public void Sleep(int miliseconds)
	{
		Thread.Sleep(miliseconds);
	}
}
