using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {

	class AttributeDatabase {

		private static List<string[]> attribute_list = new List<string[]>();

		public static void Add(string[] item) {
			attribute_list.Add(item);
		}

		public static List<string[]> List {
			get {
				return attribute_list;
			}
		}

		public static string[][] Array {
			get {
				return attribute_list.ToArray();
			}
		}

		public static List<string> ListZeroIndex {
			get {
				var single_dimension = new List<string>();
				foreach(string[] str_array in attribute_list) {
					single_dimension.Add(str_array[0]);
				}
				return single_dimension;
			}
		}

		public static string[] ArrayZeroIndex {
			get {
				return ListZeroIndex.ToArray();
			}
		}
	}
}
