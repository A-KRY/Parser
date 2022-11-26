//#undef DEBUG
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Analyser {

	// 符号枚举
	enum Symbol {

		// Nonterminal
		E,
		E1,
		T,
		T1,
		F,

		// Terminal
		i,      // operand
		PL,     // +
		MI,     // -
		MU,     // *
		DI,     // /
		LB,     // (
		RB,     // )
		END,	// #
		NULL,	// ε
	}

	public class Parser
	{
		// 唯一实例
		private static Parser? uniqueInstance = null;

		// 预测分析表
		private Dictionary<Symbol, Dictionary<Symbol, List<Symbol>>> PATable;

		// 文件输入输出流
		private StreamReader? streamReader = null;
		private StreamWriter? streamWriter = null;
		private bool bEndOfStream;

		// 符号缓冲队列
		private Queue<Symbol> synBuffer;
		private const int bufferSize = 10;

		// buffer 行数计数
		private int lineCnt;

		// 符号计数
		private int sgnCnt;
#if DEBUG
		private int stepCnt = 0;
#endif
		// 符号栈
		private Stack<Symbol> stkSymbol = new Stack<Symbol>();

		// 非终结符集合
		private HashSet<Symbol> nonterminalSet;

		// 终结符集合
		private HashSet<Symbol> terminalSet;

		// 构造函数
		private Parser()
		{
			synBuffer = new Queue<Symbol>();
			lineCnt = 0;
			sgnCnt = 0;
			bEndOfStream = false;

			// 构造预测分析表
			// 倒序装入
			PATable = new Dictionary<Symbol, Dictionary<Symbol, List<Symbol>>>();

			PATable[Symbol.E] = new Dictionary<Symbol, List<Symbol>>();
			PATable[Symbol.E][Symbol.i] = new List<Symbol> { Symbol.E1, Symbol.T};
			PATable[Symbol.E][Symbol.LB] = new List<Symbol> { Symbol.E1, Symbol.T };

			PATable[Symbol.E1] = new Dictionary<Symbol, List<Symbol>>();
			PATable[Symbol.E1][Symbol.PL] = new List<Symbol> { Symbol.E1, Symbol.T, Symbol.PL };
			PATable[Symbol.E1][Symbol.MI] = new List<Symbol> { Symbol.E1, Symbol.T, Symbol.MI };
			PATable[Symbol.E1][Symbol.RB] = new List<Symbol>();
			PATable[Symbol.E1][Symbol.END] = new List<Symbol>();

			PATable[Symbol.T] = new Dictionary<Symbol, List<Symbol>>();
			PATable[Symbol.T][Symbol.i] = new List<Symbol> { Symbol.T1, Symbol.F };
			PATable[Symbol.T][Symbol.LB] = new List<Symbol> { Symbol.T1, Symbol.F };

			PATable[Symbol.T1] = new Dictionary<Symbol, List<Symbol>>();
			PATable[Symbol.T1][Symbol.PL] = new List<Symbol>();
			PATable[Symbol.T1][Symbol.MI] = new List<Symbol>();
			PATable[Symbol.T1][Symbol.MU] = new List<Symbol> { Symbol.T1, Symbol.F, Symbol.MU };
			PATable[Symbol.T1][Symbol.DI] = new List<Symbol> { Symbol.T1, Symbol.F, Symbol.DI };
			PATable[Symbol.T1][Symbol.RB] = new List<Symbol>();
			PATable[Symbol.T1][Symbol.END] = new List<Symbol>();

			PATable[Symbol.F] = new Dictionary<Symbol, List<Symbol>>();
			PATable[Symbol.F][Symbol.i] = new List<Symbol> { Symbol.i };
			PATable[Symbol.F][Symbol.LB] = new List<Symbol> { Symbol.RB, Symbol.E, Symbol.LB };

			// 非终结符集合
			nonterminalSet = new HashSet<Symbol>();
			nonterminalSet.Add(Symbol.E);
			nonterminalSet.Add(Symbol.E1);
			nonterminalSet.Add(Symbol.T);
			nonterminalSet.Add(Symbol.T1);
			nonterminalSet.Add(Symbol.F);

			// 终结符集合
			terminalSet = new HashSet<Symbol>();
			terminalSet.Add(Symbol.i);
			terminalSet.Add(Symbol.PL);
			terminalSet.Add(Symbol.MI);
			terminalSet.Add(Symbol.MU);
			terminalSet.Add(Symbol.DI);
			terminalSet.Add(Symbol.LB);
			terminalSet.Add(Symbol.RB);
			terminalSet.Add(Symbol.END);
		}

		// 析构函数
		~Parser()
		{
			streamReader.Close();
			streamWriter.Close();
		}

		// 获取实例
		public static Parser Instance
		{
			get
			{
				if (uniqueInstance == null)
				{
					uniqueInstance = new Parser();
				}

				return uniqueInstance;
			}
		}


		// 初始化输入流
		public void InitStreamReader(String path)
		{
			if (streamReader != null)
			{
				streamReader.Close();
			}
			streamReader = new StreamReader(path);
		}


		// 初始化输出流
		public void InitStreamWriter(String path)
		{
			if (streamWriter != null)
			{
				streamWriter.Close();
			}
			streamWriter = new StreamWriter(path);
			streamWriter.AutoFlush = true;
		}

		// 读入符号缓冲队列
		private void ReadBuffer()
		{
			if (streamReader == null)
			{
				throw new IOException("StreamReader not Initialized.");
			}

			int cnt = 0;
			String inputStr = "", midStr = "";
			String[] strTuple;
			while (!streamReader.EndOfStream && cnt < bufferSize)
			{
				++cnt;
				++lineCnt;
				
				inputStr = streamReader.ReadLine();
				midStr = inputStr.Substring(1, inputStr.Length - 2);
				strTuple = midStr.Split(',');
				if (strTuple[0].Equals("ID") || strTuple[0].Equals("INT") || strTuple[0].Equals("REAL"))
				{
					synBuffer.Enqueue(Symbol.i);
				}
				else if (strTuple[0].Equals("PL"))
				{
					synBuffer.Enqueue(Symbol.PL);
				}
				else if (strTuple[0].Equals("MI"))
				{
					synBuffer.Enqueue(Symbol.MI);
				}
				else if (strTuple[0].Equals("MU"))
				{
					synBuffer.Enqueue(Symbol.MU);
				}
				else if (strTuple[0].Equals("DI"))
				{
					synBuffer.Enqueue(Symbol.DI);
				}
				else if (strTuple[0].Equals("LB"))
				{
					synBuffer.Enqueue(Symbol.LB);
				}
				else if (strTuple[0].Equals("RB"))
				{
					synBuffer.Enqueue(Symbol.RB);
				}
				else
				{
					throw new InvalidExpressionException("Invalid input at line "+lineCnt+": "+inputStr);
				}
			}

			if (synBuffer.Count == 0)
			{
				bEndOfStream = true;
			}
		}

		// 获取下一个符号
		private Symbol NextSGN()
		{
			if (synBuffer.Count == 0)
			{
				// 若输入流已空却还需读取，则匹配不成功
				if (bEndOfStream)
				{
					throw new InvalidExpressionException("Illegal input at line " + sgnCnt + " around word \"" + stkSymbol.Peek() + "\".");
				}
				
				ReadBuffer();

				// 若读完还为空，则遇到文件尾，压入END状态
				if (synBuffer.Count == 0)
				{
					return Symbol.END;
				}
			}

			++sgnCnt;
			return synBuffer.Dequeue();
		}

		private void PushExp(Symbol nonterminal, Symbol terminal)
		{
			if (!nonterminalSet.Contains(nonterminal) 
			    || !terminalSet.Contains(terminal)
			    || !PATable[nonterminal].ContainsKey(terminal))
			{
#if DEBUG
				Console.WriteLine();
				Console.WriteLine("Is nonterminal: "+ nonterminalSet.Contains(nonterminal));
				Console.WriteLine("Is terminal: "+ terminalSet.Contains(terminal));
				Console.WriteLine("Can move: " + PATable[nonterminal].ContainsKey(terminal));
				Console.WriteLine();
#endif
				throw new InvalidExpressionException("Illegal input at line "+sgnCnt+" around word \""+terminal+"\".");
			}
			else
			{
				foreach (var sgn in PATable[nonterminal][terminal])
				{
					stkSymbol.Push(sgn);
				}
			}
		}

		// 执行语法分析
		public void Run()
		{
			if (streamReader == null || streamWriter == null)
			{
				if (streamReader == null && streamWriter != null)
				{
					throw new IOException("StreamReader not initialized.");
				}
				else if (streamReader != null && streamWriter == null)
				{
					throw new IOException("StreamWriter not initialized.");
				}
				else
				{
					throw new IOException("StreamReader and StreamWriter not initialized.");
				}
			}

			Symbol currSymbol = NextSGN();
			stkSymbol.Push(Symbol.END);
			stkSymbol.Push(Symbol.E);

			while (stkSymbol.Count != 0)
			{
#if DEBUG
				Console.WriteLine();
				++stepCnt;
				Console.WriteLine("Step "+stepCnt);
				Console.WriteLine("CurrSGN: "+currSymbol);
				ShowStack();
				
#endif
				if (currSymbol == stkSymbol.Peek())
				{
					stkSymbol.Pop();

					// 符号栈非空则继续读取
					if (stkSymbol.Count != 0)
					{
						try {
						currSymbol = NextSGN();
						}
						catch (InvalidExpressionException ieException) {
							Console.WriteLine(ieException.Message);
							return;
						}
#if DEBUG
						Console.WriteLine("Read next symbol.");
#endif
					}
					// 符号栈空则匹配成功
					else
					{
						return;
					}
				}
				else
				{
					try
					{
						PushExp(stkSymbol.Pop(), currSymbol);
					}
					catch (InvalidExpressionException ieException)
					{
						Console.WriteLine(ieException.Message);
						return;
					}
				}
			}


			streamWriter.WriteLine("Successful.");
			Console.WriteLine("Successful.");
		}

#if DEBUG
		private void ShowStack()
		{
			foreach (var item in stkSymbol.Reverse())
			{
				Console.Write(item+" ");
			}
			Console.Write('\n');
		}
#endif

	}
}
