using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PseudoPopParser {

	internal class Program {

		public static readonly IniFile Config = new IniFile(AppDomain.CurrentDomain.BaseDirectory + @"config.ini");
		public static Dictionary<string, string> LaunchArguments = new Dictionary<string, string>();
		public static string FullPopFileDirectory;
		public static string FullPopFilePath;
		public static PopFile PopFile;
		public static PopulationAnalyzer PopAnalyzer;
		public static StreamWriter LogWriter;
		public static int LineCount = 0;
		public enum ParserSafetyLevel {
			UNSAFE,
			SAFE
		}
		private static bool AutoClose, NoMenu, Secret, ShowStopWatch;
		private static ParserSafetyLevel SafetyLevel;

		[STAThread]
		internal static void Main(string[] args) {
			System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

			// Console Size
			try {
				int Width = Console.LargestWindowWidth >= 100 ? 100 : Console.LargestWindowWidth;
				int Height = Console.LargestWindowHeight >= 50 ? 50 : Console.LargestWindowHeight;
				Console.SetWindowSize(Width, Height);
			}
			catch { } // Catch possible SecurityException

			// Build Dialog
			OpenFileDialog Dialog = new OpenFileDialog {
				Filter = "Pop Files|*.pop"
			};

#if DEBUG
			Dialog.InitialDirectory = @"";
#endif

			// Get Execution Safety
			SafetyLevel = Program.Config.ReadBool("bool_unsafe") ? ParserSafetyLevel.UNSAFE : ParserSafetyLevel.SAFE;

			// Launch Flags
			for (int i = 0; i < args.Length; i++) {
				if (args[i] == "-pop") {
					Dialog.FileName = args[i + 1];
                    LaunchArguments["-pop"] = args[i + 1];
                }
				if (args[i] == "-log") {
					PrintColor.InfoLine("=====Log: {f:Cyan}{$0}{r}=====", args[i + 1]);
					LogWriter = new StreamWriter(new FileStream(args[i + 1], FileMode.Append));
                    LaunchArguments["-log"] = args[i + 1];
                }
				if (args[i] == "--no_menu") {
					NoMenu = true;
                    LaunchArguments["--no_menu"] = "1";
                }
				if (args[i] == "--auto_close") {
					AutoClose = true;
                    LaunchArguments["--auto_close"] = "1";
                }
				if (args[i] == "--AF") {
					Secret = true;
                    LaunchArguments["--AF"] = "1";
                }
				if (args[i] == "--time") {
					ShowStopWatch = true;
                    LaunchArguments["--time"] = "1";
                }
				if (args[i] == "--unsafe") {
					SafetyLevel = ParserSafetyLevel.UNSAFE;
					LaunchArguments["--unsafe"] = "1";
				}
				if (args[i] == "--safe") {
					SafetyLevel = ParserSafetyLevel.SAFE;
					LaunchArguments["--safe"] = "1";
				}
			}

			// Show Dialog
			if (SafetyLevel == ParserSafetyLevel.UNSAFE) {
				PrintColor.InfoLine("P3 v2.1.0 {b:White}{f:Black} UNSAFE MODE {r}");
			}
			else {
				PrintColor.InfoLine("P3 v2.1.0");
			}


			while (Dialog.FileName == "") {
				PrintColor.InfoLine("Select your Pop file");
				Dialog.ShowDialog();
			}
			FullPopFileDirectory = Path.GetDirectoryName(Dialog.FileName);
			FullPopFilePath = Dialog.FileName;
            LaunchArguments["-pop"] = Dialog.FileName;
			Console.Title = "P3 - " + Path.GetFileName(FullPopFilePath);


			var StopWatch = System.Diagnostics.Stopwatch.StartNew();
			LineCount += File.ReadLines(FullPopFilePath).Count();

			//string FileContents = File.ReadAllText(FullPopFilePath); // Legacy input method
			//AntlrInputStream inputstream = new AntlrInputStream(FileContents);
			FileStream FS = new FileStream(FullPopFilePath, FileMode.Open);
			AntlrInputStream inputstream = new AntlrInputStream(FS);
			FS.Close();

			PopulationLexer lexer = new PopulationLexer(inputstream);
			lexer.RemoveErrorListeners();
			lexer.AddErrorListener(new PopulationLexerErrorListener<int>());

			CommonTokenStream tokenstream = new CommonTokenStream(lexer);

			PopulationParser parser = new PopulationParser(tokenstream);
			parser.RemoveErrorListeners();
			parser.AddErrorListener(new PopulationErrorListener());
			parser.ErrorHandler = new PopulationErrorStrategy();

			ItemDatabase.Build();
			AttributeDatabase.Build();

			PrintColor.InfoLine("Pop File - {f:Cyan}{$0}{r}", FullPopFilePath);
			PopulationParser.PopfileContext context = parser.popfile();
			PopulationVisitor visitor = new PopulationVisitor();
			visitor.Visit(context, tokenstream);

			Program.PopFile = visitor.GetPopFile();
			PopAnalyzer = new PopulationAnalyzer(Program.PopFile);
			PrintColor.InfoLine("\tDone Parsing Pop File - {f:Cyan}{$0}{r}", Path.GetFileName(FullPopFilePath));

			StopWatch.Stop();

			// Ending Statement
			Console.Write("\n");
			if (Error.Errors > 0)
				PrintColor.WriteLine("{f:Black}{b:Red}Finished with {$0} errors and {$1} warnings.{r}", Error.Errors.ToString(), Warning.Warnings.ToString());
			else if (Warning.Warnings > 0)
				PrintColor.WriteLine("{f:Black}{b:Yellow}Finished with {$0} warnings.{r}", Warning.Warnings.ToString());
			else
				PrintColor.WriteLine("{f:Black}{b:Green}Finished cleanly.{r}");

			// Execution Time
			if (ShowStopWatch)
				PrintColor.InfoLine("Execution time: {f:Cyan}{$1} lines{r} in {f:Cyan}{$0}ms{r}", StopWatch.ElapsedMilliseconds.ToString(), LineCount.ToString());

			if (Secret) {
				List<string> tokens = new List<string>();
				foreach (IToken t in tokenstream.GetTokens()) {
					tokens.Add(t.Text);
				}

				try {
					AprilFools.DoTheThing(tokens);
				}
				catch {
					PrintColor.InfoLine("Better luck next time! (an error occurred)");
				}
			}

			if (AutoClose) {
				// Avoid everything
			}
			else if (!NoMenu) {
				Menu.Capture();
			}
			else {
				PrintColor.InfoLine("Press any key to continue.");
				Console.ReadKey();
			}

			if (LogWriter != null) {
				LogWriter.Write("\n=========================\n\n");
				LogWriter.Close();
			}
		}

		public static ParserSafetyLevel GetSafetyLevel() {
			return SafetyLevel;
		}
	}
}
