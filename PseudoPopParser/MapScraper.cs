using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PseudoPopParser {
	class MapScraper {
		public static void Scrape(string bsp_file, out string[] spawns, out string[] relays) {
			List<string> spawn_points = new List<string>();
			List<string> logic_relays = new List<string>();
			string[] bsp_lines = File.ReadAllLines(bsp_file);
			bool in_block = false;
			string target_name = "";

			// Scan through .bsp
			for(int i = 0; i < bsp_lines.Length; i++) {
				string line = bsp_lines[i];

				// Open on open curly
				if (line == "{") {
					in_block = true;
				}

				// Reset on close curly
				else if (line == "}") {
					in_block = false;
					target_name = "";
				}

				// Scrape for values
				if (in_block) {

					// Scan targetname, verify valid target later
					if (Regex.IsMatch(line, "\"targetname\"")) {
						target_name = Regex.Match(line, "\\\"(.*?)\\\"").NextMatch().ToString().Trim('"');
					}

					// Verify valid info_player_teamspawn target, add valid targetname to list
					else if (target_name.Length > 0 && Regex.IsMatch(line, "\"classname\"") && Regex.IsMatch(line, "\"info_player_teamspawn\"")) {
						spawn_points.Add(target_name);
						in_block = false;
						target_name = "";
					}

					// Verify valid logic_relay target, add valid targetname to list
					else if (target_name.Length > 0 && Regex.IsMatch(line, "\"classname\"") && Regex.IsMatch(line, "\"logic_relay\"")) {
						logic_relays.Add(target_name);
						in_block = false;
						target_name = "";
					}
				}
			}
			spawns = spawn_points.Distinct().ToArray();
			relays = logic_relays.Distinct().ToArray();
		}
	}
}
