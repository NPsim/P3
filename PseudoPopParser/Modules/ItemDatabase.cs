using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {

	public class Item {
		public string Name;
		public string Localization;
		public string DefaultSlot;
		public string[] Slot = { "0", "0", "0", "0", "0", "0", "0", "0", "0" };
		public Dictionary<string, string> Attributes = new Dictionary<string, string>();
		public Item() { }
	}

	internal class ItemDatabase {

		private static Dictionary<string, Item> Items = new Dictionary<string, Item>();
		private static readonly string Path = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\ItemDB.ffd";
		private static readonly string[] UselessAttributes = {
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
			if (!File.Exists(Path)) {
				Error.WriteNoIncrement("Could not find local database.", -1, 997);
				return;
			}

			string[] RawDBLines = File.ReadAllLines(Path);
			PrintColor.InfoLine("ItemDB Version: {f:Cyan}{$0}{r}", RawDBLines[0]);
			Item ItemEntry = new Item();
			for (int i = 1; i < RawDBLines.Count(); i++) {
				string Line = RawDBLines[i];
				if (Regex.IsMatch(Line, @"^\S")) {
					if (!string.IsNullOrEmpty(ItemEntry.Name)) {
						Items[ItemEntry.Name] = ItemEntry;
					}
					ItemEntry = new Item {
						Name = Line
					};
				}
				else {
					MatchCollection Token = Regex.Matches(Line, "\"[^\"]*\"|[^\"\\s]\\S*[^\"\\s]|[^\"\\s]");
					switch (Token[0].Value.ToUpper().Trim('"')) {
						case "LOC":
							ItemEntry.Localization = Token[1].Value;
							break;
						case "DFS":
							ItemEntry.DefaultSlot = Token[1].Value;
							break;
						case "SLT": {
							if (Token[1].Value == "ALLCLASS") {
								for (int ClassIndex = 0; ClassIndex < 9; ClassIndex++) {
									ItemEntry.Slot[ClassIndex] = ItemEntry.DefaultSlot;
								}
							}
							else {
								for (int ClassIndex = 0; ClassIndex < 9; ClassIndex++) {
									if (Token[ClassIndex + 1].Value.Trim('"') == "1") {
										ItemEntry.Slot[ClassIndex] = ItemEntry.DefaultSlot;
									}
									else {
										ItemEntry.Slot[ClassIndex] = Token[ClassIndex + 1].Value;
									}
								}
							}
							break;
						}
						default:
							ItemEntry.Attributes[Token[0].Value.Trim('"')] = Token[1].Value.Trim('"');
							break;
					}
				}
			}
		}

		public static bool Exists(string item) {
			foreach (string entry in Items.Keys) {
				if (entry.ToUpper() == item.ToUpper()) {
					return true;
				}
			}
			return false;
		}

		public static string GetName(string item_name) {
			foreach (string real_name in Items.Keys) {
				if (item_name.ToUpper() == real_name.ToUpper()) {
					return real_name;
				}
			}
			throw new Exception("InvalidNormalItemName");
		}

		public static string GetSlot(string ItemName, string Class = "") {
			ItemName = GetName(ItemName);
			switch(Class.ToUpper()) {
				case "SCOUT":
					return Items[ItemName].Slot[0];
				case "SOLDIER":
					return Items[ItemName].Slot[1];
				case "PYRO":
					return Items[ItemName].Slot[2];
				case "DEMOMAN":
					return Items[ItemName].Slot[3];
				case "HEAVY":
					return Items[ItemName].Slot[4];
				case "ENGINEER":
					return Items[ItemName].Slot[5];
				case "MEDIC":
					return Items[ItemName].Slot[6];
				case "SNIPER":
					return Items[ItemName].Slot[7];
				case "SPY":
					return Items[ItemName].Slot[8];
				default:
					return Items[ItemName].DefaultSlot; // Possible Values: "", "primary", "secondary", "melee", "pda", "pda2", "building", "action", "head", "misc"
			}
		}

		public static string GetLocalization(string ItemName) {
			return Items[ItemName].Localization;
		}

		public static Item GetItem(string ItemName) {
			return Items[ItemName];
		}

		public static Dictionary<string, string> Attributes(string ItemName) {
			Dictionary<string, string> Attributes = Items[ItemName].Attributes;
			foreach (string Key in UselessAttributes) {
				Attributes.Remove(Key);
			}
			return Attributes;
		}

		public static List<string> List {
			get {
				return Items.Keys.ToList();
			}
		}

		public static string[] Array {
			get {
				return Items.Keys.ToArray();
			}
		}
	}
}
