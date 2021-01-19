using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser.Database {

	public class Prefab {
		public override string ToString() => this.GetType().Name + ":" + this.Name;

		public string Name { get; set; }
		public string LocalizedName { get; set; }
		public string LocalizedDescription { get; set; }
		public InventorySlot? DefaultSlot { get; set; }

		protected HashSet<string> Prefabs = new HashSet<string>();
		protected readonly Dictionary<PlayerClass, InventorySlot?> Slots = new Dictionary<PlayerClass, InventorySlot?>();
		protected readonly Dictionary<string, string> Attributes = new Dictionary<string, string>();
		protected readonly HashSet<string> EquipRegions = new HashSet<string>();

		public Prefab(string name) {
			this.Name = name;
		}

		public PlayerClass[] GetClasses() => Slots.Keys.ToArray();
		public Dictionary<PlayerClass, InventorySlot?> GetSlots() => new Dictionary<PlayerClass, InventorySlot?>(Slots);
		public string[] GetPrefabs() => Prefabs.ToArray();
		public Dictionary<string, string> GetAttributes() => new Dictionary<string, string>(Attributes);
		public string[] GetEquipRegions() => EquipRegions.ToArray();

		public void AddSlot(PlayerClass playerClass, InventorySlot? slot) => Slots[playerClass] = slot;
		public void AddPrefab(string name) => Prefabs.Add(name);
		public void AddAttribute(string attribute, string value) => Attributes[attribute] = value;
		public void AddEquipRegion(string region) => EquipRegions.Add(region);

	}
}
