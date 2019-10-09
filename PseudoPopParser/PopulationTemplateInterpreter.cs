using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {
	class PopulationTemplateInterpreter {

		private static readonly Dictionary<string, dynamic> Templates = Program.PopFile.Population.Templates;

		public static PopFile InterpretPopFile(PopFile pop) { // Currently only supports single layer templates
			return pop;
		}
	}
}

