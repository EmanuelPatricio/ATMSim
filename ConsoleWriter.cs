namespace ATMSim;

public interface IConsoleWriter
{
	public void Write(string text);
	public void Write(decimal text);
	public void WriteLine(string text);
	public void WriteLine(decimal text);
	public void ResetColor();

	public ConsoleColor ForegroundColor { get; set; }
	public ConsoleColor BackgroundColor { get; set; }
}

public class ConsoleWriter : IConsoleWriter
{

	public ConsoleColor ForegroundColor { get => Console.ForegroundColor; set => Console.ForegroundColor = value; }
	public ConsoleColor BackgroundColor { get => Console.BackgroundColor; set => Console.BackgroundColor = value; }

	public void ResetColor()
	{
		Console.ResetColor();
	}

	public void Write(string text)
	{
		Console.Write(text);
	}

	public void Write(decimal text)
	{
		Console.Write(text);
	}

	public void WriteLine(string text)
	{
		Console.WriteLine(text);
	}

	public void WriteLine(decimal text)
	{
		Console.WriteLine(text);
	}
}
