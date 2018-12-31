using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {
	class AttributesScraper : IDisposable {

		private string db_path = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\item_attributes.uwu";

		public AttributesScraper() { }

		public AttributesScraper(string att_db) {
			db_path = att_db;
		}

		public void Dispose() { }

		public bool WriteLineIfExists(StreamWriter sw, string value, string message) {
			if (value.Length > 0) {
				sw.WriteLine(message);
				return true;
			}
			return false;
		}

		public string Version {
			// Version is the MD5 sum of the source file scraped.
			get {
				string return_value = "NULL";
				try {
					return_value = File.ReadLines(db_path).First(); // Get first line
				}
				catch { }
				return return_value;
			}
		}

		public bool IsCurrentVersion(string source_filepath) {
			// Note: Version refers to size in bytes of source file

			// Get Source Version
			string source_version = "NULL_SRC";
			using (var md5 = System.Security.Cryptography.MD5.Create()) {
				using (var stream = File.OpenRead(source_filepath)) {
					source_version = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}

			// Get Current Version
			string current_version = "NULL_DB";
			try {
				current_version = File.ReadLines(db_path).First(); // intersting way to get only the second line
			}
			catch {
				Error.NoTrigger.MissingDatabase();
			}

			// Check Version
			return source_version == current_version;
		}

		public void Scrape(string source_filepath) {
			// File Path
			string file_path = source_filepath;

			// Raw File as String[]
			string[] file;
			try {
				file = File.ReadAllLines(file_path);
			}
			catch (Exception ex) {
				Error.NoTrigger.Unknown(ex.Message);
				return;
			}

			// Make the new file to deposit the findings
			StreamWriter writer = new StreamWriter(new FileStream(db_path, FileMode.Create));

			// Write version of source file
			// Version is the MD5 sum of the source file scraped.
			string version = "NULL";
			using (var md5 = System.Security.Cryptography.MD5.Create()) {
				using (var stream = File.OpenRead(source_filepath)) {
					version = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}
			writer.WriteLine(version); // Line 1 is MD5 sum

			// Insertion Point
			int ip = 0; // Usually 131757 - 139645
						// special cases 137904, 138143, 139056, 139138, 139166, 139226 (have attribute type), a lot at end

			// Find Insertion Point
			for (int i = 0; i < file.Length; i++) {
				if (Regex.IsMatch(file[i], "^\\t\"attributes\"$")) {
					ip = i;
					break;
				}
			}

			// Stop if no insertion point found
			if (ip == 0) {
				Error.NoTrigger.Unknown("Could not find insertion point.");
				return;
			}

			// Block Tracking
			int block = 0;
			string block_name = ""; // Everything has a name
			string block_format = "null_value"; // Not everything has a type; I don't want these.
			string block_type = "null_type"; // Very few have these; usually for string. Default Float

			// Start Operation
			for (int i = ip; i < file.Length; i++) {
				string line = file[i];
				List<string> token = new List<string>();

				// Get Tokens of Line
				foreach (Match match in Regex.Matches(line, "(\"([^\"]*)\"|{|})")) {
					token.Add(match.ToString());
				}

				// Scrape Ops
				for (int j = 0; j < token.Count; j++) {
					if (token[j].ToUpper() == "\"name\"".ToUpper()) {
						block_name = token[j + 1].Replace("\"", "");
					}
					if (token[j].ToUpper() == "\"description_format\"".ToUpper()) {
						block_format = token[j + 1].Replace("\"", "");
					}
					if (token[j].ToUpper() == "\"attribute_type\"".ToUpper()) {
						block_type = token[j + 1].ToUpper().Replace("\"", "");
					}
				}

				// Curly Ops
				if (Regex.IsMatch(line, "{")) {
					block++;
				}
				else if (Regex.IsMatch(line, "}")) {
					block--;

					if (block == 0) {
						break;
					}

					if (block_name.Length > 0) {
						writer.WriteLine(block_name);
						writer.WriteLine("\t" + block_format);

						// Handle Null Format and Type
						if (block_type == "null_type" && block_format != "null_value") {
							writer.WriteLine("\t" + "FLOAT");
						}
						else {
							writer.WriteLine("\t" + block_type);
						}
					}

					// Reset Values
					block_name = "";
					block_format = "null_value";
					block_type = "null_type";

				}
			}
			writer.Dispose();
		}
	}
}
