using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PseudoPopParser {
	class MapAnalyzer {
		public static void AnalyzeSimple(string bsp_file, out string[] spawns, out string[] relays, out string[] tracks, out string[] navs) {
			List<string> spawn_points = new List<string>();
			List<string> logic_relays = new List<string>();
			List<string> path_tracks = new List<string>();
			List<string> nav_paths = new List<string>();
			string[] bsp_lines = File.ReadAllLines(bsp_file);
			bool in_block = false;
			string target_name = "";
			string tags = ""; // For func_nav_prefer

			// Scan through .bsp
			for(int i = 0; i < bsp_lines.Length; i++) {
				//string target_name = "";
				//string tags = "";
				string line = bsp_lines[i];

				// Open on open curly
				if (line == "{") {
					in_block = true;
				}

				// Reset on close curly
				else if (line == "}") {
					in_block = false;
					target_name = "";
					tags = "";
				}

				// Scrape for values
				if (in_block) {

					// Scan targetname, verify valid target later
					if (Regex.IsMatch(line, "\"targetname\"")) {
						target_name = Regex.Match(line, "\\\"(.*?)\\\"").NextMatch().ToString().Trim('"');
					}

					// Scan tags, for func_nav_prefer
					else if (Regex.IsMatch(line, "\"tags\"")) {
						tags = Regex.Match(line, "\\\"(.*?)\\\"").NextMatch().ToString().Trim('"');
					}



					// Verify valid info_player_teamspawn target for spawnbot, add valid targetname to list
					else if (target_name.Length > 0 && Regex.IsMatch(line, "\"classname\"") && Regex.IsMatch(line, "\"info_player_teamspawn\"")) {
						spawn_points.Add(target_name);
						in_block = false;
						target_name = "";
					}

					// Verify valid logic_relay target for action-trigger, add valid targetname to list
					else if (target_name.Length > 0 && Regex.IsMatch(line, "\"classname\"") && Regex.IsMatch(line, "\"logic_relay\"")) {
						logic_relays.Add(target_name);
						in_block = false;
						target_name = "";
					}

					// Verify valid path_track target for tank path, add valid targetname to list
					else if (target_name.Length > 0 && Regex.IsMatch(line, "\"classname\"") && Regex.IsMatch(line, "\"path_track\"")) {
						path_tracks.Add(target_name);
						in_block = false;
						target_name = "";
					}

					// Verify valid func_nav_prefer target for bot nav prefer path, add valid targetname to list
					else if (target_name.Length > 0 && Regex.IsMatch(line, "\"classname\"") && Regex.IsMatch(line, "\"func_nav_prefer\"")) {
						nav_paths.Add(target_name);
						
						// Break up tags and add as individual aliases
						foreach(string alias in tags.Split(' ')) {
							nav_paths.Add(alias);
						}

						in_block = false;
						target_name = "";
					}
				}
			}
			spawns = spawn_points.Distinct().ToArray();
			relays = logic_relays.Distinct().ToArray();
			tracks = path_tracks.Distinct().ToArray();
			navs = nav_paths.Distinct().ToArray();
		}
	}
}
