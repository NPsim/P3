using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PseudoPopParser {

	internal class ItemDatabase {

		private static Dictionary<string, Dictionary<string, string>> items = new Dictionary<string, Dictionary<string, string>>();

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

				// 
				if (Regex.IsMatch(line, @"^\S")) {
					//item_list.Add(line);
					items[line] = new Dictionary<string, string>();
					last_item = line;

					//PrintColor.DebugLine("New Item: " + line);
				}
				else if (Regex.IsMatch(line, @"^\t\S")) {
					//var ml = Regex.Matches(line, @"\S+"); // Match nonquote
					//var mq = Regex.Matches(line, "[^ \"\\s]\\S*[^\"\\s]");  // Match quote

					MatchCollection tokens = Regex.Matches(line, "(\"([^\"]*)\")");

					if (tokens.Count <= 1) {
						tokens = Regex.Matches(line, @"\S+");
					}

					string key = Regex.Replace(tokens[0].Value, "\"", "");
					string value = Regex.Replace(tokens[1].Value, "\"", "");
					items[last_item][key] = value;

					//PrintColor.DebugLine("\t" + "{b:Blue}{0}{r}" + "\t" + "{b:Red}{1}{r}", key, value);
				}

				{ }
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

		public static Dictionary<string, string> AttributesNoRemove(string item) {
			return items[item];
		}

		public static Dictionary<string, string> Attributes(string item) {
			Dictionary<string, string> attributes = items[item];
			string[] remove = {
				"loc_name",
				"slot",
				"base",
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
			foreach (string key in remove) {
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
