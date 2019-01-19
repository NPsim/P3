using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {

	internal class ItemDatabase {

		private static List<string> item_list = new List<string>();

		public static void Add(string item) {
			item_list.Add(item);
		}

		public static bool Exists(string item) {
			foreach (string entry in item_list) {
				if (entry.ToUpper() == item.ToUpper()) {
					return true;
				}
			}
			return false;
		}

		public static List<string> List {
			get {
				return item_list;
			}
		}

		public static string[] Array {
			get {
				return item_list.ToArray();
			}
		}
	}
}
