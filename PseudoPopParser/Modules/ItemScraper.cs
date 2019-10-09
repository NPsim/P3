using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {
	class ItemScraper : IDisposable {

		private readonly string p3_db_path = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\item_db.uwu";
		private readonly string tf_db_path;
		private Dictionary<string, Dictionary<string, string>> prefabs = new Dictionary<string, Dictionary<string, string>>();
		private string[] items_file;
		private readonly string tf_md5;
		private string p3_md5;

		public ItemScraper(string items_game_path) {
			tf_db_path = items_game_path;
			//writer = new StreamWriter(new FileStream(db_path, FileMode.Create));

			tf_md5 = GenerateMD5(items_game_path);

			// Raw File as string[]
			try {
				items_file = File.ReadAllLines(items_game_path);
			}
			catch (Exception e) {
				Error.WriteNoIncrement("{f:Cyan}Unknown{r} exception '{$0}'", -1, 998, e.Message);
				return;
			}

			// Try to retrieve local db version
			try {
				p3_md5 = File.ReadLines(p3_db_path).First(); // Get first line
				if (p3_md5.Length != 32) {
					throw new Exception();
				}
			}
			catch {
				p3_md5 = "DATABASE MISSING";
			}
		}

		public void Dispose() { }

		private string GenerateMD5(string file_path) {
			string md5_hash;
			using (var md5 = System.Security.Cryptography.MD5.Create()) {
				using (var stream = File.OpenRead(file_path)) {
					md5_hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}
			return md5_hash;
		}

		public bool WriteEntryLine(StreamWriter writer, string value, string message) {
			if (value.Length > 0) {
				writer.WriteLine(message);
				return true;
			}
			return false;
		}

		public string CurrentVersion {
			get {
				return p3_md5;
			}
		}

		public bool IsCurrentVersion() {
			string source_version = GenerateMD5(tf_db_path);
			string current_version = GenerateMD5(p3_db_path);
			return source_version == current_version;
		}

		private int[] FindInsertionPoints(string items_game) {
			int[] ip = { 0, 0 };

			for (int i = 0; i < items_file.Length; i++) {
				if (ip[0] > 0 && ip[1] > 0) {
					break;
				}

				if (Regex.IsMatch(items_file[i], "^\\t\"prefabs\"$")) {
					ip[0] = i;
				}
				else if (Regex.IsMatch(items_file[i], "^\\t\"items\"$")) {
					ip[1] = i;
				}
			}

			return ip;
		}

		private Dictionary<string, string> GetPrefab (string target) {
			//PrintColor.DebugInternalLine("\tNew Curse target: " + target);
			var ret = new Dictionary<string, string>();

			// Import target's prefabs (recursively)
			if (prefabs[target]["ID prefab"].Length > 0) {
				foreach (Match m in Regex.Matches(prefabs[target]["ID prefab"], "[^ \"\\s]\\S*[^\"\\s]")) {
					string target_prefab = m.Value;
					var p = GetPrefab("\"" + target_prefab + "\""); // Recurse here
					foreach(string key in p.Keys) {
						if (p[key].Length > 0) {
							ret[key] = p[key];
						}
					}
				}
			}

			// Get target's prefab values
			foreach (string key in prefabs[target].Keys) {
				if (prefabs[target][key].Length > 0) {
					ret[key] = prefabs[target][key];
				}
			}

			//PrintColor.DebugInternalLine("\tEnd Curse target: " + target);
			return ret;
		}

		public void Scrape(string items_game) { // TODO Break into multiple methods: Prefab Setup and Item Compilation

			StreamWriter writer = new StreamWriter(new FileStream(p3_db_path, FileMode.Create));

			// Version Tracking
			writer.WriteLine(tf_md5); // Line 1 is MD5 sum
			p3_md5 = tf_md5;

			// Insertion Point
			int[] ip = { 0, 0 };

			// Block Tracking
			List<string> block_path = new List<string>();

			// Find Insertion Points
			for (int i = 0; i < items_file.Length; i++) {
				if (ip[0] > 0 && ip[1] > 0) {
					break;
				}

				if (Regex.IsMatch(items_file[i], "^\\t\"prefabs\"$")) {
					ip[0] = i;
				}
				else if (Regex.IsMatch(items_file[i], "^\\t\"items\"$")) {
					ip[1] = i;
				}
			}
			if (ip.Contains(0)) {
				Error.WriteNoIncrement("{f:Cyan}Unknown{r} exception '{$0}'", -1, 998, "Could not find insertion point.");
				return;
			}

			/* Prefab Operation */
			string prefab_key = "";
			var characteristics = new Dictionary<string, string> {
				["ID prefab"] = "",
				["ID item_slot"] = "",
				["ID loc_name_id"] = ""
			};

			for (int i = ip[0]; i < items_file.Length; i++) {

				// Skip completely whitespace lines
				if (Regex.IsMatch(items_file[i], @"^\s*$")) {
					continue;
				}

				// Specify General-Use vars
				string line = items_file[i];
				string look_back_line = items_file[i - 1];
				List<string> tokens = new List<string>();

				// Get Tokens of Line
				foreach (Match match in Regex.Matches(line, "(\"([^\"]*)\")")) { // Match double quote bounded strings
					tokens.Add(match.ToString());
				}

				// Read contents of line
				for (int j = 0; j < tokens.Count; j++) {
					if (block_path.Count == 2) { // Relying on autogeneration here
						if (tokens[j].ToUpper() == "\"prefab\"".ToUpper()) {
							characteristics["ID prefab"] = tokens[j + 1].Replace("\"", "");
						}
						if (tokens[j].ToUpper() == "\"item_slot\"".ToUpper()) {
							characteristics["ID item_slot"] = tokens[j + 1].Replace("\"", "");
						}
						if (tokens[j].ToUpper() == "\"item_name\"".ToUpper()) {
							characteristics["ID loc_name_id"] = tokens[j + 1].Replace("\"", "");
						}
					}
				}

				// Get Static Attributes
				if (block_path.Count == 3 && block_path.Last() == "\"static_attrs\"" && tokens.Count >= 2) {
					characteristics[tokens[0]] = tokens[1];
				}

				// Get Attributes
				if (block_path.Count == 4 && block_path.ElementAt(block_path.Count - 2) == "\"attributes\"" && tokens.Count >= 2) {
					// Add attribute on "VALUE" key-pair line
					if (tokens[0].ToUpper() == "\"VALUE\"") {
						characteristics[block_path.Last()] = tokens[1];
					}
				}

				// Curly Ops
				if (Regex.IsMatch(line, "{")) {
					block_path.Add(Regex.Replace(look_back_line, @"^\s+|\s+$", "")); // Increments block_path.Count

					if (block_path.Count == 2) {
						prefab_key = block_path.Last();
					}

				}
				else if (Regex.IsMatch(line, "}")) {
					block_path.RemoveAt(block_path.Count() - 1); // Decrements block_path.Count

					// Break on end of block
					if (block_path.Count == 0) {
						break;
					}

					// Write to main prefan dictionary
					if (block_path.Count == 1) {
						if (prefab_key.Length > 0) {

							// Check attempt to add duplicate key
							if (prefabs.Keys.Contains(prefab_key)) {
								Error.Write("ATTEMPT TO ADD DUPLICATE KEY");
							}

							prefabs[prefab_key] = characteristics;
						}

						// Reset Item
						prefab_key = "";
						characteristics = new Dictionary<string, string> {
							["ID prefab"] = "",
							["ID item_slot"] = "",
							["ID loc_name_id"] = ""
						};
					}
				}

			} // End of Prefab Section

			/* Item Operation */
			string block_key = "";
			block_path = new List<string>();
			Dictionary<string, string> item_dict = new Dictionary<string, string> {
				["ID name"] = "",
				["ID loc_name_id"] = "",
				["ID item_slot"] = "",
				["ID base"] = ""
			};

			// Read File
			for (int i = ip[1]; i < items_file.Length; i++) {

				// Specify General-Use vars
				string line = items_file[i];
				string look_back_line = items_file[i - 1];
				List<string> tokens = new List<string>();

				// Get Tokens of Line
				foreach (Match match in Regex.Matches(line, "(\"([^\"]*)\"|{|})")) { // Match double quote bounded strings
					tokens.Add(match.ToString());
				}

				// Read contents of line
				for (int j = 0; j < tokens.Count; j++) {
					if (block_path.Count == 2) { // Relying on autogeneration here
						if (tokens[j].ToUpper() == "\"prefab\"".ToUpper()) {


							foreach (Match prefab_target_match in Regex.Matches(tokens[j + 1], "[^ \"\\s]\\S+[^\"\\s]")) {
								string prefab_target = "\"" + prefab_target_match.Value + "\"";

								// Check valid prefab
								/*if (!prefabs.ContainsKey(prefab_target)) {
									PrintColor.DebugInternalLine("INVALID KEY: " + prefab_target);
								}*/
								
								// Import config from prefab dictionary
								var p = GetPrefab(prefab_target); // Recursive retrieval
								foreach (string key in p.Keys) {
									item_dict[key] = p[key];
								}
							}
						}
						if (tokens[j].ToUpper() == "\"name\"".ToUpper()) {
							item_dict["ID name"] = tokens[j + 1].Replace("\"", "");
						}
						if (tokens[j].ToUpper() == "\"item_slot\"".ToUpper()) {
							item_dict["ID item_slot"] = tokens[j + 1].Replace("\"", "");
						}
						if (tokens[j].ToUpper() == "\"baseitem\"".ToUpper()) {
							item_dict["ID base"] = tokens[j + 1].Replace("\"", "");
						}
					}
				}

				// Get Static Attributes
				if (block_path.Count == 3 && block_path.Last() == "\"static_attrs\"" && tokens.Count >= 2) {
					item_dict[tokens[0]] = tokens[1];
				}

				// Get Attributes
				if (block_path.Count == 4 && block_path.ElementAt(block_path.Count - 2) == "\"attributes\"" && tokens.Count >= 2) {
					// Add attribute on "VALUE" key-pair line
					if (tokens[0].ToUpper() == "\"VALUE\"") {
						item_dict[block_path.Last()] = tokens[1];
					}
				}

				// Curly Ops
				if (Regex.IsMatch(line, "{")) {
					block_path.Add(Regex.Replace(look_back_line, @"^\s+|\s+$", "")); // Increments block_path.Count

					if (block_path.Count == 2) {
						block_key = block_path.Last();
					}
				}
				else if (Regex.IsMatch(line, "}")) {
					block_path.RemoveAt(block_path.Count() - 1); // Decrements block_path.Count

					// Break on end of block
					if (block_path.Count == 0) {
						break;
					}

					if (block_path.Count == 1) {

						// Write to file
						if (WriteEntryLine(writer, item_dict["ID name"], item_dict["ID name"])) {
							WriteEntryLine(writer, item_dict["ID loc_name_id"], "\tloc_name " + item_dict["ID loc_name_id"]);
							WriteEntryLine(writer, item_dict["ID item_slot"], "\tslot " + item_dict["ID item_slot"]);
							WriteEntryLine(writer, item_dict["ID base"], "\tbase " + item_dict["ID base"]);

							// Write Attributes
							foreach (string attribute_key in item_dict.Keys) {
								if (attribute_key.Substring(0, 2) != "ID") {
									WriteEntryLine(writer, item_dict[attribute_key], "\t" + attribute_key + " " + item_dict[attribute_key]);
								}
							}
						}

						// Reset Item
						item_dict = new Dictionary<string, string> {
							["ID name"] = "",
							["ID loc_name_id"] = "",
							["ID item_slot"] = "",
							["ID base"] = ""
						};
					}
				}
			} // End of Item Section

			// Abandon Ship
			writer.Dispose();
		}
	}
}
