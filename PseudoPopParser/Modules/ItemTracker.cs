using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PseudoPopParser {

	internal class ItemTracker {

		public static string[] DefaultLoadout(string class_name) {
			if (Regex.IsMatch(class_name, "SCOUT", RegexOptions.IgnoreCase)) {
				return new string[] { "TF_WEAPON_SCATTERGUN", "TF_WEAPON_PISTOL_SCOUT", "TF_WEAPON_BAT" };
			}
			else if (Regex.IsMatch(class_name, "SOLDIER", RegexOptions.IgnoreCase)) {
				return new string[] { "TF_WEAPON_ROCKETLAUNCHER", "TF_WEAPON_SHOTGUN_SOLDIER", "TF_WEAPON_SHOVEL" };
			}
			else if (Regex.IsMatch(class_name, "PYRO", RegexOptions.IgnoreCase)) {
				return new string[] { "TF_WEAPON_FLAMETHROWER", "TF_WEAPON_SHOTGUN_PYRO", "TF_WEAPON_FIREAXE" };
			}
			else if (Regex.IsMatch(class_name, "DEMOMAN", RegexOptions.IgnoreCase)) {
				return new string[] { "TF_WEAPON_GRENADELAUNCHER", "TF_WEAPON_PIPEBOMBLAUNCHER", "TF_WEAPON_BOTTLE" };
			}
			else if (Regex.IsMatch(class_name, "HEAVY", RegexOptions.IgnoreCase)) {
				return new string[] { "TF_WEAPON_MINIGUN", "TF_WEAPON_SHOTGUN_HWG", "TF_WEAPON_FISTS" };
			}
			else if (Regex.IsMatch(class_name, "ENGINEER", RegexOptions.IgnoreCase)) {
				return new string[] { "TF_WEAPON_SHOTGUN_PRIMARY", "TF_WEAPON_WEAPON_PISTOL", "TF_WEAPON_WRENCH", "TF_WEAPON_PDA_ENGINEER_BUILD", "TF_WEAPON_PDA_ENGINEER_DESTROY" };
			}
			else if (Regex.IsMatch(class_name, "MEDIC", RegexOptions.IgnoreCase)) {
				return new string[] { "TF_WEAPON_SYRINGEGUN_MEDIC", "TF_WEAPON_MEDIGUN", "TF_WEAPON_BONESAW" };
			}
			else if (Regex.IsMatch(class_name, "SNIPER", RegexOptions.IgnoreCase)) {
				return new string[] { "TF_WEAPON_SNIPERRIFLE", "TF_WEAPON_SMG", "TF_WEAPON_CLUB" };
			}
			else if (Regex.IsMatch(class_name, "SPY", RegexOptions.IgnoreCase)) {
				return new string[] { "TF_WEAPON_REVOLVER", "TF_WEAPON_KNIFE", "TF_WEAPON_BUILDER_SPY", "TF_WEAPON_INVIS", "TF_WEAPON_PDA_SPY" };
			}
			else {
				throw new Exception("ClassValueNotFound");
			}
		}

		private static List<string> inventory = new List<string>();

		public static void AddItem(string item) {
			inventory.Add(item);
			if (!ItemDatabase.Exists(item)) {
				PrintColor.Colorf("{b:Red}IT{r}");
				Warning.ItemInvalid(Program.CurrentLineNumber, item);
			}
		}

		public static void Clear() {
			inventory.Clear();
		}

		public static void Reset(string class_name) {

		}

		public static bool IsEquipped(string item) {
			return inventory.Contains(item);
		}

	}
}
