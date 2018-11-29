// MSDN says I should use XML over INI, but whatever. I don't need unicode support.
// Taken from https://stackoverflow.com/questions/217902/reading-writing-an-ini-file

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace PseudoPopParser {

	class IniFile { // revision 11

		string Path;
		string EXE = Assembly.GetExecutingAssembly().GetName().Name;

		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

		public IniFile(string IniPath = null) {
			Path = new FileInfo(IniPath ?? EXE + ".ini").FullName.ToString();
		}

		public string Read(string Key, string Section = null) {
			var RetVal = new StringBuilder(255);
			GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
			return RetVal.ToString();
		}

		public void Write(string Key, string Value, string Section = null) {
			WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
		}

		public void DeleteKey(string Key, string Section = null) {
			Write(Key, null, Section ?? EXE);
		}

		public void DeleteSection(string Section = null) {
			Write(null, null, Section ?? EXE);
		}

		public bool KeyExists(string Key, string Section = null) {
			return Read(Key, Section).Length > 0;
		}

		public string[] Keys() {
			var keys = new System.Collections.Generic.List<string>();
			string[] ini_lines = File.ReadAllLines(Path);
			foreach (string line in ini_lines) {
				string key = Regex.Match(line, @".*\s=").ToString();
				if (key.Length > 0) {
					key = key.Substring(0, key.Length - 2);
					keys.Add(key);
				}
			}
			return keys.ToArray();
		}

		public string[] Keys(string section) {
			var keys = new System.Collections.Generic.List<string>();
			bool read = false;
			string[] ini_lines = File.ReadAllLines(Path);
			foreach (string line in ini_lines) {
				string section_match = Regex.Match(line, @"\[.*\]").ToString();
				// Stop Reading
				if (section_match.Length > 0 && section_match != "[" + section + "]") {
					read = false;
				}
				// Start Reading
				else if (section_match == "[" + section + "]") {
					read = true;
				}
				else if (read) {
					string key = Regex.Match(line, @".*\s=").ToString();
					if (key.Length > 0) {
						key = key.Substring(0, key.Length - 2);
						keys.Add(key);
					}
				}
			}
			return keys.ToArray();
		}

		public string[] Sections() {
			var sections = new System.Collections.Generic.List<string>();
			string[] ini_lines = File.ReadAllLines(Path);
			foreach (string line in ini_lines) {
				string section = Regex.Match(line, @"\[.*\]").ToString();
				if (section.Length > 0) {
					section = section.Substring(1, section.Length - 2);
					sections.Add(section);
				}
			}
			return sections.ToArray();
		}
	}

}