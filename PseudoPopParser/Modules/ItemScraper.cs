using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {
	class ItemScraper : IDisposable {

		private string db_path = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\item_db.uwu";

		public ItemScraper() { }

		public ItemScraper(string item_db) {
			db_path = item_db;
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
				PrintColor.ErrorNoTrigger("Could not find local database.");
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
			catch (Exception e) {
				PrintColor.ErrorNoTrigger(e.Message);
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
			int ip = 0; // Usually 20297 - 131757

			// Find Insertion Point
			for (int i = 0; i < file.Length; i++) {
				if (Regex.IsMatch(file[i], "^\\t\"items\"$")) {
					ip = i;
					break;
				}
			}

			// Stop if no insertion point found
			if (ip == 0) {
				PrintColor.ErrorNoTrigger("Could not find insertion point.");
				return;
			}

			// Block Tracking
			int block = 0;
			int block_item = block;
			string block_name = ""; // Everything has a name
			string block_slot = "";
			string block_base = "";
			string block_reskin = "";

			// Start Operation
			for (int i = ip; i < file.Length; i++) {

				string line = file[i];
				List<string> tokens = new List<string>();

				// Get Tokens of Line
				foreach (Match match in Regex.Matches(line, "(\"([^\"]*)\"|{|})")) { // Match double quote bounded strings
					tokens.Add(match.ToString());
				}

				// Read contents of line
				for (int j = 0; j < tokens.Count; j++) {
					if (block == 2) {
						if (tokens[j].ToUpper() == "\"name\"".ToUpper()) {
							block_name = tokens[j + 1].Replace("\"", "");
							block_item = block;
						}
						if (tokens[j].ToUpper() == "\"item_slot\"".ToUpper()) {
							block_slot = tokens[j + 1].Replace("\"", "");
						}
						if (tokens[j].ToUpper() == "\"baseitem\"".ToUpper()) {
							block_base = tokens[j + 1].Replace("\"", "");
						}
						if (tokens[j].ToUpper() == "\"set_item_remap\"".ToUpper()) {
							block_reskin = tokens[j + 1].Replace("\"", "");
						}
					}
				}

				// Curly Ops
				if (Regex.IsMatch(line, "{")) {
					block++;
				}
				else if (Regex.IsMatch(line, "}")) {
					block--;

					if (block == 0) {
						break; // Terminate Scraping
					}

					// Write to file
					if (block == 1) {
						if (WriteLineIfExists(writer, block_name, block_name)) {
							WriteLineIfExists(writer, block_slot, "\tslot " + block_slot);
							WriteLineIfExists(writer, block_base, "\tbase " + block_base);
						}

						// Reset
						block_name = "";
						block_slot = "";
						block_base = "";
						block_reskin = "";
					}
				}
			}
			writer.Dispose();
		}
	}
}
