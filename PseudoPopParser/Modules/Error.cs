using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {

	internal class Error {

		private static string ReadString(string s) { return Program._INI.Read(s); }
		private static bool ReadBool(string s) { return Program._INI.ReadBool(s); }
		private static int ReadInt(string s) { return Program._INI.ReadInt(s); }
		private static double ReadDouble(string s) { return Program._INI.ReadDouble(s); }
		private static bool ErrorStatus = false;

		public static bool ErrorOccurred { get { return ErrorStatus; } }
		public static void SetError() { ErrorStatus = true; }

		public class NoTrigger {

			public static void FailedParseTemplate(string template_name, string base_name) {
				uint code = 0801;
				PrintColor.ErrorNoTrigger("Could not parse Template file '{f:Red}{0}{r}' at location: '{f:Red}{1}{r}'", -1, code, template_name, base_name);
			}

			public static void MissingDatabase() {
				uint code = 0997;
				PrintColor.ErrorNoTrigger("Could not find local database.", -1, code);
			}

			public static void FailedDialog() {
				uint code = 0997;
				PrintColor.ErrorNoTrigger("Failed to get file by dialog.", -1, code);
			}

			public static void MissingExtension() {
				uint code = 0997;
				PrintColor.ErrorNoTrigger("Pop file must have *.pop file extension.", -1, code);
			}

			public static void MapInvalidVBSP() {
				uint code = 0996;
				PrintColor.ErrorNoTrigger("Invalid VBSP file.", -1, code);
			}

			public static void MapFailedDecompile(string message) {
				uint code = 0995;
				PrintColor.Error("Failed to decompile VBSP: '{0}'", -1, code, message);
			}

			public static void Unknown(string message) {
				uint code = 0998;
				PrintColor.Error("{f:Cyan}Unknown{r} exception '{0}'", -1, code, message);
				PrintColor.WriteLineColor("Please contact the developer regarding this error", ConsoleColor.Blue);
				PrintColor.WriteLineColor("Contact info can be found in the README", ConsoleColor.Blue);
			}

		}

		public static void InvalidSymbol(int line, string token) {
			uint code = 0501;
			PrintColor.Error("{f:Red}Invalid symbol{r} found near '{f:Red}{0}{r}'", line, code, token);
		}

		public static void BadComment(int line, string token) {
			uint code = 0502;
			PrintColor.Error("Bad comment found near '{f:Red}{0}{r}'", line, code, token);

			if (ReadBool("bool_show_potential_fix")) {
				PrintColor.PotentialFix("Insert a space between \"" + System.Text.RegularExpressions.Regex.Replace(token, @"\/\/.*[\s]*", "") + "\" and \"//\"");
			}
		}

		// Internal Exception
		public static void InvalidName(int line, string token) {
			uint code = 0999; // 0901
			PrintColor.Error("{f:Red}Invalid name{r} found near '{f:Red}{0}{r}'", line, code, token);
		}

		// Internal Exception
		public static void InvalidValueType(int line, string token) {
			uint code = 0999; // 0902
			PrintColor.Error("{f:Red}Invalid value{r} found near '{f:Red}{0}{r}'", line, code, token);
		}

		// Internal Exception
		public static void InvalidCloseCurly(int line, string token) {
			uint code = 0999; // 0903
			PrintColor.Error("{f:Red}Invalid Closing Curly Bracket{r} found: '{f:Red}{0}{r}'", line, code, token);

			if (ReadBool("bool_show_potential_fix")) {
				PrintColor.PotentialFix("Recount and remove excess Close Curly Brackets");
			}
		}

		// Internal Exception
		public static void InvalidKey(int line, string token) {
			uint code = 0999; // 0904
			PrintColor.Error("{f:Red}Invalid key{r} found near '{f:Red}{0}{r}'", line, code, token);
		}

		public static void NestedCollection(int line, string token) {
			uint code = 0503;
			PrintColor.Error("Cannot have nested complex spawners: '{f:Red}{0}{r}'", line, code, token);

			if (ReadBool("bool_show_potential_fix")) {
				PrintColor.PotentialFix("Cannot have Squad, Mob, or RandomChoice within another Squad, Mob, or RandomChoice.");
				PrintColor.PotentialFix("Valve has not implemented a recursive spawner.");
			}
		}

		public static void Unknown(int line, string token, string message) {
			uint code = 0999; // 0999
			PrintColor.Error("{f:Cyan}Unknown{r} exception '{f:Red}{1}{r}' near '{f:Red}{0}{r}'", line, code, token, message);
			PrintColor.WriteLineColor("Please contact the developer regarding this error", ConsoleColor.Blue);
			PrintColor.WriteLineColor("Contact info can be found in the README", ConsoleColor.Blue);
		}
	}
}
