using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PseudoPopParser {
	class AprilFools {
		// A terrible solution to a terrible problem that never existed
		public static void DoTheThing(List<string> tokens) {
			string target;
			try {
				SaveFileDialog dialog = new SaveFileDialog {
					InitialDirectory = Program.FullPopFileDirectory,
					Filter = "Efficient Pop File | *.pop"
				};
				dialog.ShowDialog();
				target = dialog.FileName;
				PrintColor.InfoLine("Created efficient pop file at {f:Black}{b:Gray}{$0}{r}", target);
			}
			catch {
				throw new System.Exception("SecretException");
			}

			using (StreamWriter writer = new StreamWriter(new FileStream(target, FileMode.Create))) {
				foreach (string token in tokens) {
					if (token == "{") {
						writer.Write("{ ");
					}
					else if (token == "}") {
						writer.Write("} ");
					}
					else if (token == "<EOF>") continue;
					else
						writer.Write(token + " ");
				}
				writer.Close();
			}
		}
	}
}
