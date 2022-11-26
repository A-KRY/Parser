namespace Analyser
{
	public class Program
	{
		public static void Main()
		{
			Parser parser = Parser.Instance;
			parser.InitStreamReader("input.txt");
			parser.InitStreamWriter("output.txt");
			parser.Run();
		}
	}
}

