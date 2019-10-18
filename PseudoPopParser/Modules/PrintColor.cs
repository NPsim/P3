using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace PseudoPopParser {
	class PrintColor {
		private readonly static ConsoleColor DefaultForeground = ConsoleColor.White;
		private readonly static ConsoleColor DefaultBackground = ConsoleColor.Black;
		public static string LogOutput;
		public static bool SuppressOutput;

		private static void ResetColor() {
			Console.ForegroundColor = DefaultForeground;
			Console.BackgroundColor = DefaultBackground;
		}

		private static void ChangeColor(string color, bool is_background) {
			ConsoleColor Change;
			switch (color.ToUpper()) {
				case "BLACK":
					Change = ConsoleColor.Black;
					break;
				case "DARKBLUE":
					Change = ConsoleColor.DarkBlue;
					break;
				case "DARKGREEN":
					Change = ConsoleColor.DarkGreen;
					break;
				case "DARKCYAN":
					Change = ConsoleColor.DarkCyan;
					break;
				case "DARKRED":
					Change = ConsoleColor.DarkRed;
					break;
				case "DARKMAGENTA":
					Change = ConsoleColor.DarkMagenta;
					break;
				case "DARKYELLOW":
					Change = ConsoleColor.DarkYellow;
					break;
				case "GRAY":
					Change = ConsoleColor.Gray;
					break;
				case "DARKGRAY":
					Change = ConsoleColor.DarkGray;
					break;
				case "BLUE":
					Change = ConsoleColor.Blue;
					break;
				case "GREEN":
					Change = ConsoleColor.Green;
					break;
				case "CYAN":
					Change = ConsoleColor.Cyan;
					break;
				case "RED":
					Change = ConsoleColor.Red;
					break;
				case "MAGENTA":
					Change = ConsoleColor.Magenta;
					break;
				case "YELLOW":
					Change = ConsoleColor.Yellow;
					break;
				case "WHITE":
					Change = ConsoleColor.White;
					break;
				default:
					throw new Exception("ColorNotFound");
			}
			if (is_background) {
				Console.BackgroundColor = Change;
			}
			else {
				Console.ForegroundColor = Change;
			}
		}

		public static void Write(string format, params dynamic[] args) {
			if (SuppressOutput) return;

			string[] segments = Regex.Split(format, @"({f:[^}]*}|{b:[^}]*}|{\$[0-9]+}|{r})");
			for (int i = 0; i < segments.Count(); i++) {
				string token = segments[i];
				if (token.Length == 0) continue;
				else if (token[0] == '{' && token[1] == '$' && Int32.TryParse(token[2].ToString(), out int arg_index) && args.Count() > arg_index) {
					Console.Write(args[arg_index].ToString());
					if (Program.LogWriter != null) {
						Program.LogWriter.Write(args[arg_index].ToString());
					}
				}
				else if (token[0] == '{' && token[1] == 'f') {
					ChangeColor(token.Substring(3, token.Length - 4), false);
				}
				else if (token[0] == '{' && token[1] == 'b') {
					ChangeColor(token.Substring(3, token.Length - 4), true);
				}
				else if (token[0] == '{' && token[1] == 'r') {
					ResetColor();
				}
				else {
					Console.Write(token);
					if (Program.LogWriter != null) {
						Program.LogWriter.Write(token);
					}
				}
			}
		}
		public static void WriteLine(string format, params dynamic[] args) {
			Write(format + "\n", args);
		}

		public static void Info(string message, params string[] args) {
			Write("{f:Black}{b:DarkCyan}[Info]{r}\t" + message, args);
		}
		public static void InfoLine(string message, params string[] args) {
			Info(message + "\n", args);
		}

		/*public static void DebugInternalLine(string message, params string[] args) {
			WriteLine("{f:Black}{b:Magenta}[DEBUG]{r}\t" + message, args);
		}*/
	}
}
