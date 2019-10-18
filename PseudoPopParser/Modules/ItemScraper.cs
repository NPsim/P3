using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {

	class ItemScraper : IDisposable {

		private class PrefabProfile {
			public string ID;
			public string Localization;
			public string DefaultSlot;
			public string[] Slot = { "0", "0", "0", "0", "0", "0", "0", "0", "0" };
			public bool AllClass;
			public List<string> Prefabs = new List<string>();
			public Dictionary<string, string> Attributes = new Dictionary<string, string>();
			public PrefabProfile() { }
			public void Merge(PrefabProfile Remote) {
				if (!string.IsNullOrEmpty(Remote.Localization)) {
					this.Localization = Remote.Localization;
				}
				if (!string.IsNullOrEmpty(Remote.DefaultSlot)) {
					this.DefaultSlot = Remote.DefaultSlot;
				}
				for (int ClassIndex = 0; ClassIndex < 9; ClassIndex++) {
					if (Remote.Slot[ClassIndex] != "0") {
						this.Slot[ClassIndex] = Remote.Slot[ClassIndex];
					}
				}
				if (Remote.AllClass) {
					this.AllClass = true;
				}
				if (Remote.Prefabs.Count > 0) {
					foreach (string Entry in Remote.Prefabs) {
						this.Prefabs.Add(Entry);
					}
				}
				if (Remote.Attributes.Keys.Count > 0) {
					foreach (string Key in Remote.Attributes.Keys) {
						this.Attributes[Key] = Remote.Attributes[Key];
					}
				}
			}
		}

		private class ItemProfile : PrefabProfile {
			public string Name;
			public ItemProfile() { }
		}

		private readonly string LocalDatabasePath = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\ItemDB.ffd";
		private readonly string RemotePath;
		private readonly string[] RemoteRawLines;
		private Dictionary<string, PrefabProfile> PrefabLookUp = new Dictionary<string, PrefabProfile>();
		private readonly string RemoteMD5 = "00000000000000000000000000000000";
		private readonly StreamWriter Writer;
		public string CurrentVersion { get; private set; } = "00000000000000000000000000000000";

		public ItemScraper(string ItemsGamePath) {
			this.RemotePath = ItemsGamePath;
			this.RemoteRawLines = File.ReadAllLines(ItemsGamePath);
			this.RemoteMD5 = GenerateMD5(ItemsGamePath);

			try {
				this.CurrentVersion = File.ReadLines(LocalDatabasePath).First();
			}
			catch {
				this.CurrentVersion = "Missing Local Database";
			}
			Writer = new StreamWriter(new FileStream(LocalDatabasePath, FileMode.Create));
		}

		public void Dispose() {
			Writer.Close();
		}

		public bool IsCurrentVersion() => GenerateMD5(this.RemotePath) == GenerateMD5(this.LocalDatabasePath);

		private string GenerateMD5(string file_path) {
			string md5_hash;
			using (var md5 = System.Security.Cryptography.MD5.Create()) {
				using (var stream = File.OpenRead(file_path)) {
					md5_hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}
			return md5_hash;
		}

		private int[] FindInsertionPoints() {
			int[] ip = { 0, 0 }; // [0]: Prefabs [1]: Items

			for (int i = 0; i < RemoteRawLines.Length; i++) {
				if (ip[0] > 0 && ip[1] > 0) break;
				if (RemoteRawLines[i] == "\t\"prefabs\"") {
					ip[0] = i;
				}
				else if (RemoteRawLines[i] == "\t\"items\"") {
					ip[1] = i;
				}
			}
			return ip;
		}

		private int GetClassIndex(string ClassName) {
			switch (ClassName.ToUpper()) {
				case "SCOUT":
					return 0;
				case "SOLDIER":
					return 1;
				case "PYRO":
					return 2;
				case "DEMOMAN":
					return 3;
				case "HEAVY":
					return 4;
				case "ENGINEER":
					return 5;
				case "MEDIC":
					return 6;
				case "SNIPER":
					return 7;
				case "SPY":
					return 8;
			}
			return -1;
		}

		private PrefabProfile GetPrefab(string TargetID) {
			//PrintColor.DebugInternalLine("\tNew Curse target: " + target);
			var TargetPrefab = this.PrefabLookUp[TargetID];
			var Final = new PrefabProfile();

			// Import target's prefabs (recursively)
			if (TargetPrefab.Prefabs.Count > 0) {
				foreach (string Import in TargetPrefab.Prefabs) {
					PrefabProfile p = GetPrefab(Import); // Recurse here
					Final.Merge(p);
				}
			}
			Final.Merge(TargetPrefab);

			//PrintColor.DebugInternalLine("\tEnd Curse target: " + target);
			return Final;
		}

		private void Operate() {
			this.Writer.WriteLine(RemoteMD5);

			int[] IP = FindInsertionPoints();

			for (int Line = IP[0]; Line < RemoteRawLines.Length; Line++) {

			}
		}

		public void Scrape(string items_game) { // TODO Break into multiple methods: Prefab Setup and Item Compilation

			//StreamWriter writer = new StreamWriter(new FileStream(LocalDatabasePath, FileMode.Create));

			// Version Tracking
			Writer.WriteLine(RemoteMD5); // Line 1 is MD5 sum
			CurrentVersion = RemoteMD5;

			// Insertion Point
			int[] ip = { 0, 0 };

			// Block Tracking
			List<string> block_path = new List<string>();

			// Find Insertion Points
			for (int i = 0; i < RemoteRawLines.Length; i++) {
				if (ip[0] > 0 && ip[1] > 0) {
					break;
				}

				if (Regex.IsMatch(RemoteRawLines[i], "^\\t\"prefabs\"$")) {
					ip[0] = i;
				}
				else if (Regex.IsMatch(RemoteRawLines[i], "^\\t\"items\"$")) {
					ip[1] = i;
				}
			}
			if (ip.Contains(0)) {
				Error.WriteNoIncrement("{f:Red}Unknown{r} exception '{$0}'", -1, 998, "Could not find insertion point.");
				return;
			}

			/* Prefab Operation */
			PrefabProfile Prefab = new PrefabProfile();

			for (int i = ip[0]; i < RemoteRawLines.Length; i++) {

				// Skip completely whitespace lines
				if (Regex.IsMatch(RemoteRawLines[i], @"^\s*$")) {
					continue;
				}

				// Specify General-Use vars
				string line = RemoteRawLines[i];
				string look_back_line = RemoteRawLines[i - 1];
				List<string> tokens = new List<string>();

				// Get Tokens of Line
				foreach (Match match in Regex.Matches(line, "(\"([^\"]*)\")")) { // Match double quote bounded strings
					tokens.Add(match.ToString());
				}

				// Read contents of line
				for (int j = 0; j < tokens.Count; j++) {
					if (block_path.Count == 2) { // Relying on autogeneration here
						if (tokens[j].ToUpper() == "\"PREFAB\"") {
							string Contents = tokens[j + 1].Trim('"');
							foreach (string ID in Contents.Split(' ')) {
								Prefab.Prefabs.Add(ID);
							}
						}
						if (tokens[j].ToUpper() == "\"ITEM_SLOT\"") {
							Prefab.DefaultSlot = tokens[j + 1].Trim('"');
						}
						if (tokens[j].ToUpper() == "\"ITEM_NAME\"") {
							Prefab.Localization = tokens[j + 1].Trim('"');
						}
						if (tokens[0].ToUpper() == "\"ITEM_CLASS\"" && tokens[1].ToUpper().StartsWith("\"TF_WEARABLE")) {
							Prefab.AllClass = true;
						}
						if (tokens[0].ToUpper() == "\"ACT_AS_WEARABLE\"" && tokens[1] == "\"1\"") {
							Prefab.AllClass = true;
						}
					}
				}

				// Get Explicit Slots
				if (block_path.Count == 3 && block_path.Last() == "\"used_by_classes\"" && tokens.Count >= 2) {
					Prefab.Slot[GetClassIndex(tokens[0].Trim('"'))] = tokens[1].Trim('"');
				}

				// Get Static Attributes
				if (block_path.Count == 3 && block_path.Last() == "\"static_attrs\"" && tokens.Count >= 2) {
					Prefab.Attributes[tokens[0]] = tokens[1];
				}

				// Get Attributes
				if (block_path.Count == 4 && block_path.ElementAt(block_path.Count - 2) == "\"attributes\"" && tokens.Count >= 2) {
					// Add attribute on "VALUE" key-pair line
					if (tokens[0].ToUpper() == "\"VALUE\"") {
						Prefab.Attributes[block_path.Last()] = tokens[1];
					}
				}

				// Curly Ops
				if (Regex.IsMatch(line, "{")) {
					block_path.Add(Regex.Replace(look_back_line, @"^\s+|\s+$", "")); // Increments block_path.Count

					if (block_path.Count == 2) {
						Prefab.ID = block_path.Last().Trim('"');
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
						if (Prefab.ID != null) {

							// Check attempt to add duplicate key
							if (this.PrefabLookUp.Keys.Contains(Prefab.ID)) {
								Error.Write("ATTEMPT TO ADD DUPLICATE KEY");
							}

							this.PrefabLookUp[Prefab.ID] = Prefab;
						}

						// Reset Item
						Prefab = new PrefabProfile();
					}
				}

			} // End of Prefab Section

			/* Item Operation */
			string block_key = "";
			block_path = new List<string>();
			ItemProfile Item = new ItemProfile();

			// Read File
			for (int i = ip[1]; i < RemoteRawLines.Length; i++) {

				// Specify General-Use vars
				string line = RemoteRawLines[i];
				string look_back_line = RemoteRawLines[i - 1];
				List<string> tokens = new List<string>();

				// Get Tokens of Line
				foreach (Match match in Regex.Matches(line, "(\"([^\"]*)\"|{|})")) { // Match double quote bounded strings
					tokens.Add(match.ToString());
				}

				// Read contents of line
				for (int j = 0; j < tokens.Count; j++) {
					if (block_path.Count == 2) { // Relying on autogeneration here
						if (tokens[j].ToUpper() == "\"PREFAB\"") {


							foreach (Match prefab_target_match in Regex.Matches(tokens[j + 1], "[^ \"\\s]\\S+[^\"\\s]")) {
								string prefab_target = prefab_target_match.Value;

								// Check valid prefab
								/*if (!prefabs.ContainsKey(prefab_target)) {
									PrintColor.DebugInternalLine("INVALID KEY: " + prefab_target);
								}*/

								// Import config from prefab dictionary
								PrefabProfile p = GetPrefab(prefab_target); // Recursive retrieval
								Item.Merge(p);
							}
						}
						if (tokens[j].ToUpper() == "\"NAME\"") {
							Item.Name = tokens[j + 1].Replace("\"", "");
						}
						if (tokens[j].ToUpper() == "\"ITEM_SLOT\"") {
							Item.DefaultSlot = tokens[j + 1].Replace("\"", "");
						}
						if (tokens[0].ToUpper() == "\"ITEM_CLASS\"" && tokens[1].ToUpper().StartsWith("\"TF_WEARABLE")) {
							Item.AllClass = true;
						}
						if (tokens[0].ToUpper() == "\"ACT_AS_WEARABLE\"" && tokens[1] == "\"1\"") {
							Item.AllClass = true;
						}
					}
				}

				// Get Explicit Slots
				if (block_path.Count == 3 && block_path.Last() == "\"used_by_classes\"" && tokens.Count >= 2) {
					Item.Slot[GetClassIndex(tokens[0].Trim('"'))] = tokens[1].Trim('"');
				}

				// Get Static Attributes
				if (block_path.Count == 3 && block_path.Last() == "\"static_attrs\"" && tokens.Count >= 2) {
					Item.Attributes[tokens[0]] = tokens[1];
				}

				// Get Attributes
				if (block_path.Count == 4 && block_path.ElementAt(block_path.Count - 2) == "\"attributes\"" && tokens.Count >= 2) {
					// Add attribute on "VALUE" key-pair line
					if (tokens[0].ToUpper() == "\"VALUE\"") {
						Item.Attributes[block_path.Last()] = tokens[1];
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
						if (Item.Name != null) {
							// Write Traits
							this.Writer.WriteLine(Item.Name);

							if (!string.IsNullOrEmpty(Item.Localization)) {
								this.Writer.WriteLine("\tLOC " + Item.Localization);
							}
							if (!string.IsNullOrEmpty(Item.DefaultSlot)) {
								this.Writer.WriteLine("\tDFS " + Item.DefaultSlot);
							}

							// Write Slots
							if (Item.AllClass || string.Join("", Item.Slot) == "111111111") {
								this.Writer.WriteLine("\tSLT ALLCLASS");
							}
							else if (string.Join("", Item.Slot) != "000000000") {
								this.Writer.Write("\tSLT ");
								for (int ClassIndex = 0; ClassIndex <= 7; ClassIndex++) {
									this.Writer.Write(Item.Slot[ClassIndex] + " ");
								}
								this.Writer.WriteLine(Item.Slot[8]);
							}

							// Write Attributes
							foreach (string Key in Item.Attributes.Keys) {
								this.Writer.WriteLine("\t{0} {1}", Key, Item.Attributes[Key]);
							}
						}

						// Reset Item
						Item = new ItemProfile();
					}
				}
			} // End of Item Section
		}
	}
}
