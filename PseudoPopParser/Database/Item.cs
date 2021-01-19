using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser.Database {

	public class Item : Prefab {

		public int ID { get; private set; }
		public List<string> PrefabImportSequence { get; private set; } = new List<string>(); // Contains every prefab in import order from deepest to most recent

		public Item(int id, string name) : base(name) {
			this.ID = id;
			this.Prefabs = null; // We don't care about an item's local prefabs
		}

		public void ImportPrefab(Dictionary<string, Prefab> prefabTable, string prefabName) {
			RecursiveGeneratePrefabImportSequence(prefabTable, prefabName);
			foreach (string importSequenceName in this.PrefabImportSequence) {
				MergePrefabHelper(prefabTable[importSequenceName]);
			}
		}

		private void RecursiveGeneratePrefabImportSequence(Dictionary<string, Prefab> prefabTable, string prefabName) {
			if (prefabTable[prefabName].GetPrefabs().Length > 0) {
				foreach (string deepPrefabName in prefabTable[prefabName].GetPrefabs()) {
					RecursiveGeneratePrefabImportSequence(prefabTable, deepPrefabName);
				}
			}
			this.PrefabImportSequence.Add(prefabName);
		}

		private void MergePrefabHelper(Prefab remote) {
			// Default Slot	
			this.DefaultSlot = remote.DefaultSlot != null ? remote.DefaultSlot : this.DefaultSlot;

			// Localized Name
			this.LocalizedName = remote.LocalizedName != null ? remote.LocalizedName : this.LocalizedName;

			// Localized Description
			this.LocalizedDescription = remote.LocalizedDescription != null ? remote.LocalizedDescription : this.LocalizedDescription;

			// Default Slot
			this.DefaultSlot = remote.DefaultSlot != null ? remote.DefaultSlot : this.DefaultSlot;

			// Specific Slots
			Dictionary<PlayerClass, InventorySlot?> remoteSlots = remote.GetSlots();
			foreach (PlayerClass playerClass in remoteSlots.Keys) {
				this.Slots[playerClass] = remoteSlots[playerClass];
			}

			// Attributes
			Dictionary<string, string> remoteAttributes = remote.GetAttributes();
			foreach (string remoteAttribute in remoteAttributes.Keys) {
				this.Attributes[remoteAttribute] = remoteAttributes[remoteAttribute];
			}

			// Equip Regions
			foreach (string remoteEquipRegion in remote.GetEquipRegions()) {
				this.EquipRegions.Add(remoteEquipRegion); // Does not add to set if key already present.
			}
		}
	}
}
