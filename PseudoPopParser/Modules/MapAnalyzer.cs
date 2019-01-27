using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PseudoPopParser {

	internal class MapAnalyzer {

		private List<string> spawn_points = new List<string>();
		private List<string> logic_relays = new List<string>();
		private List<string> path_tracks = new List<string>();
		private List<string> nav_paths = new List<string>();
		private string jar_path = Program.root_directory + @"BSPSource\bspsrc.jar";
		private string decompile_target = Program.root_directory + @"BSPSource\decompiled.vmf";
		private string target_path;
		private string[] bsp_lines;
		private bool bsp_compiled = false;

		public MapAnalyzer(string bsp_file) {
			target_path = bsp_file;
			bsp_lines = File.ReadAllLines(target_path);

			// Check VBSP header
			if (!IsValidVBSP) {
				throw new Exception("InvalidVBSPException");
			}

			// Check if map needs decompiling
			if (Program._INI.ReadBool("bool_map_analyzer_always_decompile") || !(bsp_lines.Any(s => s.Contains("targetname")) && bsp_lines.Any(s => s.Contains("hammerid")))) {
				bsp_compiled = true;
				Decompile(target_path, decompile_target);
				target_path = decompile_target;
			}

			// Do the thing
			AnalyzeSimple(target_path);

			// Reset decompile buffer
			if (bsp_compiled && Program._INI.ReadBool("bool_map_analyzer_clear_buffer")) {
				File.WriteAllText(target_path, string.Empty);
			}
		}

		private void Decompile(string bsp, string out_path) {
			try {
				PrintColor.InfoLine("{f:Black}{b:White}=============Decompiling using BSPsrc============={r}");

				System.Diagnostics.Process BSPsrc = new System.Diagnostics.Process();
				BSPsrc.StartInfo.FileName = "java";
				if (Program._INI.ReadBool("bool_map_analyzer_full_decompile")) {
					BSPsrc.StartInfo.Arguments = "-jar " + jar_path + " \"" + target_path + "\" -o \"" + out_path + "\"";
				}
				else {
					BSPsrc.StartInfo.Arguments = "-jar " + jar_path + " \"" + target_path + "\" -o \"" + out_path + "\" -no_areaportals -no_cubemaps -no_details -no_ladders -no_occluders -no_overlays -no_rotfix -no_sprp -no_brushes -no_disps -no_texfix -no_cams -no_lumpfiles -no_prot -no_visgroups";
				}
				BSPsrc.StartInfo.UseShellExecute = false;
				BSPsrc.StartInfo.RedirectStandardOutput = false;
				BSPsrc.StartInfo.RedirectStandardError = false;
				BSPsrc.Start();
				BSPsrc.WaitForExit();

				PrintColor.InfoLine("{f:Black}{b:White}=================BSPsrc Finished=================={r}");
			}
			catch (Exception e) {
				Error.NoTrigger.MapFailedDecompile(e.Message);
			}
		}

		private void AnalyzeSimple(string target) {
			bsp_lines = File.ReadAllLines(target);
			bool in_block = false;
			string target_name = "";
			string tags = ""; // For func_nav_prefer

			// Scan through .bsp
			for (int i = 0; i < bsp_lines.Length; i++) {
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
					else if ((target_name.Length > 0 || tags.Length > 0) && Regex.IsMatch(line, "\"classname\"") && Regex.IsMatch(line, "\"func_nav_prefer\"")) {

						if (target_name.Length > 0) {
							nav_paths.Add(target_name);
						}

						// Break up tags and add as individual aliases
						foreach(string alias in tags.Split(' ')) {
							if (alias.Length > 0) {
								nav_paths.Add(alias);
							}
						}

						in_block = false;
						tags = "";
						target_name = "";
					}
				}
			}
		}

		public bool IsValidVBSP {
			get {
				return bsp_lines[0].Substring(0, 4) == "VBSP";
			}
		}

		public bool IsCompiled {
			get {
				return bsp_compiled;
			}
		}

		public string[] Spawns {
			get {
				return spawn_points.Distinct().ToArray();
			}
		}

		public string[] Relays {
			get {
				return logic_relays.Distinct().ToArray();
			}
		}

		public string[] Tracks {
			get {
				return path_tracks.Distinct().ToArray();
			}
		}

		public string[] Navs {
			get {
				return nav_paths.Distinct().ToArray();
			}
		}
	}
}
