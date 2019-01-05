using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {

	class Search {

		public static string[] Simple(List<string> database, string phrase) {
			List<string> results = new List<string>(database);
			string[] split_phrase = phrase.Split(' ');
			foreach (string term in split_phrase) {
				foreach (string entry in database) {
					if (!entry.ToUpper().Contains(term.ToUpper())) {
						results.Remove(entry);
					}
				}
			}
			return results.ToArray();
		}

	}

}
