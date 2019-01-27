using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {

	internal class ItemDatabase {

		private static Dictionary<string, Dictionary<string, string>> items = new Dictionary<string, Dictionary<string, string>>();
		private static readonly string[] useless_attributes = {
				"loc_name", // Identifier
				"slot", // Identifier
				"base", // Identifier
				"min_viewmodel_offset",
				"inspect_viewmodel_offset",
				"disable fancy class select anim",
				"kill eater kill type",
				"kill eater score type",
				"kill eater score type 2",
				"kill eater score type 3",
				"weapon_allow_inspect",
				"limited quantity item",
				"is marketable",
				"cosmetic_allow_inspect",
				"weapon_allow_inspect",
				"meter_label",
				"weapon_uses_stattrak_module",
				"weapon_stattrak_module_scale"
			};

		public static void Build() {

			// Database must exist
			string db_path = Program.root_directory + @"\datatypes\item_db.uwu";
			if (!File.Exists(db_path)) {
				Error.NoTrigger.MissingDatabase();
				return;
			}

			string[] db = File.ReadAllLines(db_path);
			string last_item = "";
			for (int i = 1; i < db.Count(); i++) { // Line 0 is version MD5
				string line = db[i];

				// Add New Item
				if (Regex.IsMatch(line, @"^\S")) {
					items[line] = new Dictionary<string, string>();
					last_item = line;
				}

				// Add new Attribute to last item added
				else if (Regex.IsMatch(line, @"^\t\S")) {
					MatchCollection tokens = Regex.Matches(line, "(\"([^\"]*)\")");

					// Get ID Tokens if database line has no double quotes
					if (tokens.Count <= 1) {
						tokens = Regex.Matches(line, @"\S+");
					}

					string key = Regex.Replace(tokens[0].Value, "\"", "");
					string value = Regex.Replace(tokens[1].Value, "\"", "");
					items[last_item][key] = value;
				}
			}
		}

		public static bool Exists(string item) {
			foreach (string entry in items.Keys) {
				if (entry.ToUpper() == item.ToUpper()) {
					return true;
				}
			}
			return false;
		}

		public static string NormalizeName(string item_name) {
			foreach (string real_name in items.Keys) {
				if (item_name.ToUpper() == real_name.ToUpper()) {
					return real_name;
				}
			}
			throw new Exception("InvalidNormalItemName");
		}

		public static string GetSlot(string item) {
			string normalized = NormalizeName(item);
			return items[normalized]["slot"]; // Possible Values: "", "primary", "secondary", "melee", "pda", "pda2", "building", "action", "head", "misc"
		}

		public static string GetLocalization(string item) {
			item = NormalizeName(item);
			return items[item]["loc_name"];
		}

		public static Dictionary<string, string> AttributesNoRemove(string item) {
			item = NormalizeName(item);
			return items[item];
		}

		public static Dictionary<string, string> Attributes(string item) {
			item = NormalizeName(item);
			Dictionary<string, string> attributes = items[item];
			foreach (string key in useless_attributes) {
				attributes.Remove(key);
			}
			return attributes;
		}

		public static List<string> List {
			get {
				return items.Keys.ToList();
			}
		}

		public static string[] Array {
			get {
				return items.Keys.ToArray();
			}
		}
	}
}
