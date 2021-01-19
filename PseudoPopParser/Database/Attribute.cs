using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser.Database {
	class Attribute {
		public string Name { get; set; }
		public string LocalizedDescription { get; set; }
		public string Format { get; set; }
		public string ValueType { get; set; }
		public string EffectType { get; set; }

		public Attribute() { }

		public Attribute(string name) {
			this.Name = name;
		}
	}
}
