using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyser {

	// 关键字
	enum KEYWORD
	{
		INVALID = 0,
		IF = 1,
		ELSE = 2,
		FOR = 3,
		DO = 4,
		BEGIN = 5,
		END = 6,
	}

	// 助记符
	enum MNEMONIC {
		INVALID = KEYWORD.INVALID,
		IF = KEYWORD.IF,
		ELSE = KEYWORD.ELSE,
		FOR = KEYWORD.FOR,
		DO = KEYWORD.DO,
		BEGIN = KEYWORD.BEGIN,
		END = KEYWORD.END,
		ID = 7,  // Identifier
		INT = 8,  // Integer
		REAL = 9,  // Real number
		LT = 10, // Less than    <
		LE = 11, // Less equal   <=
		EQ = 12, // Equal        =
		NE = 13, // Not equal    <>
		GT = 14, // Greater than >
		GE = 15, // Greater equal>=
		IS = 16, // Is           :=
		PL = 17, // Plus         +
		MI = 18, // Minus        -
		MU = 19, // Multiply     *
		DI = 20, // Divide       /
	};

	// 词法分析自动机状态
	enum LA_STATE {
		BGN = 0,    // 初态
		IAD = 1,    // Input alpha and digit of ID
		OID = 2,    // Output ID
		IN = 3,    // Input numeric
		ON = 4,    // Output numeric
		ILT = 5,    // Input less than       <
		OLE = 6,    // Output less equal    <=
		ONE = 7,    // Output not equal     <>
		OLT = 8,    // Output less than     <
		OEQ = 9,    // Output equal         =
		IGT = 10,   // Input greater than   >
		OGE = 11,   // Output greater equal >=
		OGT = 12,   // Output greater than  >
		IIS = 13,   // Input is             :=
		OIS = 14,   // Output is            :=
		OPL = 15,   // Output plus          +
		OMI = 16,   // Output minus         -
		OMU = 17,   // Output multiply      *
		ODI = 18,   // Output divide        /
		END,        // 末态
	};

	// 词法字符类别
	enum LA_TYPE {
		INVALID = 0,  // Invalid
		DIGIT = 1,  // Digit
		ALPHA = 2,  // Alpha
		LT = 3,  // Less than    <
		GT = 4,  // Greater than >
		EQ = 5,  // Equal        =
		CL = 6,  // Colon        :
		PL = 7,  // Plus         +
		MI = 8,  // Minus        -
		MU = 9,  // Multiply     *
		DI = 10, // Divide       /
		BLANK,
		NULL
	};

	internal class LexicalAnalyser
	{
		// 唯一实例
		private static LexicalAnalyser uniqueInstance = null;

		// 当前读入的部分字符
		private String buffer;
		// buffer 字符个数
		//private Int32 bufferSize;
		// 当前指向字符的角标
		private Int32 currIndex;
		// 当前行数
		private Int32 bufferCnt;

		// 当前单词的各个字符
		private String token;

		// 输入输出文件流
		private StreamReader InStream;
		private StreamWriter OutStream;

		private bool InStreamOpened, OutStreamOpened;

		// 关键字表
		private Dictionary<String, KEYWORD> KeywordTable;

		// 数值识别器
		private NumericAnalyser numericAnalyser;


		// 数值及类型
		private String value;
		private MNEMONIC numType;

		private LexicalAnalyser()
		{
			buffer = String.Empty;
			//bufferSize = 0;
			currIndex = 0;
			bufferCnt = 0;
			token = String.Empty;
			InStream = StreamReader.Null;
			OutStream = StreamWriter.Null;
			InStreamOpened = false;
			OutStreamOpened = false;
			InitKeywordTable();
			numericAnalyser = NumericAnalyser.GetInstance();
		}

		~LexicalAnalyser()
		{
			/*
			if (InStreamOpened)
			{
				InStream.Close();
			}

			if (OutStreamOpened)
			{
				OutStream.Close();
			}
			*/
		}

		// 获取单例
		public static LexicalAnalyser GetInstance()
		{
			if (uniqueInstance == null)
			{
				uniqueInstance = new LexicalAnalyser();
			}

			return uniqueInstance;
		}

		// 初始化 KeywordTable
		private void InitKeywordTable()
		{
			KeywordTable = new Dictionary<String, KEYWORD>();
			KeywordTable["if"] = KEYWORD.IF;
			KeywordTable["else"] = KEYWORD.ELSE;
			KeywordTable["for"] = KEYWORD.FOR;
			KeywordTable["do"] = KEYWORD.DO;
			KeywordTable["begin"] = KEYWORD.BEGIN;
			KeywordTable["end"] = KEYWORD.END;
		}

		// 读入 buffer
		private void ReadBuffer()
		{
			buffer = InStream.ReadLine();
			if (buffer != null)
			{
				//bufferSize = buffer.Length;
				++bufferCnt;
			}
		}

		// 将当前字符送入 currCh 并更新 currIndex
		private Char GetChar()
		{
			if (currIndex == buffer.Length || buffer == String.Empty) {
				ReadBuffer();
				if (buffer != null || !InStream.EndOfStream)
				{
					currIndex = 0;
					buffer += ' ';
					//++bufferSize;
				}
			}

			if (buffer == null || buffer == String.Empty)
			{
				return '\0';
			}

			buffer.Append(' ');
			
			Char currCh = buffer[currIndex];
			++currIndex;

			return currCh;
		}

		// 将 currCh 拼入 token
		void CAT(char currCh)
		{
			token += currCh;
			Console.WriteLine("Cat");
		}

		// 判断 token 的关键字类型
		KEYWORD LookUp()
		{
			if (KeywordTable.ContainsKey(token))
			{
				Console.WriteLine("LookUp");
				return KeywordTable[token];
			}
			else
			{
				return KEYWORD.INVALID;
			}
		}

		// 回退一个字符
		private void Retract()
		{
			--currIndex;
			Console.WriteLine("Before:"+token+";");
			token = token.Remove(token.Length - 1);
			Console.WriteLine("After:"+token+";");
		}

		// 输出单词的二元表达式
		private void Out(MNEMONIC type, String str = "")
		{
			OutStream.Write('(');
			switch (type)
			{
				case MNEMONIC.IF:
					OutStream.Write("IF");
					break;
				case MNEMONIC.ELSE:
					OutStream.Write("ELSE");
					break;
				case MNEMONIC.FOR:
					OutStream.Write("FOR");
					break;
				case MNEMONIC.DO:
					OutStream.Write("DO");
					break;
				case MNEMONIC.BEGIN:
					OutStream.Write("BEGIN");
					break;
				case MNEMONIC.END:
					OutStream.Write("END");
					break;
				case MNEMONIC.ID:
					OutStream.Write("ID");
					break;
				case MNEMONIC.INT:
					OutStream.Write("INT");
					break;
				case MNEMONIC.REAL:
					OutStream.Write("REAL");
					break;
				case MNEMONIC.LT:
					OutStream.Write("LT");
					break;
				case MNEMONIC.LE:
					OutStream.Write("LE");
					break;
				case MNEMONIC.EQ:
					OutStream.Write("EQ");
					break;
				case MNEMONIC.NE:
					OutStream.Write("NE");
					break;
				case MNEMONIC.GT:
					OutStream.Write("GT");
					break;
				case MNEMONIC.GE:
					OutStream.Write("GE");
					break;
				case MNEMONIC.IS:
					OutStream.Write("IS");
					break;
				case MNEMONIC.PL:
					OutStream.Write("PL");
					break;
				case MNEMONIC.MI:
					OutStream.Write("MI");
					break;
				case MNEMONIC.MU:
					OutStream.Write("MU");
					break;
				case MNEMONIC.DI:
					OutStream.Write("DI");
					break;
				case MNEMONIC.INVALID:
				default:
					OutStream.Write("INVALID");
					break;
			}
			OutStream.WriteLine(','+str+')');
		}

		public void InitInStream(String path)
		{
			InStream = new StreamReader(path);
			InStreamOpened = true;
		}

		public void InitOutStream(String path)
		{
			OutStream = new StreamWriter(path);
			OutStreamOpened = true;
		}

		// 识别单词字符类型
		private LA_TYPE LA_GetChType()
		{
			
			char ch = GetChar();
			CAT(ch);
			Console.WriteLine("ch="+ch+"; "+Convert.ToInt32(ch));
			if (Char.IsDigit(ch)) {
				return LA_TYPE.DIGIT;
			}
			else if (Char.IsLetter(ch)) {
				return LA_TYPE.ALPHA;
			}
			else if ('<' == ch) {
				return LA_TYPE.LT;
			}
			else if ('>' == ch) {
				return LA_TYPE.GT;
			}
			else if ('=' == ch) {
				return LA_TYPE.EQ;
			}
			else if (':' == ch) {
				return LA_TYPE.CL;
			}
			else if ('+' == ch) {
				return LA_TYPE.PL;
			}
			else if ('-' == ch) {
				return LA_TYPE.MI;
			}
			else if ('*' == ch) {
				return LA_TYPE.MU;
			}
			else if ('/' == ch) {
				return LA_TYPE.DI;
			}
			else if (' ' == ch) {
				return LA_TYPE.BLANK;
			}
			else if ('\0' == ch)
			{
				return LA_TYPE.NULL;
			}
			else {
				return LA_TYPE.INVALID;
			}
		}

		// 执行单词识别
		private void LA_Execute(ref LA_STATE currState, LA_TYPE chType)
		{
			LOG("currState=", currState.ToString(), " Line "+(bufferCnt+1));
			switch (currState) {
				case LA_STATE.BGN:
					switch (chType) {
						case LA_TYPE.ALPHA:
							currState = LA_STATE.IAD;
							//CAT(buffer[currIndex]);
							break;
						case LA_TYPE.DIGIT:
							currState = LA_STATE.IN;
							Retract();
							//CAT(buffer[currIndex]);
							break;
						case LA_TYPE.LT:
							currState = LA_STATE.ILT;
							break;
						case LA_TYPE.EQ:
							currState = LA_STATE.OEQ;
							break;
						case LA_TYPE.GT:
							currState = LA_STATE.IGT;
							break;
						case LA_TYPE.CL:
							currState = LA_STATE.IIS;
							break;
						case LA_TYPE.PL:
							currState = LA_STATE.OPL;
							break;
						case LA_TYPE.MI:
							currState = LA_STATE.OMI;
							break;
						case LA_TYPE.MU:
							currState = LA_STATE.OMU;
							break;
						case LA_TYPE.DI:
							currState = LA_STATE.ODI;
							break;
						case LA_TYPE.BLANK:
							currState = LA_STATE.END;
							//OutStream.WriteLine("here");
							token = String.Empty;
							break;
						case LA_TYPE.NULL:
							currState = LA_STATE.END;
							break;
						default:
							currState = LA_STATE.END;
							LA_Fail();
							break;
					}
					break;
				case LA_STATE.IAD:
					switch (chType) {
						case LA_TYPE.ALPHA:
							currState = LA_STATE.IAD;
							//CAT(buffer[currIndex]);
							break;
						case LA_TYPE.DIGIT:
							currState = LA_STATE.IAD;
							//CAT(buffer[currIndex]);
							break;
						default:
							currState = LA_STATE.OID;
							Retract();
							break;
					}
					break;
				case LA_STATE.OID: {
					currState = LA_STATE.END;
					Retract();
					KEYWORD TYPE = LookUp();
					if (KEYWORD.INVALID == TYPE) {
						Out(MNEMONIC.ID, token);
					}
					else {
						Console.WriteLine("Out!");
						Out((MNEMONIC)TYPE);
					}
					token = String.Empty;
				}
				break;
				case LA_STATE.IN:
					currState = LA_STATE.ON;
					numType = MNEMONIC.INVALID;
					numericAnalyser.Run(Convert.ToInt32(token), out value, out numType);
					break;
				case LA_STATE.ON: {
					currState = LA_STATE.END;
					Retract();
					if (MNEMONIC.INT == numType) {
						Out(MNEMONIC.INT, value);
					}
					else if (MNEMONIC.REAL == numType) {
						Out(MNEMONIC.REAL, value);
					}
					else {
						LA_Fail();
					}
					token = String.Empty;
				}
				break;
				case LA_STATE.ILT:
					switch (chType) {
						case LA_TYPE.EQ:
							currState = LA_STATE.OLE;
							break;
						case LA_TYPE.GT:
							currState = LA_STATE.ONE;
							break;
						default:
							currState = LA_STATE.OLT;
							Retract();
							break;
					}
					break;
				case LA_STATE.OLE:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.LE);
					token = String.Empty;
					break;
				case LA_STATE.ONE:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.NE);
					token = String.Empty;
					break;
				case LA_STATE.OLT:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.LT);
					token = String.Empty;
					break;
				case LA_STATE.OEQ:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.EQ);
					token = String.Empty;
					break;
				case LA_STATE.IGT:
					switch (chType) {
						case LA_TYPE.EQ:
							currState = LA_STATE.OGE;
							break;
						default:
							currState = LA_STATE.OGT;
							break;
					}
					break;
				case LA_STATE.OGE:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.GE);
					token = String.Empty;
					break;
				case LA_STATE.OGT:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.GT);
					token = String.Empty;
					break;
				case LA_STATE.IIS:
					switch (chType) {
						case LA_TYPE.EQ:
							currState = LA_STATE.OIS;
							break;
						default:
							currState = LA_STATE.END;
							LA_Fail();
							break;
					}
					break;
				case LA_STATE.OIS:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.IS);
					token = String.Empty;
					break;
				case LA_STATE.OPL:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.PL);
					token = String.Empty;
					break;
				case LA_STATE.OMI:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.MI);
					token = String.Empty;
					break;
				case LA_STATE.OMU:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.MU);
					token = String.Empty;
					break;
				case LA_STATE.ODI:
					currState = LA_STATE.END;
					Retract();
					Out(MNEMONIC.DI);
					token = String.Empty;
					break;
				case LA_STATE.END:
					break;
			}
		}

		// 识别失败
		private void LA_Fail()
		{
			OutStream.WriteLine("ERROR at Line " + bufferCnt + 
			                    " Column " + (currIndex+1)+" : 单词 \"" + 
			                    token + "\" 非法");
		}

		public void Run()
		{
			if (!InStreamOpened)
			{
				throw new IOException("Input file not opened.");
			}
			if (!OutStreamOpened)
			{
				throw new IOException("Output file not opened.");
			}

			NumericAnalyser.SetDelegate(GetChar, CAT, Retract, LOG);

			while (buffer != null) {
				LA_STATE currState = LA_STATE.BGN;
				while (currState != LA_STATE.END) {
					LA_Execute(ref currState, LA_GetChType());
					//System.Console.WriteLine(currState.ToString() + "  " + buffer[currIndex]);
					//System.Console.Read();
				}
			}

			InStream.Close();
			OutStream.Close();
		}

		private void LOG<T>(params T[] vars)
		{
			Console.Write("currIndex="+currIndex+" token="+token+";");
			foreach (T var in vars)
			{
				Console.Write(var.ToString());
			}
			Console.Write('\n');
		}

	}
}
