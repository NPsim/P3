using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PseudoPopParser {
	class Menu {

		private static bool Exit;
		public static void Capture() {
			ConsoleKey KeyPressed;
			while(!Exit) {
				ShowOptions();
				KeyPressed = Console.ReadKey().Key;
				Console.Write("\n");

				switch (KeyPressed) {
					case ConsoleKey.F1: { // Credit Stats
						PrintColor.InfoLine("===Credit Statistics===");
						uint StartingCurrency = Program.PopFile.Population.StartingCurrency;
						List<List<uint>> Stats = Program.PopAnalyzer.CreditStatistics();
						List<string> ExplicitWaveCredits = new List<string>();

						// Analyze
						uint TotalDropped = 0;
						uint TotalBonus = 0;
						for (int i = 0; i < Stats.Count; i++) {
							List<uint> WaveCredits = Stats[i];

							uint WaveMax = 0;
							string Out = "";
							foreach (uint Credit in WaveCredits) {
								WaveMax += Credit;
								Out += " + " + Credit.ToString().PadLeft(4);
							}
							TotalDropped += WaveMax;

							// Format Zero Max
							if (Out.Length == 0) {
								//PrintColor.InfoLine("W{$0} : 0", (i + 1).ToString());
								ExplicitWaveCredits.Add("{f:Cyan}W" + (i + 1).ToString() + "{r} :    {f:Cyan}0{r}");
							}
							else {
								//PrintColor.InfoLine("W{$0} : {$1} = {$2}", (i + 1).ToString(), Max.ToString().PadLeft(5), Out.Substring(3));
								ExplicitWaveCredits.Add("{f:Cyan}W" + (i + 1).ToString() + "{r} : {f:Cyan}" + WaveMax.ToString().PadLeft(4) + "{r} = " + Out.Substring(3));
								if (i != Stats.Count - 1) {
									TotalBonus += 100; // Wave must drop at least 1 credit and not be last wave to receive bonus.
								}
							}
						}

						// Write
						PrintColor.InfoLine("Starting Credits         : {f:Cyan}{$0}{r}", StartingCurrency.ToString());
						PrintColor.InfoLine("Total Dropped Credits    : {f:Cyan}{$0}{r}", TotalDropped.ToString());
						PrintColor.InfoLine("Total Bonus Credits      : {f:Cyan}{$0}{r}", TotalBonus.ToString());
						PrintColor.InfoLine("Maximum Possible Credits : {f:Cyan}{$0}{r}", (StartingCurrency + TotalDropped + TotalBonus).ToString());
						foreach (string StatLine in ExplicitWaveCredits) {
							PrintColor.InfoLine(StatLine);
						}
						break;
					}
					case ConsoleKey.F2: { // WaveSpawn Names
						PrintColor.InfoLine("===WaveSpawn Names===");
						List<List<string>> Roster = Program.PopAnalyzer.WaveSpawnNames();

						for (int i = 0; i < Roster.Count; i++) {
							List<string> Names = Roster[i];
							PrintColor.InfoLine("Wave {$0}:", (i + 1).ToString());
							foreach (string WaveSpawnName in Names) {
								PrintColor.InfoLine("\t{$0}", WaveSpawnName);
							}
						}
						break;
					}
					case ConsoleKey.F3: // Templates
						PrintColor.InfoLine("===Template Names===");
						var Types = Program.PopAnalyzer.TemplateTypeNames();
						List<string> TFBotNames = Sort.PadSort(Types.Item1);
						List<string> WaveSpawnNames = Sort.PadSort(Types.Item2);
						List<string> GenericNames = Sort.PadSort(Types.Item3);
						PrintColor.InfoLine("TFBot Templates:");
						foreach (string Name in TFBotNames) {
							PrintColor.InfoLine("\t" + Name);
						}
						PrintColor.InfoLine("WaveSpawn Templates:");
						foreach (string Name in WaveSpawnNames) {
							PrintColor.InfoLine("\t" + Name);
						}
						PrintColor.InfoLine("Generic Templates:");
						foreach (string Name in GenericNames) {
							PrintColor.InfoLine("\t" + Name);
						}
						break;
					case ConsoleKey.F4: // Custom Icons
						PrintColor.InfoLine("===Custom Icons Used===");
						List<string> Icons = Program.PopAnalyzer.CustomIcons();
						// TODO Analyzer return all nondefault custom icons
						foreach (string Icon in Sort.PadSort(Icons)) {
							if (Icon == "scout_sunstick") {
								PrintColor.InfoLine("\t{$0} {f:DarkGray}(used in robot_standard.pop but has no default icon){r}", Icon);
							}
							else {
								PrintColor.InfoLine("\t{$0}", Icon);
							}
						}
						break;
					case ConsoleKey.F5: // Reparse
						Exit = true;
						// TODO Just quit and relaunch with specific flags
						System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
						myProcess.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "P3.exe";
						myProcess.StartInfo.Arguments = "\"" + string.Join("\" \"", Program.LaunchArguments) + "\"";
						myProcess.Start();
						break;
					case ConsoleKey.F6: // Search Items and Character Attributes
						PrintColor.InfoLine("===Search Item Names & Item/Char Attributes===");
						SearchItemCharacterAttributes();
						break;
					case ConsoleKey.F7: // Unused
						break;
					case ConsoleKey.F8: // Map Analyzer
						PrintColor.InfoLine("===Analyze Map (.bsp)===");
						MapAnalyzer();
						break;
					case ConsoleKey.F9: // Unused
						break;
					case ConsoleKey.F10: // Retarget items_game.txt
						PrintColor.InfoLine("===Retarget Database (items_game.txt)===");
						PrintColor.InfoLine(@"Please open your items_game.txt at");
						PrintColor.InfoLine(@".\steamapps\Team Fortress 2\tf\scripts\items\items_game.txt");

						// File Dialog
						OpenFileDialog dialog = new OpenFileDialog {
							InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
							Filter = "|items_game.txt"
						};
						try {
							dialog.ShowDialog();
							Program.Config.Write("items_source_file", "\"" + dialog.FileName + "\"", "Global");
							if (dialog.FileName.Length == 0) {
								throw new System.IO.FileNotFoundException();
							}
						}
						catch {
							Error.WriteNoIncrement("Failed to get file by dialog.", -1, 997);
						}

						// Attributes
						PrintColor.InfoLine("===Updating Databases===");
						using (AttributeScraper s = new AttributeScraper()) {

							PrintColor.InfoLine("> Attributes Database");
							PrintColor.InfoLine("Old version: {f:Yellow}{$0}{r}", s.Version);
							s.Scrape(dialog.FileName);
							PrintColor.InfoLine("New version: {f:Green}{$0}{r}", s.Version);
						}

						// Items
						using (ItemScraper s = new ItemScraper(dialog.FileName)) {

							PrintColor.InfoLine("> Items Database");
							PrintColor.InfoLine("Old version: {f:Yellow}{$0}{r}", s.CurrentVersion);
							s.Scrape(dialog.FileName);
							PrintColor.InfoLine("New version: {f:Green}{$0}{r}", s.CurrentVersion);
						}
						break;
					case ConsoleKey.F11: // Fullscreen
						// Windows Default Key
						// This cannot be easily changed
						break;
					case ConsoleKey.F12:
						break;
					default:
						Exit = true;
						break;
				}
			}
		}

		private static void ShowOptions() {
			Console.Write("\n");
			PrintColor.WriteLine("{b:White}{f:Black}F1{r} Show Credit Stats".PadRight(33 + 21) + "{b:White}{f:Black}F5{r} Reparse Pop File (Restart)".PadRight(33 + 21) + "{b:White}{f:Black}F9{r}  -Unused-".PadRight(33 + 21)	);
			PrintColor.WriteLine("{b:White}{f:Black}F2{r} Show WaveSpawn Names".PadRight(33 + 21) + "{b:White}{f:Black}F6{r} Search Items & Attributes".PadRight(33 + 21) + "{b:White}{f:Black}F10{r} Update Databases".PadRight(33 + 21)	);
			PrintColor.WriteLine("{b:White}{f:Black}F3{r} Show Template Names".PadRight(33 + 21) + "{b:White}{f:Black}F7{r} -Unused-".PadRight(33 + 21) + "{b:White}{f:Black}F11{r} Fullscreen (Windows Default)".PadRight(33 + 21)	);
			PrintColor.WriteLine("{b:White}{f:Black}F4{r} Show Custom Icons".PadRight(33 + 21) + "{b:White}{f:Black}F8{r} Analyze Map (BSP)".PadRight(33 + 21) + "{b:White}{f:Black}F12{r} Open P3 Code Reference (PDF)".PadRight(33 + 21)	);
			Console.Write("\n");
			PrintColor.WriteLine("{b:White}{f:Black}Any Key{r} Quit");
			Console.Write("\n");
		}

		private static void SearchItemCharacterAttributes() {
			PrintColor.InfoLine("{f:Black}{b:Gray}Search for an item or attribute name{r} or {f:Black}{b:Gray}Enter an item's exact name to view its attributes{r}");

			// Get User Input
			PrintColor.Info("Search Term: ");
			string search_phrase = Console.ReadLine();

			// Go Back if User Enters Blank or Spaces
			if (Regex.Replace(search_phrase, @"\s*", "") == "") {
				PrintColor.InfoLine("Invalid entry. Canceling Search.");
				return;
			}

			// Blank Separator Line
			PrintColor.InfoLine("");

			/* Item Names */
			// Get Results
			string[] item_results = Search.Simple(ItemDatabase.List, search_phrase);

			// Show Results
			PrintColor.InfoLine("Item results for {b:White}{f:Black} " + search_phrase + " {r}");
			foreach (string item in item_results) {
				PrintColor.InfoLine("\t" + item);
				if (search_phrase.ToUpper() == item.ToUpper()) {
					Dictionary<string, string> attributes = ItemDatabase.Attributes(item);
					foreach (string key in attributes.Keys) {
						PrintColor.InfoLine("\t    \"" + key + "\" " + attributes[key] + "");
					}

					if (attributes.Keys.Count == 0) {
						PrintColor.InfoLine("\t    No Usable Item Attributes Found.");
					}
				}
			}

			// No Items Found
			if (item_results.Count() == 0) {
				PrintColor.InfoLine("\tNo Items Found.");
			}

			// Blank Separator Line
			PrintColor.Info("\n");

			/* Item/Char Attributes */
			// Get Results
			string[] att_results = Search.Simple(AttributeDatabase.ListZeroIndex, search_phrase);

			// Show Results
			PrintColor.InfoLine("Attribute results for {b:White}{f:Black} " + search_phrase + " {r}");
			foreach (string attribute in att_results) {
				PrintColor.InfoLine("\t" + attribute);
			}

			// No Attributes Found
			if (att_results.Count() == 0) {
				PrintColor.InfoLine("\tNo Attributes Found.");
			}
		}

		private static void MapAnalyzer() {
			PrintColor.InfoLine("{f:Black}{b:Gray}Select a BSP to generate a list of bot spawns, logic relays, nav prefers, and tank nodes{r}");
			string map_path = "";
			try {

				// TODO Refactor pop_folder
				string pop_folder = Regex.Match(Program.FullPopFilePath, @"^.*[\/\\]").ToString(); // Regex: Match everything up to last / or \
				System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog {
					InitialDirectory = System.IO.Path.GetFullPath(pop_folder),
					Filter = "MvM Map Files|*.bsp"
				};
				dialog.ShowDialog();
				if (dialog.FileName.Length == 0) {
					throw new Exception("NoFile");
				}
				PrintColor.InfoLine("Map: " + dialog.FileName);
				map_path = dialog.FileName;
			}
			catch {
				Error.WriteNoIncrement("Failed to get file by dialog.", -1, 997);
				return;
			}

			try {
				// Analyze Map
				MapAnalyzer map = new MapAnalyzer(map_path);

				// Display Results
				PrintColor.InfoLine("Bot Spawns:");
				foreach (string location in Sort.PadSort(map.Spawns)) {
					PrintColor.InfoLine("\t" + location);
				}
				PrintColor.InfoLine("Logic Relays:");
				foreach (string relay in Sort.PadSort(map.Relays)) {
					PrintColor.InfoLine("\t" + relay);
				}
				PrintColor.InfoLine("Nav Prefers:");
				foreach (string nav in Sort.PadSort(map.Navs)) {
					PrintColor.InfoLine("\t" + nav);
				}
				PrintColor.InfoLine("Tank Nodes:");
				foreach (string track in Sort.PadSort(map.Tracks)) {
					PrintColor.InfoLine("\t" + track);
				}


			}
			catch (Exception e) {
				switch (e.Message) {
					case "InvalidVBSPException":
						Error.WriteNoIncrement("Invalid VBSP file.", -1, 996);
						break;

					default:
						Error.WriteNoIncrement("Failed to decompile VBSP: '{$0}'", -1, 995, e.Message);
						break;
				}
			}
		}
	}
}
