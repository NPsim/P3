using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

namespace PseudoPopParser {

	internal class PrintColor {

		private static ConsoleColor default_foreground = ConsoleColor.White;
		private static ConsoleColor default_background = ConsoleColor.Black;
		public static string log_path = "";

		// Used only for debug batch parsing multiple test files at once.
		private static void LogWrite(string message, string[] args = default(string[]), string line = "-1") {
			if (log_path.Length == 0) {
				return;
			}
			using (StreamWriter sw = new StreamWriter(log_path, true)) {

				string[] segments = Regex.Split(message, "[{}]");
				for (int i = 0; i < segments.Count(); i++) {
					string token = segments[i];
					if (token.Length == 0) {
						continue;
					}
					else if (Int32.TryParse(token[0].ToString(), out int arg_index) && args.Count() > arg_index) {
						sw.Write(args[arg_index]);
					}
					else if (token[0] == 'f') { }
					else if (token[0] == 'b') { }
					else if (token[0] == 'r') { }
					else {
						sw.Write(token);
					}
				}
				if (line != "-1") {
					sw.Write(" Line:" + line + "\n");
				}
				else {
					sw.Write("\n");
				}
				/*
				sw.Write(message);
				foreach (string s in args) {
					sw.Write("|" + s);
				}
				sw.Write("|\n");
				*/
			}
		}

		private static void ResetColor() {
			Console.ForegroundColor = default_foreground;
			Console.BackgroundColor = default_background;
		}

		private static void ChangeColor(string color, bool is_background) {
			ConsoleColor change_to; // I can't find a better way of doing it using C# reflection.
			switch (color.ToUpper()) {
				case "BLACK":
					change_to = ConsoleColor.Black;
					break;
				case "DARKBLUE":
					change_to = ConsoleColor.DarkBlue;
					break;
				case "DARKGREEN":
					change_to = ConsoleColor.DarkGreen;
					break;
				case "DARKCYAN":
					change_to = ConsoleColor.DarkCyan;
					break;
				case "DARKRED":
					change_to = ConsoleColor.DarkRed;
					break;
				case "DARKMAGENTA":
					change_to = ConsoleColor.DarkMagenta;
					break;
				case "DARKYELLOW":
					change_to = ConsoleColor.DarkYellow;
					break;
				case "GRAY":
					change_to = ConsoleColor.Gray;
					break;
				case "DARKGRAY":
					change_to = ConsoleColor.DarkGray;
					break;
				case "BLUE":
					change_to = ConsoleColor.Blue;
					break;
				case "GREEN":
					change_to = ConsoleColor.Green;
					break;
				case "CYAN":
					change_to = ConsoleColor.Cyan;
					break;
				case "RED":
					change_to = ConsoleColor.Red;
					break;
				case "MAGENTA":
					change_to = ConsoleColor.Magenta;
					break;
				case "YELLOW":
					change_to = ConsoleColor.Yellow;
					break;
				case "WHITE":
					change_to = ConsoleColor.White;
					break;
				default:
					throw new Exception("ColorNotFound");
			}
			if (is_background) {
				Console.BackgroundColor = change_to;
			}
			else {
				Console.ForegroundColor = change_to;
			}
		}

		public static void Colorf(string message, params string[] args) {
			PopParser p = new PopParser();
			if (p.SuppressPrint) {
				return;
			}
			string[] segments = Regex.Split(message, "[{}]");
			for (int i = 0; i < segments.Count(); i++) {
				string token = segments[i];
				if (token.Length == 0) {
					continue;
				}
				else if (Int32.TryParse(token[0].ToString(), out int arg_index) && args.Count() > arg_index) {
					Console.Write(args[arg_index]);
				}
				else if (token[0] == 'f') {
					string color_str = token.Substring(2);
					ChangeColor(color_str, false);
				}
				else if (token[0] == 'b') {
					string color_str = token.Substring(2);
					ChangeColor(color_str, true);
				}
				else if (token[0] == 'r') {
					ResetColor();
				}
				else {
					Console.Write(token);
				}
			}
		}

		public static void ColorLinef(string message, params string[] args) {
			Colorf(message + "\n", args);
		}

		public static void Warn(string message, string line, uint code, params string[] args) {
			PopParser p = new PopParser();
			if (p.SuppressPrint) {
				return;
			}
			
			LogWrite("\tWarn: " + message, args, line); // Debug batch testing

			// Build warning code
			string str_code = code.ToString("0000");

			if (line != "-1") {
				ColorLinef("{f:Black}{b:Yellow}[Warning-W" + str_code + "]{r}:" + line + "\t" + message, args);
			}
			else {
				ColorLinef("{f:Black}{b:Yellow}[Warning-W" + str_code + "]{r}    \t" + message, args);
			}
			Warning.IncrementWarnings(); // todo localize
		}
		public static void Warn(string message, int line = -1, uint code = 0, params string[] args) {
			Warn(message, line.ToString(), code, args);
		}

		public static void Error(string message, string line, uint code = 0999, params string[] args) {
			string str_code = code.ToString("0000");
			LogWrite("\tErrr: " + message, args, line); // Debug batch testing

			if (line != "-1") {
				ColorLinef("{f:Black}{b:Red}[Error-E" + str_code + "]{r}:" + line + "\t" + message, args);
			}
			else {
				ColorLinef("{f:Black}{b:Red}[Error-E" + str_code + "]{r}\t" + message, args);
			}
			PseudoPopParser.Error.SetError();
		}
		public static void Error(string message, int line = -1, uint code = 0999, params string[] args) {
			Error(message, line.ToString(), code, args);
		}

		public static void ErrorNoTrigger(string message, string line, uint code = 0998, params string[] args) {
			string str_code = code.ToString("0000");
			LogWrite("\tErNT: " + message, args, line); // Debug batch testing

			if (line != "-1") {
				ColorLinef("{f:Black}{b:Red}[Error-E" + str_code + "]{r}:" + line + "\t" + message, args);
			}
			else {
				ColorLinef("{f:Black}{b:Red}[Error-E" + str_code + "]{r}\t" + message, args);
			}
		}
		public static void ErrorNoTrigger(string message, int line = -1, uint code = 0998, params string[] args) {
			ErrorNoTrigger(message, line.ToString(), code, args);
		}

		public static void Info(string message) {
			if (Regex.IsMatch(message, "^Pop File")) {
				LogWrite("Info: " + message); // Debug batch testing
			}
			Colorf("{f:Black}{b:DarkCyan}[Info]{r}\t" + message);
		}

		public static void InfoLine(string message, params string[] args) {
			if (Regex.IsMatch(message, "^Pop File")) {
				LogWrite("Info: " + message, args); // Debug batch testing
			}
			ColorLinef("{f:Black}{b:DarkCyan}[Info]{r}\t" + message, args);
		}

		public static void PotentialFix(string message, params string[] args) {
			ColorLinef("\t{f:Black}{b:Gray}[PotentialFix]{r}\t" + message, args);
		}

		public static void DebugInternalLine(string message, params string[] args) {
			ColorLinef("{f:Black}{b:Magenta}[DEBUG]{r}\t" + message, args);
		}
		public static void DebugLine(string message, params string[] args) {
			DebugInternalLine(message, args);
		}

		public static void WriteColor(string message, ConsoleColor background = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.White) {
			Console.BackgroundColor = background;
			Console.ForegroundColor = foreground;
			Console.Write(message);
			Console.ResetColor();
		}
		public static void WriteLineColor(string message, ConsoleColor background = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.White) {
			WriteColor(message, background, foreground);
			Console.Write("\n");
		}
	}
}
