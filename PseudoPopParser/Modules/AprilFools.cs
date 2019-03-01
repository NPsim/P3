using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PseudoPopParser {
	class AprilFools {
		private static bool tcolor = false;
		private static void toggle_color(string message) {
			if (tcolor) {
				PrintColor.Colorf("{b:DarkGreen}{0}{r}", message);
				tcolor = !tcolor;
			}
			else {
				PrintColor.Colorf("{b:DarkBlue}{0}{r}", message);
				tcolor = !tcolor;
			}
		}

		// A terrible solution to a terrible problem that never existed
		public static void DoTheThing(List<string[]> tokens, string start_in = "") {
			string target_path = "NULL PATH";
			try {
				SaveFileDialog dialog = new SaveFileDialog {
					InitialDirectory = start_in,
					Filter = "Efficient Pop File | *.pop"
				};
				dialog.ShowDialog();
				target_path = dialog.FileName;
				PrintColor.InfoLine("Created efficient pop file at {f:Black}{b:Gray}{0}{r}", target_path);
			}
			catch {
				target_path = "";
			}

			using (StreamWriter writer = new StreamWriter(new FileStream(target_path, FileMode.Create))) {
				bool opened_string = false;
				bool wrote_first = false;
				foreach (string[] line in tokens) {
					bool found_comment = false;
					foreach (string term in line) {
						if (Regex.IsMatch(term, @"\/\/") || found_comment) {
							found_comment = true;
							continue;
						}
						if (term.Length == 0) {
							continue;
						}
						if (term == "\"") {
							if (opened_string) { // Deal with Close Quote
								writer.Write("\"");
								//toggle_color("\"");
								opened_string = false;
							}
							else { // Deal with Open Quote
								writer.Write(" \"");
								//toggle_color(" \"");
								wrote_first = false;
								opened_string = true;
							}
						}
						else {
							if (!wrote_first) {
								writer.Write(term);
								//toggle_color(term);
								wrote_first = true;
							}
							else {
								writer.Write(" " + term);
								//toggle_color(" " + term);
							}
						}
					}
				}
				writer.Close();
			}
		}
	}
}
