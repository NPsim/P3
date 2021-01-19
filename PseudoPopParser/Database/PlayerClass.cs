namespace PseudoPopParser {
	public enum PlayerClass {
		//NULL,
		SCOUT,
		SOLDIER,
		PYRO,
		DEMOMAN,
		HEAVY,
		MEDIC,
		SNIPER,
		ENGINEER,
		SPY
	}

	// Direct switch map is faster than Enum.Parse()
	public static class PlayerClassHelper {
		public static PlayerClass Cast(string slot) {
			switch (slot.ToUpper()) {
				case "SCOUT": return PlayerClass.SCOUT;
				case "SOLDIER": return PlayerClass.SOLDIER;
				case "PYRO": return PlayerClass.PYRO;
				case "DEMOMAN": return PlayerClass.DEMOMAN;
				case "HEAVY": return PlayerClass.HEAVY;
				case "MEDIC": return PlayerClass.MEDIC;
				case "SNIPER": return PlayerClass.SNIPER;
				case "ENGINEER": return PlayerClass.ENGINEER;
				case "SPY": return PlayerClass.SPY;
				default: throw new System.InvalidCastException();
			}
		}
	}
}