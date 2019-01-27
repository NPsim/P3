using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {

	internal class ItemTracker {

		private static Dictionary<string, int> modded_items = new Dictionary<string, int>(); // String : Non-normalized Item Name	Int : Line Number Look-Back
		private static Dictionary<string, string> slot = new Dictionary<string, string> {
			["primary"] = "",
			["secondary"] = "",
			["melee"] = "",
			["pda"] = "", // Engineer Build
			["pda2"] = "", // Engineer Destroy, Spy Watch
			["building"] = "", // Spy Sapper
			["action"] = "", // (Concat) Spellbook, Grapplehook, Canteen, etc
			["head"] = "", // (Concat)
			["misc"] = "" // (Concat)
		};
		private static readonly Dictionary<string, string[]> default_loadout = new Dictionary<string, string[]> {
			["SCOUT"] = new string[] { "TF_WEAPON_SCATTERGUN", "TF_WEAPON_PISTOL_SCOUT", "TF_WEAPON_BAT" },
			["SOLDIER"] = new string[] { "TF_WEAPON_ROCKETLAUNCHER", "TF_WEAPON_SHOTGUN_SOLDIER", "TF_WEAPON_SHOVEL" },
			["PYRO"] = new string[] { "TF_WEAPON_FLAMETHROWER", "TF_WEAPON_SHOTGUN_PYRO", "TF_WEAPON_FIREAXE" },
			["DEMOMAN"] = new string[] { "TF_WEAPON_GRENADELAUNCHER", "TF_WEAPON_PIPEBOMBLAUNCHER", "TF_WEAPON_BOTTLE" },
			["HEAVY"] = new string[] { "TF_WEAPON_MINIGUN", "TF_WEAPON_SHOTGUN_HWG", "TF_WEAPON_FISTS" },
			["ENGINEER"] = new string[] { "TF_WEAPON_SHOTGUN_PRIMARY", "TF_WEAPON_PISTOL", "TF_WEAPON_WRENCH", "TF_WEAPON_PDA_ENGINEER_BUILD", "TF_WEAPON_PDA_ENGINEER_DESTROY" },
			["MEDIC"] = new string[] { "TF_WEAPON_SYRINGEGUN_MEDIC", "TF_WEAPON_MEDIGUN", "TF_WEAPON_BONESAW" },
			["SNIPER"] = new string[] { "TF_WEAPON_SNIPERRIFLE", "TF_WEAPON_SMG", "TF_WEAPON_CLUB" },
			["SPY"] = new string[] { "TF_WEAPON_REVOLVER", "TF_WEAPON_KNIFE", "TF_WEAPON_BUILDER_SPY", "TF_WEAPON_INVIS", "TF_WEAPON_PDA_SPY" }
		};
		private static readonly string[] single_slots = { "primary", "secondary", "melee", "pda", "pda2", "building" };
		private static readonly string[] concat_slots = { "action", "head", "misc" };

		public static void DebugDisplayContents() {
			PrintColor.DebugInternalLine("\tInventory");
			foreach (string key in slot.Keys) {
				PrintColor.DebugInternalLine(key + ": " + slot[key]);
			}

			PrintColor.DebugInternalLine("\tMods");
			foreach (string key in modded_items.Keys) {
				PrintColor.DebugInternalLine(key + " L: " + modded_items[key]);
			}
		}

		public static void DebugDisplay123() {
			PrintColor.DebugInternalLine("\tPriSecMel: " + slot["primary"] + "|" + slot["secondary"] + "|" + slot["melee"]);
		}

		public static void Add(string item) { // Called on "Item" key
			item = ItemDatabase.NormalizeName(item);

			if (!ItemDatabase.Exists(item)) {
				Warning.ItemInvalid(Program.CurrentLineNumber, item); // Warn "Item does not exist in database"
				return;
			}

			item = ItemDatabase.NormalizeName(item);

			string item_slot = ItemDatabase.GetSlot(item);
			if (concat_slots.Contains(item_slot)) {
				slot[item_slot] = slot[item_slot] + "\"" +  item + "\" ";
			}
			else {
				slot[item_slot] = item;
			}
		}

		public static void Clear() {
			slot = new Dictionary<string, string> {
				["primary"] = "",
				["secondary"] = "",
				["melee"] = "",
				["pda"] = "",
				["pda2"] = "",
				["building"] = "",
				["action"] = "",
				["head"] = "",
				["misc"] = ""
			};
			modded_items = new Dictionary<string, int>();
		}

		public static void FillClass(string class_name) {
			foreach (string default_item in default_loadout[class_name.ToUpper()]) {
				string default_item_slot = ItemDatabase.GetSlot(default_item);
				if (slot[default_item_slot].Length == 0) {
					slot[default_item_slot] = default_item;
				}
			}
		}

		public static bool IsEquipped(string item) {

			if (!ItemDatabase.Exists(item)) {
				return true;
			}

			string item_slot = ItemDatabase.GetSlot(item);
			return Regex.IsMatch(slot[item_slot], item, RegexOptions.IgnoreCase);
		}

		public static void AddModifier(string item_name, int line_number) { // Called on "ItemName" key
			modded_items[item_name] = line_number;
		}

		public static void VerifyModifications() {
			foreach(string itemname in modded_items.Keys) {
				if (!IsEquipped(itemname)) {
					Warning.ItemMissing(modded_items[itemname], itemname); // Warn for "Bot does not have item"
				}
			}
		}

		public static string[] Inventory() {
			List<string> ret = new List<string>();
			
			// Add single slots
			foreach (string slot_key in single_slots) {
				if (slot[slot_key].Length > 0) {
					ret.Add(slot[slot_key]);
				}
			}

			// Add concat slots
			foreach (string slot_key in concat_slots) {
				foreach(Match m in Regex.Matches(slot[slot_key], "(\"([^\"]*)\")")) {
					string item = Regex.Replace(m.Value, "\"", "");
					ret.Add(item);
				}
			}
			return ret.ToArray();
		}

	}
}
