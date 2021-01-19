using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PseudoPopParser.Database {

	class SQLHandler : IDisposable {

		private static readonly string DefaultSQLDatabasePath = AppDomain.CurrentDomain.BaseDirectory + @"\Database\ItemsGame.db";
		private static readonly DBNull Null = DBNull.Value;
		private readonly SqliteConnection Connection;

		public SQLHandler() : this(DefaultSQLDatabasePath) { }
		
		public SQLHandler(string sqlFilePath) {
			this.Connection = new SqliteConnection("Data Source=" + sqlFilePath);
			this.Connection.Open();
		}

		public void Dispose() { // Implemented by IDisposable
			this.Connection.Dispose();
		}

		public void BeginTransaction() {
			SqliteCommand Command = new SqliteCommand("Begin", this.Connection);
			Command.ExecuteNonQuery();
		}

		public void EndTransaction() {
			SqliteCommand Command = new SqliteCommand("End", this.Connection);
			Command.ExecuteNonQuery();
		}

		public Dictionary<string, Item> GetItemDictionary() {
			Dictionary<string, Item> items = new Dictionary<string, Item>();
			SqliteCommand command = new SqliteCommand("SELECT * FROM items", this.Connection);
			SqliteDataReader reader = command.ExecuteReader();

			// Columns: item_id, item_name, localized_name, localized_desc, default_slot, slots, equip_regions, attributes
			if (reader.HasRows) {
				while (reader.Read()) {
					Item item = new Item(Convert.ToInt32(reader["item_id"]), reader["item_name"].ToString());
					item.LocalizedName = reader["localized_name"].ToString();
					item.LocalizedDescription = reader["localized_desc"].ToString();
					item.DefaultSlot = InventorySlotHelper.Cast(reader["default_slot"].ToString());
					if (reader["slots"] != SQLHandler.Null) {
						MatchCollection matches = Regex.Matches(reader["slots"].ToString(), "\"([^\"]+)\":\"([^\"]+)\",");
						foreach (Match slotPair in matches) {
							item.AddSlot(
								PlayerClassHelper.Cast(slotPair.Groups[1].ToString()),
								InventorySlotHelper.Cast(slotPair.Groups[2].ToString())
							);
						}
					}
					if (reader["equip_regions"] != SQLHandler.Null) {
						MatchCollection matches = Regex.Matches(reader["equip_regions"].ToString(), "\"([^\"]+)\",");
						foreach (Match equipRegion in matches) {
							item.AddEquipRegion(equipRegion.Groups[1].ToString());
						}
					}
					if (reader["attributes"] != SQLHandler.Null) {
						MatchCollection matches = Regex.Matches(reader["attributes"].ToString(), "\"([^\"]+)\":\"([^\"]+)\",");
						foreach (Match attributePair in matches) {
							item.AddAttribute(
								attributePair.Groups[1].ToString(),
								attributePair.Groups[2].ToString()
							);
						}
					}
					items.Add(item.Name, item);
				}
			}
			reader.Close();
			return items;
		}

		public Dictionary<string, Attribute> GetAttributeDictionary() {
			Dictionary<string, Attribute> attributes = new Dictionary<string, Attribute>();
			SqliteCommand command = new SqliteCommand("SELECT * FROM attributes", this.Connection);
			SqliteDataReader reader = command.ExecuteReader();

			// Columns: attribute_name, localized_desc, format, value_type, effect_type
			if (reader.HasRows) {
				while (reader.Read()) {
					Attribute attribute = new Attribute();
					attribute.Name = reader["attribute_name"].ToString();
					attribute.LocalizedDescription = reader["localized_desc"].ToString();
					attribute.Format = reader["format"].ToString();
					attribute.ValueType = reader["value_type"].ToString();
					attribute.EffectType = reader["effect_type"].ToString();
					attributes.Add(attribute.Name, attribute);
				}
			}
			reader.Close();
			return attributes;
		}

		public void InsertItem(Item item) {
			const string commandText =
				"INSERT INTO items (item_id, item_name, localized_name, localized_desc, default_slot, slots, equip_regions, attributes)" +
				"VALUES (@id, @name, @loc_name, @loc_desc, @default_slot, @slots, @equip_regions, @attributes)";
			SqliteCommand Command = new SqliteCommand(commandText, this.Connection);

			// Item ID
			Command.Parameters.AddWithValue("@id", item.ID);

			// Item Name
			if (!string.IsNullOrEmpty(item.Name)) {
				Command.Parameters.AddWithValue("@name", item.Name);
			} else Command.Parameters.AddWithValue("@name", SQLHandler.Null);

			// Localized Name
			if (!string.IsNullOrEmpty(item.LocalizedName)) {
				Command.Parameters.AddWithValue("@loc_name", item.LocalizedName);
			} else Command.Parameters.AddWithValue("@loc_name", SQLHandler.Null);

			// Localized Description
			if (!string.IsNullOrEmpty(item.LocalizedDescription)) {
				Command.Parameters.AddWithValue("@loc_desc", item.LocalizedDescription);
			} else Command.Parameters.AddWithValue("@loc_desc", SQLHandler.Null);

			// Default Slot
			if (item.DefaultSlot != null) {
				Command.Parameters.AddWithValue("@default_slot", item.DefaultSlot.ToString());
			}
			else Command.Parameters.AddWithValue("@default_slot", SQLHandler.Null);

			// Attribute : Value (serialized)
			Dictionary<string, string> attributes = item.GetAttributes();
			if (attributes.Count > 0) {
				StringBuilder builder = new StringBuilder();
				builder.Append('{');
				foreach (string key in attributes.Keys) {
					builder.Append(string.Format("\"{0}\":\"{1}\",", key, attributes[key])); // "key1":"value1",
				}
				builder.Append('}');
				Command.Parameters.AddWithValue("@attributes", builder.ToString());
			} else Command.Parameters.AddWithValue("@attributes", SQLHandler.Null);

			// Class : Slot (serialized)
			Dictionary<PlayerClass, InventorySlot?> slots = item.GetSlots();
			if (slots.Count > 0) {
				StringBuilder builder = new StringBuilder();
				builder.Append('{');
				foreach (PlayerClass key in slots.Keys) {
					builder.Append(string.Format("\"{0}\":\"{1}\",", key.ToString(), slots[key].ToString())); // "key1":"value1",
				}
				builder.Append('}');
				Command.Parameters.AddWithValue("@slots", builder.ToString());
			} else Command.Parameters.AddWithValue("@slots", SQLHandler.Null);

			// Equip Region (serialized)
			string[] equipRegions = item.GetEquipRegions();
			if (equipRegions.Length > 0) {
				StringBuilder builder = new StringBuilder();
				builder.Append('{');
				foreach (string region in equipRegions) {
					builder.Append(string.Format("\"{0}\",", region)); // "key1":"value1",
				}
				builder.Append('}');
				Command.Parameters.AddWithValue("@equip_regions", builder.ToString());
			} else Command.Parameters.AddWithValue("@equip_regions", SQLHandler.Null);

			Command.ExecuteNonQuery();
		}

		public void InsertAttribute(Attribute attribute) {
			const string commandText =
				"INSERT INTO attributes (attribute_name, localized_desc, format, value_type, effect_type)" +
				"VALUES (@attribute_name, @localized_desc, @format, @value_type, @effect_type)";
			SqliteCommand Command = new SqliteCommand(commandText, this.Connection);

			// Attribute Name
			Command.Parameters.AddWithValue("@attribute_name", attribute.Name);

			// Localized Description String
			if (!string.IsNullOrEmpty(attribute.LocalizedDescription) && attribute.LocalizedDescription[0] == '#') {
				Command.Parameters.AddWithValue("@localized_desc", attribute.LocalizedDescription);
			}
			else Command.Parameters.AddWithValue("@localized_desc", SQLHandler.Null);

			// Attribute Format (additive, percentage, inverted_percentage, ...)
			if (!string.IsNullOrEmpty(attribute.Format)) {
				Command.Parameters.AddWithValue("@format", attribute.Format);
			}
			else Command.Parameters.AddWithValue("@format", SQLHandler.Null);

			// Value Type
			if (!string.IsNullOrEmpty(attribute.ValueType)) {
				Command.Parameters.AddWithValue("@value_type", attribute.ValueType);
			}
			else Command.Parameters.AddWithValue("@value_type", SQLHandler.Null);

			// Effect Type
			if (!string.IsNullOrEmpty(attribute.EffectType)) {
				Command.Parameters.AddWithValue("@effect_type", attribute.EffectType);
			}
			else Command.Parameters.AddWithValue("@effect_type", SQLHandler.Null);

			Command.ExecuteNonQuery();
		}
	}
}
