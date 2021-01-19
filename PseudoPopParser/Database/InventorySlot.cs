namespace PseudoPopParser {
	public enum InventorySlot {
		//NULL,
		PRIMARY,    // Equippable
		SECONDARY,  // Equippable
		MELEE,      // Equippable
		PDA,        // Equippable
		PDA2,       // Equippable
		BUILDING,   // Equippable
		ACTION,
		TAUNT,
		HEAD,
		MISC,
		QUEST,      // Used for prefab (dynamic_quest_base)
		UTILITY,    // Used for PassTime (TF_WEAPON_PASSTIME_GUN)
		DEFAULT     // Only used for block "used_by_classes" to indicate pointer to Prefab.DefaultSlot; denoted by "scout" "1" in items_game.txt
	}

	// Direct switch map is faster than Enum.Parse()
	public static class InventorySlotHelper {
		public static InventorySlot? Cast(string slot) {
			switch(slot.ToUpper()) {
				case "PRIMARY": return InventorySlot.PRIMARY;
				case "SECONDARY": return InventorySlot.SECONDARY;
				case "MELEE": return InventorySlot.MELEE;
				case "PDA": return InventorySlot.PDA;
				case "PDA2": return InventorySlot.PDA2;
				case "BUILDING": return InventorySlot.BUILDING;
				case "ACTION": return InventorySlot.ACTION;
				case "TAUNT": return InventorySlot.TAUNT;
				case "HEAD": return InventorySlot.HEAD;
				case "MISC": return InventorySlot.MISC;
				case "QUEST": return InventorySlot.QUEST;
				case "UTILITY": return InventorySlot.UTILITY;
				case "DEFAULT": return InventorySlot.DEFAULT;
				default: return null;
			}
		}
	}
}