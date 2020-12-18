using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {

	internal class ItemTracker {

		private class ItemConfiguration {
			public Dictionary<string, int> ModifierKeys;
			public Dictionary<string, dynamic> Slot;
			public string Class;
			public ItemConfiguration() { }
		}

		private static Dictionary<string, ItemConfiguration> Templates = new Dictionary<string, ItemConfiguration>();
		private static Dictionary<string, int> ModifierKeys = new Dictionary<string, int>(); // String : Raw item name // Int : "ItemName" Key line number lookback
		private static Dictionary<string, dynamic> Slot = new Dictionary<string, dynamic> {
			["primary"] = "",
			["secondary"] = "",
			["melee"] = "",
			["pda"] = "", // Engineer Build
			["pda2"] = "", // Engineer Destroy, Spy Watch
			["building"] = "", // Spy Sapper
			["action"] = new List<string>(), // Spellbook, Grapplehook, Canteen, etc
			["head"] = new List<string>(),
			["misc"] = new List<string>()
		};
		private static string Class;
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

		public static void Add(string Item, int Line, string Class = "") { // Called on "Item" key
			string OriginalItemName = Item;

			// Check item exists
			if (!ItemDatabase.Exists(OriginalItemName)) {
				if (Program.Config.ReadBool("bool_warn_invalid_item_name")) {
					Warning.Write("{f:Yellow}Invalid{r} TF2 {f:Yellow}Item{r} Name: '{f:Yellow}{$0}{r}'", Line, 209, Item);
				}
				return;
			}
			Item = ItemDatabase.GetName(Item);

			// Check item equippable by class
			if (!string.IsNullOrEmpty(Class) && !IsEquippable(Item, Class) && Program.Config.ReadBool("bool_warn_tfbot_unequippable_item")) {
				Warning.Write("TFBot {f:Yellow}Class{r} <{f:Yellow}{$0}{r}> cannot equip {f:Yellow}item{r}: '{f:Yellow}{$1}{r}'", Line, 218, Class, OriginalItemName);
			}

			// Add item to slot
			string ItemSlot = ItemDatabase.GetSlot(Item).ToLower();
			if (concat_slots.Contains(ItemSlot)) {
				Slot[ItemSlot].Add(Item);
			}
			else {
				Slot[ItemSlot] = Item;
			}
		}

		public static void Clear() {
			Slot = new Dictionary<string, dynamic> {
				["primary"] = "",
				["secondary"] = "",
				["melee"] = "",
				["pda"] = "",
				["pda2"] = "",
				["building"] = "",
				["action"] = new List<string>(),
				["head"] = new List<string>(),
				["misc"] = new List<string>()
			};
			ModifierKeys = new Dictionary<string, int>();
			Class = "";
		}

		public static void StoreTemplateAndClear(string TemplateName) {
			ItemConfiguration Config = new ItemConfiguration {
				ModifierKeys = ModifierKeys,
				Slot = Slot
			};
			Templates[TemplateName] = Config;
			Clear();
		}

		public static void ImportTemplate(string TemplateName) {
			ItemConfiguration Template;
			try {
				Template = Templates[TemplateName];
			}
			catch {
				return;
			}

			// ModifierKeys
			/*foreach(string Key in Template.ModifierKeys.Keys) { // Disabled, causes weird false positives
				if (!ModifierKeys.Keys.Contains(Key)) { // Collisions are avoided, same key is updated to latest value (higher line number)
					ModifierKeys[Key] = Template.ModifierKeys[Key];
				}
			}*/

			// Slots
			string[] DefaultItems = { };
			if (!string.IsNullOrEmpty(Template.Class)) {
				DefaultItems = default_loadout[Template.Class.ToUpper()];
			}
			foreach (string SlotName in Template.Slot.Keys) {
				switch (Slot[SlotName].GetType().ToString()) {
					case "System.String": {// primary, secondary, melee, pda, pda2, building
						string TemplateItem = Template.Slot[SlotName];
						if (!string.IsNullOrEmpty(TemplateItem) && !DefaultItems.ToList().Contains(TemplateItem)) { // Template has an item in the slot && that item is not a default item
							Slot[SlotName] = TemplateItem;
						}
						break;
					}
					case "System.Collections.Generic.List`1[System.String]": { // action, head, misc
						List<string> TemplateList = Template.Slot[SlotName];
						foreach (string ItemName in TemplateList) {
							if (!Slot[SlotName].Contains(ItemName)) {
								Slot[SlotName].Add(ItemName);
							}
						}
						break;
					}
				}
			}
		}

		public static void SetupClass(string Class) {
			ItemTracker.Class = Class;
			foreach (string StockItem in default_loadout[Class.ToUpper()]) {
				string StockItemSlot = ItemDatabase.GetSlot(StockItem);
				if (Slot[StockItemSlot].Length == 0) {
					Slot[StockItemSlot] = StockItem;
				}
			}
		}

		public static bool IsEquippable(string ItemName, string Class) {
			return ItemDatabase.GetSlot(ItemName, Class) != "0";
		}

		// Check if an item is in the inventory.
		public static bool IsEquipped(string item) {
			if (!ItemDatabase.Exists(item)) {
				return true; // Assume true, warning handled elsewhere
			}
			string ItemSlot = ItemDatabase.GetSlot(item);
			if (concat_slots.Contains(ItemSlot)) {
				foreach(string Equipped in Slot[ItemSlot]) {
					if (Equipped.ToUpper() == item.ToUpper()) {
						return true;
					}
				}
				return false;
			}
			else {
				return Regex.IsMatch(Slot[ItemSlot], item, RegexOptions.IgnoreCase);
			}
		}

		public static void AddModifier(string ItemName, int Line) { // Called on "ItemName" key
			if (!ItemDatabase.Exists(ItemName)) {
				if (Program.Config.ReadBool("bool_warn_invalid_item_name")) {
					Warning.Write("{f:Yellow}Invalid{r} TF2 {f:Yellow}Item{r} Name: '{f:Yellow}{$0}{r}'", Line, 209, ItemName);
				}
				return;
			}
			ModifierKeys[ItemName] = Line;
		}

		public static void VerifyModifications() {
			foreach(string ItemName in ModifierKeys.Keys) {
				if (!IsEquipped(ItemName) && Program.Config.ReadBool("bool_warn_tfbot_missing_item")) {
					Warning.Write("{f:Yellow}TFBot{r} does not have {f:Yellow}item{r}: '{f:Yellow}{$0}{r}'", ModifierKeys[ItemName], 210, ItemName);
				}
			}
		}

		public static string[] Inventory() {
			List<string> ret = new List<string>();
			
			// Add single slots
			foreach (string slot_key in single_slots) {
				if (Slot[slot_key].Length > 0) {
					ret.Add(Slot[slot_key]);
				}
			}

			// Add concat slots
			foreach (string slot_key in concat_slots) {
				foreach(Match m in Regex.Matches(Slot[slot_key], "(\"([^\"]*)\")")) {
					string item = Regex.Replace(m.Value, "\"", "");
					ret.Add(item);
				}
			}
			return ret.ToArray();
		}

	}
}
