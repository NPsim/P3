using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PseudoPopParser {
	class Scraper:IDisposable {

		private PopParser p;
		private string attributes_filepath = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\item_attributes.owo";
		private string items_filepath = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\item_db.owo";

		public Scraper (PopParser parser) {
			p = parser;
		}

		public void Dispose() {}

		public bool WriteLineIfExists(StreamWriter sw, string value, string message) {
			if (value.Length > 0) {
				sw.WriteLine(message);
				return true;
			}
			return false;
		}

		public string Version {
			get {
				string return_value = "VERSION DOES NOT EXIST";
				try {
					return_value = File.ReadLines(attributes_filepath).Skip(1).Take(1).First();
				}
				catch {}
				return return_value;
			}
		}

		public bool AttUpToDate(string source_filepath) {
			// Note: Version refers to size in bytes of source file

			// Get Source Version
			long source_version = new FileInfo(source_filepath).Length;

			// Get Current Version
			long current_version = -1;
			try {
				current_version = Int64.Parse(File.ReadLines(attributes_filepath).Skip(1).Take(1).First()); // intersting way to get only the second line
			}
			catch {}

			// Check Version
			return source_version == current_version;
		}

		public void ScrapeAttributes(string source_filepath) {

			// File Path
			string file_path = source_filepath;

			// Raw File as String[]
			string[] file;
			try {
				file = File.ReadAllLines(file_path);
			}
			catch (Exception e) {
				//Console.WriteLine("Could not find directory: " + source_filepath);
				p.Error(e.Message);
				return;
			}

			// Make the new file to deposit the findings
			StreamWriter writer = new StreamWriter(new FileStream(attributes_filepath, FileMode.Create));

			// Write version of source file
			long version = new FileInfo(source_filepath).Length;

			writer.WriteLine("VERSION ID:");
			writer.WriteLine(version);

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
				Console.WriteLine("Could not find insertion point.");
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
			p.InfoLine("Finished scraping for attributes.");
			p.InfoLine("Current version is now " + version);
		}
		public void ScrapeItems(string source_filepath) { // A lot of this is redundant compared to ScrapeAttributes(). TODO refactor this

			// File Path
			string file_path = source_filepath;

			// Raw File as String[]
			string[] file;
			try {
				file = File.ReadAllLines(file_path);
			}
			catch (Exception e) {
				//Console.WriteLine("Could not find directory: " + source_filepath);
				p.Error(e.Message);
				return;
			}

			// Make the new file to deposit the findings
			StreamWriter writer = new StreamWriter(new FileStream(items_filepath, FileMode.Create));

			// Write version of source file
			long version = new FileInfo(source_filepath).Length;

			writer.WriteLine("VERSION ID:");
			writer.WriteLine(version);

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
				Console.WriteLine("Could not find insertion point.");
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
							//Console.WriteLine("Wrote due to line " + (i + 1) + "\tBlock was " + block + " | " + block_item);
							WriteLineIfExists(writer, block_slot, "\tslot " + block_slot);
							WriteLineIfExists(writer, block_base, "\tbase " + block_base);
							//WriteLineIfExists(writer, block_reskin, "\treskin " + block_reskin);
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
			p.InfoLine("Finished scraping for items.");
			p.InfoLine("Current version is now " + version);
		}
	}
}
