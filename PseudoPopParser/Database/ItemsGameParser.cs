using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PseudoPopParser;
using System.Security.Cryptography;
using System.Text;

namespace PseudoPopParser.Database {

	class ItemsGameParser : IDisposable {

		private readonly StreamReader Reader;
		private readonly StringBuilder Builder = new StringBuilder();
		private readonly SQLHandler SQL = new SQLHandler();
		public VDFTree ItemsGame { get; private set; } = new VDFTree();
		public Dictionary<string, Prefab> PrefabsTable { get; private set; } = new Dictionary<string, Prefab>(); // string is primary key
		public HashSet<Item> ItemsSet { get; private set; } = new HashSet<Item>();
		public HashSet<Attribute> AttributesSet { get; private set; } = new HashSet<Attribute>();

		public ItemsGameParser(string itemsGameFilePath) {
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			this.Reader = new StreamReader(itemsGameFilePath);
			Parse();

			BuildPrefabs();
			BuildItems();
			BuildAttributes();

			SQL.BeginTransaction();
			InsertItems();
			InsertAttributes();
			SQL.EndTransaction();

			sw.Stop();
			Console.WriteLine(sw.ElapsedMilliseconds);

			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
			Test();
		}

		public void Dispose() { // Implemented by IDisposable
			this.Reader.Dispose();
			this.SQL.Dispose();
		}

		public void Test() {
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			SQL.BeginTransaction();
			//var idict = SQL.GetItemDictionary();
			var adict = SQL.GetAttributeDictionary();
			SQL.EndTransaction();

			sw.Stop();
			Console.WriteLine(sw.ElapsedMilliseconds);
		}

		private string ReadToNextMatch(string pattern) {
			Match match = null;
			do {
				if (Reader.EndOfStream) break;
				Builder.Append(Reader.ReadLine());
				match = Regex.Match(Builder.ToString(), pattern);
			}
			while (!match.Success);
			Builder.Remove(0, match.Index + match.Length);
			return match.ToString();
		}

		private void Parse() { // https://regex101.com/r/QYfFLf/1/
			do {
				string keyPair = ReadToNextMatch("(\"[^\"]*\"\\s*\"[^\"]*\")|(\"[^\"]*\"\\s*{)|(})");
				MatchCollection tokens = Regex.Matches(keyPair, "(\"([^\"]*)\")|({)|(})");
				if (tokens[0].ToString() == "}") { // Close Block
					ItemsGame.StepOut();
				}
				else if (tokens[1].ToString() == "{") { // Value is Block
					ItemsGame.StepBlock(tokens[0].ToString().Trim('"'));
				}
				else if (tokens[1].ToString().First() == '"') { // Value is String
					ItemsGame.AddKeyValue(tokens[0].ToString().Trim('"'), tokens[1].ToString().Trim('"'));
				}
				else { // Default; should never get here
					throw new Exception("VDF object case not found");
				}
			}
			while (!Reader.EndOfStream);
		}

		private void BuildPrefabs() {
			List<VDFNode> tfPrefabs = this.ItemsGame.Head.GetValueFromKey("prefabs");

			foreach (VDFNode entry in tfPrefabs) {
				Prefab prefab = new Prefab(entry.Key);

				// Accummulate Prefabs (just store for later, don't do anything with them yet)
				if (entry.KeyExists("prefab")) {
					string prefabValue = entry.GetValueFromKey("prefab");
					string[] inheritList = prefabValue.Split(' ');
					foreach (string prefabName in inheritList) {
						prefab.AddPrefab(prefabName);
					}
				}

				// Localization
				if (entry.KeyExists("item_name")) {
					prefab.LocalizedName = entry.GetValueFromKey("item_name");
				}
				if (entry.KeyExists("item_description")) {
					prefab.LocalizedDescription = entry.GetValueFromKey("item_description");
				}

				// Attributes
				if (entry.KeyExists("attributes")) { // Standard attributes
					List<VDFNode> attributesList = entry.GetValueFromKey("attributes");
					foreach (VDFNode attributeNode in attributesList) {
						prefab.AddAttribute(attributeNode.Key, attributeNode.GetValueFromKey("value"));
					}
				}
				if (entry.KeyExists("static_attrs")) { // Static attributes
					List<VDFNode> staticAttributesList = entry.GetValueFromKey("static_attrs");
					foreach (VDFNode attributeNode in staticAttributesList) {
						prefab.AddAttribute(attributeNode.Key, attributeNode.Value);
					}
				}

				// Slots
				if (entry.KeyExists("item_slot")) { // Default slot definition
					InventorySlot inventorySlot = InventorySlotHelper.Cast(entry.GetValueFromKey("item_slot"));
					prefab.DefaultSlot = inventorySlot;
				}
				if (entry.KeyExists("used_by_classes")) { // Direct slot definition
					List<VDFNode> classesList = entry.GetValueFromKey("used_by_classes");
					foreach (VDFNode classNode in classesList) {
						PlayerClass classType = PlayerClassHelper.Cast(classNode.Key);
						InventorySlot inventorySlot;
						if (classNode.Value == "1") {
							inventorySlot = InventorySlot.DEFAULT;
						}
						else {
							inventorySlot = InventorySlotHelper.Cast(classNode.Value);
						}
						prefab.AddSlot(classType, inventorySlot);
					}
				}

				// Equip region
				if (entry.KeyExists("equip_region")) {
					if (entry.GetNode("equip_region").Value is string) { // Single region
						prefab.AddEquipRegion(entry.GetValueFromKey("equip_region"));
					}
					else if (entry.GetNode("equip_region").Value is List<VDFNode>) { // Ambiguous multiple region (Valve error?)
						List<VDFNode> regions = entry.GetValueFromKey("equip_region");
						foreach (VDFNode equipRegion in regions) {
							if (equipRegion.Value == "1") {
								prefab.AddEquipRegion(equipRegion.Key);
							}
						}
					}
					else {
						throw new Exception("prefab equip_region type not found");
					}
				}
				if (entry.KeyExists("equip_regions")) { // Multiple region
					List<VDFNode> regions = entry.GetValueFromKey("equip_regions");
					foreach (VDFNode equipRegion in regions) {
						if (equipRegion.Value == "1") {
							prefab.AddEquipRegion(equipRegion.Key);
						}
					}
				}

				// Add to table
				PrefabsTable.Add(prefab.Name, prefab);
			}
		}

		private void BuildItems() {
			List<VDFNode> tfItems = this.ItemsGame.Head.GetValueFromKey("items");

			foreach (VDFNode entry in tfItems) {
				string itemName = entry.GetValueFromKey("name");
				int itemID = itemName == "default" ? -1 : int.Parse(entry.Key); // The only item without a valid int ID is "default"
				Item item = new Item(itemID, itemName);

				// Import Prefabs
				if (entry.KeyExists("prefab")) {
					string prefabValue = entry.GetValueFromKey("prefab");
					string[] inheritList = prefabValue.Split(' ');
					foreach (string prefabName in inheritList) {
						item.ImportPrefab(this.PrefabsTable, prefabName);
					}
				}

				// Localization
				if (entry.KeyExists("item_name")) {
					item.LocalizedName = entry.GetValueFromKey("item_name");
				}
				if (entry.KeyExists("item_description")) {
					item.LocalizedDescription = entry.GetValueFromKey("item_description");
				}

				// Attributes
				if (entry.KeyExists("attributes")) { // Standard attributes
					List<VDFNode> attributesList = entry.GetValueFromKey("attributes");
					foreach (VDFNode attributeNode in attributesList) {
						item.AddAttribute(attributeNode.Key, attributeNode.GetValueFromKey("value"));
					}
				}
				if (entry.KeyExists("static_attrs")) { // Static attributes
					List<VDFNode> staticAttributesList = entry.GetValueFromKey("static_attrs");
					foreach (VDFNode attributeNode in staticAttributesList) {
						item.AddAttribute(attributeNode.Key, attributeNode.Value);
					}
				}

				// Slots
				if (entry.KeyExists("item_slot") && ((string)entry.GetValueFromKey("item_slot")).Length > 0) { // Default slot definition
					InventorySlot inventorySlot = InventorySlotHelper.Cast(entry.GetValueFromKey("item_slot"));

					item.DefaultSlot = inventorySlot;
				}
				if (entry.KeyExists("used_by_classes")) { // Direct slot definition
					List<VDFNode> classesList = entry.GetValueFromKey("used_by_classes");
					foreach (VDFNode classNode in classesList) {
						PlayerClass classType = PlayerClassHelper.Cast(classNode.Key);
						InventorySlot inventorySlot;
						if (classNode.Value == "1") {
							inventorySlot = InventorySlot.DEFAULT;
						}
						else {
							inventorySlot = InventorySlotHelper.Cast(classNode.Value);
						}
						item.AddSlot(classType, inventorySlot);
					}
				}

				// Equip region
				if (entry.KeyExists("equip_region")) {
					if (entry.GetNode("equip_region").Value is string) { // Single region
						item.AddEquipRegion(entry.GetValueFromKey("equip_region"));
					}
					else if (entry.GetNode("equip_region").Value is List<VDFNode>) { // Ambiguous multiple region (Valve error?)
						List<VDFNode> regions = entry.GetValueFromKey("equip_region");
						foreach (VDFNode equipRegion in regions) {
							if (equipRegion.Value == "1") {
								item.AddEquipRegion(equipRegion.Key);
							}
						}
					}
					else {
						throw new Exception("item equip_region type not found");
					}
				}
				if (entry.KeyExists("equip_regions")) { // Multiple region
					List<VDFNode> regions = entry.GetValueFromKey("equip_regions");
					foreach (VDFNode equipRegion in regions) {
						if (equipRegion.Value == "1") {
							item.AddEquipRegion(equipRegion.Key);
						}
					}
				}

				// Add to table
				ItemsSet.Add(item);
			}
		}

		private void BuildAttributes() {
			List<VDFNode> tfAttributes = this.ItemsGame.Head.GetValueFromKey("attributes");

			foreach (VDFNode entry in tfAttributes) {
				Attribute attribute = new Attribute(entry.GetValueFromKey("name"));

				// Localized Description String
				if (entry.KeyExists("description_string")) {
					attribute.LocalizedDescription = entry.GetValueFromKey("description_string");
				}

				// Attribute Format (additive, percentage, inverted_percentage, ...)
				if (entry.KeyExists("description_format")) {
					attribute.Format = entry.GetValueFromKey("description_format");
				}

				// Value Type
				if (entry.KeyExists("attribute_type")) {
					attribute.ValueType = entry.GetValueFromKey("attribute_type");
				}

				// Effect Type
				if (entry.KeyExists("effect_type")) {
					attribute.EffectType = entry.GetValueFromKey("effect_type");
				}

				AttributesSet.Add(attribute);
			}
		}

		private void InsertItems() {
			foreach (Item item in ItemsSet) {
				this.SQL.InsertItem(item);
			}
		}

		private void InsertAttributes() {
			foreach (Attribute attribute in AttributesSet) {
				this.SQL.InsertAttribute(attribute);
			}
		}
	}
}