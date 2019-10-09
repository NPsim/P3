using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {

	internal class Warning {

		public static uint Warnings { get; private set; } = 0;
		public static void Write(string message, int line = -1, uint code = 0300, params string[] args) {
			Warnings++;
			WriteNoIncrement(message, line, code, args);
		}
		public static void WriteNoIncrement(string message, int line = -1, uint code = 0300, params string[] args) {
			if (line == -1)
				PrintColor.WriteLine("{f:Black}{b:Yellow}[Warning-W" + code.ToString("0000") + "]{r}" + String.Empty.PadRight(7) + message, args);
			else
				PrintColor.WriteLine("{f:Black}{b:Yellow}[Warning-W" + code.ToString("0000") + "]{r}:" + line.ToString().PadRight(6) + message, args);
		}
	}
}
