using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {

	class Sort {

		public static string[] PadSort(string[] unsorted) {
			var result = unsorted.OrderBy(str => Regex.Replace(str, "[0-9]+", match => match.Value.PadLeft(10, '0')));
			return result.ToArray();
		}

		public static List<string> PadSort(List<string> unsorted) {
			var result = unsorted.OrderBy(str => Regex.Replace(str, "[0-9]+", match => match.Value.PadLeft(10, '0')));
			return result.ToList();
		}

	}

}
