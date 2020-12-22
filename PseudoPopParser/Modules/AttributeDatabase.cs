using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {

	internal class AttributeDatabase {

		private class AttributeDefinition {
			public string ID;
			public string Type;
			public string ValueType;
			public AttributeDefinition() { }
		}

		private static readonly string Path = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\item_attributes.ffd";
		private static readonly Dictionary<string, AttributeDefinition> Attributes = new Dictionary<string, AttributeDefinition>();
		public static List<string> Keys => Attributes.Keys.ToList();
		public static string Version { private set; get; }

		public static void Build() {
		
			// Database must exist
			if (!File.Exists(Path)) {
				Error.WriteNoIncrement("Could not find local database.", -1, 997);
				return;
			}

			string[] RawDBLines = File.ReadAllLines(Path);
			Version = RawDBLines[0];
			for (int Line = 1; Line < RawDBLines.Length; Line += 3) {
				Attributes[RawDBLines[Line]] = new AttributeDefinition {
					ID = RawDBLines[Line],
					Type = RawDBLines[Line + 1].Substring(1, RawDBLines[Line + 1].Length - 1),
					ValueType = RawDBLines[Line + 2].Substring(1, RawDBLines[Line + 2].Length - 1)
				};
			}
		}
	
		public static bool Exists(string Key) {
			foreach(string Attribute in Keys) {
				if (Attribute.ToUpper() == Key.ToUpper()) return true;
			}
			return false;
		}

		public static void Verify(string Key, Antlr4.Runtime.ParserRuleContext Context) {
			if (!Exists(Key) && Program.Config.ReadBool("bool_warn_invalid_item_char_attribute") && !Program.Config.ReadBool("bool_unsafe")) {
				Warning.Write("{f:Yellow}Invalid{r} {f:Yellow}Attribute Name{r} found: '{f:Yellow}{$0}{r}'", Context.Stop.Line, 201, Key);
			}
		}
	}
}
