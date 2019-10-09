using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PseudoPopParser {

	internal class Program {

		public static readonly IniFile Config = new IniFile(AppDomain.CurrentDomain.BaseDirectory + @"config.ini");
		public static string[] LaunchArguments;
		public static string FullPopFileDirectory;
		public static string FullPopFilePath;
		public static PopFile PopFile;
		public static PopulationAnalyzer PopAnalyzer;
		public static StreamWriter LogWriter;
		private static bool AutoClose;
		private static bool NoMenu;
		private static bool Secret;
		private static bool ShowStopWatch;
		private static int LineCount;

		[STAThread]
		internal static void Main(string[] args) {
			var StopWatch = System.Diagnostics.Stopwatch.StartNew();
			LaunchArguments = args;

			// Console Size
			try {
				int Width = Console.LargestWindowWidth >= 100 ? 100 : Console.LargestWindowWidth;
				int Height = Console.LargestWindowHeight >= 50 ? 50 : Console.LargestWindowHeight;
				Console.SetWindowSize(Width, Height);
			}
			catch { } // Catch possible SecurityException

			// Build Dialog
			OpenFileDialog dialog = new OpenFileDialog {
				InitialDirectory = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory),
				Filter = "Pop Files|*.pop"
			};
			dialog.FileName = @"C:\Users\Simple\Desktop\ANTLR Files\testrig.pop"; // TODO Remove this line //

			// Launch Flags
			for (int i = 0; i < args.Length; i++) {
				if (args[i] == "-pop") {
					dialog.FileName = args[i + 1];
				}
				if (args[i] == "-log") {
					PrintColor.InfoLine("=====Log: {f:Cyan}{$0}{r}=====", args[i + 1]);
					LogWriter = new StreamWriter(new FileStream(args[i + 1], FileMode.Append));
				}
				if (args[i] == "--no_menu") {
					NoMenu = true;
				}
				if (args[i] == "--auto_close") {
					AutoClose = true;
				}
				if (args[i] == "--AF") {
					Secret = true;
				}
				if (args[i] == "--time") {
					ShowStopWatch = true;
				}
			}

			// Show Dialog
			PrintColor.InfoLine("P3 v2 Alpha");
			while (dialog.FileName == "") {
				PrintColor.InfoLine("Select your Pop file");
				dialog.ShowDialog();
			}
			FullPopFileDirectory = Path.GetDirectoryName(dialog.FileName);
			FullPopFilePath = dialog.FileName;

			if (ShowStopWatch) {
				LineCount = File.ReadLines(FullPopFilePath).Count();
			}

			//string FileContents = File.ReadAllText(FullPopFilePath); // Legacy input method
			//AntlrInputStream inputstream = new AntlrInputStream(FileContents);
			FileStream FS = new FileStream(FullPopFilePath, FileMode.Open);
			AntlrInputStream inputstream = new AntlrInputStream(FS);

			PopulationLexer lexer = new PopulationLexer(inputstream);
			lexer.RemoveErrorListeners();
			lexer.AddErrorListener(new PopulationLexerErrorListener<int>());

			CommonTokenStream tokenstream = new CommonTokenStream(lexer);

			PopulationParser parser = new PopulationParser(tokenstream);
			parser.RemoveErrorListeners();
			parser.AddErrorListener(new PopulationErrorListener());
			parser.ErrorHandler = new PopulationErrorStrategy();

			ItemDatabase.Build();

			PrintColor.InfoLine("Pop File - {f:Cyan}{$0}{r}", FullPopFilePath);
			PopulationParser.PopfileContext context = parser.popfile();
			PopulationVisitor visitor = new PopulationVisitor();
			visitor.Visit(context, tokenstream); // Stage 1

			Program.PopFile = visitor.GetPopFile();
			PopAnalyzer = new PopulationAnalyzer(Program.PopFile);
			PopAnalyzer.Analyze();
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

			FS.Close();

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
	}
}
