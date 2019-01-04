using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PseudoPopParser {

	class Program {

		public static IniFile _INI;
		public static string launch_arguments = "";
		private static bool auto_close = false;

		static bool _IsDebug(string key) {
			string[] true_values = { "1", "YES", "TRUE" };
			string dict_value = _INI.Read(key);
			if (true_values.Contains(dict_value.ToUpper())) {
				return true;
			}
			return false;
		}

		[STAThread]
		static void Main(string[] args) {

			foreach(string argument in args) {
				launch_arguments += argument + " ";
			}

			// Console Size
			Console.SetWindowSize(100, 50);

			// Version Message
			PrintColor.InfoLine("P3 v1.3.0");

			string P3_root = AppDomain.CurrentDomain.BaseDirectory;
			_INI = new IniFile(P3_root + @"config.ini");
			string file_path = "";
			string datatypes_folder = P3_root;
			string grammar_file = P3_root + @"datatypes\grammar.twt";
			string[] file = null;
			bool bypass_print_config = false;

			// Debug Terminator
			if (_IsDebug("bool_Print_Terminators")) {
				Console.WriteLine(">>>>>Start of file | Debug Level ");
			}

			List<string[]> token_list = new List<string[]> {
				new string[] { } // No 0 indexing
			};

			// Get Launch Flags
			for (int i = 0; i < args.Length; i++) {
				if (args[i] == "-pop") {
					file_path = args[i + 1];
				}
				if (args[i] == "-grammar") {
					grammar_file = args[i + 1];
				}
				if (args[i] == "-datatypes_folder_path") {
					datatypes_folder = args[i + 1];
				}
				if (args[i] == "--print_config") {
					bypass_print_config = true;
				}
				if (args[i] == "-output_log") {
					PrintColor.log_path = args[i + 1];
				}
				if (args[i] == "--auto_close") {
					auto_close = true;
				}
			}

			if (!Regex.IsMatch(file_path, @"\.pop$")) {
				PrintColor.InfoLine("Open your Pop file.");
				try {
					OpenFileDialog ofd = new OpenFileDialog {
						InitialDirectory = Path.GetFullPath(P3_root),
						Filter = "Population Files|*.pop"
				};
					ofd.ShowDialog();
					if (ofd.FileName.Length == 0 || !Regex.IsMatch(ofd.FileName, @"\.pop$")) {
						throw new Exception("NoFile");
					}
					file_path = ofd.FileName;
				}
				catch {
					Error.NoTrigger.FailedDialog();
				}
			}

			// Get file_path if not defined previously
			while (!Regex.IsMatch(file_path, @"\.pop$")) {
				Console.Write("Enter the path to your Pop file: ");
				file_path = Console.ReadLine();

				if (!Regex.IsMatch(file_path, @"\.pop$")) {
					Error.NoTrigger.MissingExtension();
				}
			}

			// Get pop file's containing directory
			string pop_folder = Regex.Match(file_path, @"^.*[\/\\]").ToString(); // Regex: Match everything up to last / or \

			// Get pop file's name
			string pop_file_name = Regex.Match(file_path, @"[\w-]+\.pop").ToString();

			// Init Parser
			PopParser p = new PopParser(datatypes_folder, pop_folder, pop_file_name);
			ParseTree pt = new ParseTree(grammar_file);

			string[] globalkeys = _INI.Keys("Debug");

			// Debug Print Config
			if (_IsDebug("bool_Print_Config") || bypass_print_config) {

				string[] keys;
				// Global Config
				PrintColor.DebugLine("\t[Global]");
				keys = _INI.Keys("Global");
				foreach(string key in keys) {
					PrintColor.DebugLine(key + " : " + _INI.Read(key, "Global"));
				}

				// Global Config
				PrintColor.DebugLine("\t[Debug]");
				keys = _INI.Keys("Debug");
				foreach (string key in keys) {
					PrintColor.DebugLine(key + " : " + _INI.Read(key, "Debug"));
				}

				// End of Configuration
				PrintColor.DebugLine("\tEnd Config");
			}

			/* Begin */

			// Populate file[] var
			try {
				file = File.ReadAllLines(file_path);
				PrintColor.InfoLine("Pop File - {f:Cyan}{0}{r}", file_path);
			}
			catch {
				//PrintColor.ErrorNoTrigger("Could not open Pop file.");
				Error.NoTrigger.Unknown("Could not open Pop file.");
				PrintColor.ColorLinef("Press Any Key to quit.");
				Console.ReadKey();
				return;
			}

			// Modify strings
			for (int i = 0; i < file.Length; i++) {
				//file[i] = Regex.Replace(file[i], @"(\s|\/+|^)\/\/.*[\s]*", "");     // Remove Comments; breaks // within string
				file[i] = Regex.Replace(file[i], @"{", " { ");                      // Separate Open Curly Braces
				file[i] = Regex.Replace(file[i], @"}", " } ");                      // Separate Close Curly Braces
				file[i] = Regex.Replace(file[i], "\"", " \" ");                     // Separate Double Quotes
				file[i] = Regex.Replace(file[i], @"^\s+", "");                      // Remove Indentation Whitespace
			}

			// Get Tokens
			for (int i = 0; i < file.Length; i++) {
				token_list.Add(Regex.Split(file[i], @"\s+"));
			}

			/* Parse Tokens
			 * i: line number (1 index)
			 * j: token position in line (0 index)
			 */
			int global_line = 0;
			string global_token = "";
			List<string> string_builder = new List<string>();
			bool building_string = false;
			bool built_string = false;
			bool look_ahead_open = false;
			bool found_comment = false;
			string look_back_token = "";
			string look_ahead_buffer_node = "";
			string template_name = "";
			try {

				// Iterate by Line List
				for (int i = 0; i < token_list.Count; i++) {
					found_comment = false;
					// Iterate by Token List of Line
					for (int j = 0; j < token_list[i].Length; j++) {

						// Only Scan Real Tokens
						if (string.IsNullOrWhiteSpace(token_list[i][j]) || found_comment) {
							continue;
						}

						if (!building_string && Regex.IsMatch(token_list[i][j], @"\/\/")) {
							found_comment = true;

							// Continue if no value attached to front of comment token
							if (Regex.IsMatch(token_list[i][j], @"^\/\/")) {
								continue;
							}
						}

						// Tokenized Information
						int line = i;
						global_line = i; // Redundant
						string token_raw_comment = token_list[i][j]; // Only used to throw CollectionBadCommentException
						string token = Regex.Replace(token_list[i][j], @"\/\/.*", "");
						global_token = token; // Redundant
						TreeNode<string[]> current = pt.Current;
						List<TreeNode<string[]>> children = pt.Current.Children;

						// Debug Level 2
						if (_IsDebug("bool_Print_Tokens")) Console.WriteLine(token + "\t\t\t" + i + " " + j);

						{ } // Debug Breakpoint

						// Debug Level 4
						if (_IsDebug("bool_Print_Token_Operations")) {
							Console.WriteLine("->token:" + token);

							if (building_string) Console.WriteLine("====STRINGBUILDER ACTIVE====");

							Console.Write("->currentvalue:");
							foreach (string vindex in current.Value) Console.Write(vindex + "|");
							Console.WriteLine("");

							Console.Write("->currentchildren:");
							foreach (TreeNode<string[]> cindex in pt.Current.Children) Console.Write(cindex.Value[1] + "|");
							Console.WriteLine("");
						}

						// Iterate through childen for token match
						bool found = false; // Must be true to continue token
						for (int c = 0; c < children.Count; c++) {
							TreeNode<string[]> child = children[c];

							/* String Builder */
							if (token == "\"") {

								if (!building_string) {
									found = true;
									building_string = true;
									break;
								}
								else if (building_string) {
									building_string = false;

									// Finalize string
									token = "";
									foreach (string term in string_builder) {
										token = token + term + " ";
									}
									token = token.Substring(0, token.Length - 1);
									global_token = token;

									// Clear string builder
									string_builder.Clear();
									built_string = true;
								}
							}
							else if (building_string) {
								found = true;
								string_builder.Add(token_raw_comment);
								break;
							}

							/* Parser Operations */
							// Collection Emergence
							if (token == "}") {
								found = true;

								// Trigger End of Calculations Before Exiting
								p.ParseCollectionEnd(pt.CurrentValue[1], line, pt.ParentValue[1]);

								// Reset Template Name Tracker
								if (pt.ParentValue[1] == "Templates{}") {
									template_name = "";
								}

								pt.MoveUp();

								// Detect Possible Premature End of WaveSchedule : WaveSchedule closes in <99% of the total line count.
								if (i < token_list.Count * 99 / 100 && pt.Current.Value[2] == "NONE") {
									Warning.PrematureEndWaveSchedule(global_line, global_token);
								}

								if (_IsDebug("bool_Print_PT_Cursor_Traversal")) {
									Console.WriteLine("==== UP CLOSE BRACE");
								}
								break;
							}

							// Collection Diving
							else if (look_ahead_open && token == "{") {

								found = true;
								if (_IsDebug("bool_Print_PT_Cursor_Traversal")) {
									Console.WriteLine("==== DOWN LOOK AHEAD OPEN");
								}

								pt.Move(look_ahead_buffer_node);

								// Parse Collection After Diving
								p.ParseCollection(pt.CurrentValue[1], line, pt.ParentValue[1], look_back_token);

								// Save Template Name for Parsing Later
								if (pt.ParentValue[1] == "Templates{}") {
									template_name = look_back_token;
								}

								look_ahead_buffer_node = "";
								look_ahead_open = false;
								break;
							}

							// Handles collections and % (any valid string)
							else if (token.ToUpper() == p.RemoveCurly(child.Value[1]).ToUpper() || Regex.IsMatch(child.Value[1], @"%")) {
								found = true;

								// Check for Valid String
								if (Regex.IsMatch(child.Value[1], @"%") && !p.IsDatatype("$ANY_VALID_STRING", token) && !built_string) {
									throw new Exception("IllegalIdentifierException");
								}

								// Move Down If Token Is Collection
								if (child.Value[0].ToUpper() == "COLLECTION") {
									if (_IsDebug("bool_Print_PT_Cursor_Traversal")) {
										Console.WriteLine("==== LOOK AHEAD OPEN " + token);
									}

									if (Regex.IsMatch(token_raw_comment, @"\S\/\/")) {
										throw new Exception("CollectionBadCommentException");
									}

									look_ahead_open = true;
									look_ahead_buffer_node = child.Value[1];
									break;
								}

								// Handles Keys
								else if (child.Value[0].ToUpper() == "KEY") { // Move in if token is key

									// Special case When versus When{} in PeriodicSpawn{}
									if (token.ToUpper() == "WHEN" && token_list[i + 1][j] == "{") {
										look_ahead_open = true;
										look_ahead_buffer_node = children[c + 1].Value[1];
										if (_IsDebug("bool_Print_PT_Cursor_Traversal")) {
											Console.WriteLine("SAW WHEN{}");
										}
										Warning.MinMaxIntervalStopSpawn(global_line);
										break;
									}

									if (_IsDebug("bool_Print_PT_Cursor_Traversal")) {
										Console.WriteLine("==== DOWN " + token);
									}

									if (Regex.IsMatch(child.Value[1], @"%")) {
										pt.Move(child.Value[1]);
									}
									else {
										pt.Move(token);
									}

									break;
								}

								// Handles Values; assume token must be value string (catch by IsDatatype)
								else {

									string child_datatype = child.Value[3];

									if (p.IsDatatype(child_datatype.ToUpper(), token, line)) {
										if (_IsDebug("bool_Print_PT_Cursor_Traversal")) {
											Console.WriteLine("==== UP 1");
										}
										pt.MoveUp();
										break;
									}
									else if (_IsDebug("bool_Print_PT_Cursor_Traversal")) {
										Console.WriteLine("==== CHILD DT DID NOT MATCH TOKEN 1");
									}
								}
							}

							else if (current.Value[0].ToUpper() == "KEY") { // Handle All Keys

								// Parse Template Pop File
								if (look_back_token.ToUpper() == "#BASE") {
									string base_file_path = pop_folder + token;
										
									// Detect Default Template
									string[] default_templates = { "ROBOT_STANDARD.POP", "ROBOT_GIANT.POP", "ROBOT_GATEBOT.POP" };
									bool is_default = false;
									if (default_templates.Contains(token.ToUpper())) {
										base_file_path = P3_root + "base_templates\\" + token;
										is_default = true;
									}

									// Do the thing.
									p.ParseBase(base_file_path, is_default);
								}
								// Parse Key and Value
								else {
									p.ParseKeyValue(look_back_token, token, line, pt.ParentValue[1], template_name);
								}

								{ } // Debug Breakpoint

								// Debug Token Lookback
								// Writes all readable key-value pairs
								if (_IsDebug("bool_Print_Token_Lookback")) {
									Console.WriteLine("Key is: " + look_back_token);
									Console.WriteLine("\tValue is: " + token);
									Console.WriteLine("\tParent is: " + pt.ParentValue[1]);
								}

								if (current.Value[1] == "$char_attribute%" || current.Value[1] == "$item_attribute%") { // Special Case Item/Character Attribute
									found = true;

									// Placeholder for item attribute verification
									// look_back_token : "damage bonus"
									// token : "1.0"

									if (_IsDebug("bool_Print_PT_Cursor_Traversal")) {
										Console.WriteLine("==== UP C1: " + token);
									}

									p.ParseAttribute(look_back_token, token, line);

									pt.MoveUp();
									break;
								}

								string child_datatype = child.Value[3];
								if (p.IsDatatype(child_datatype.ToUpper(), token, line)) {
									found = true;

									if (_IsDebug("bool_Print_PT_Cursor_Traversal")) {
										Console.WriteLine("==== UP 2");
									}

									pt.MoveUp();
									break;
								}

								else if (_IsDebug("bool_Print_PT_Cursor_Traversal")) {
									Console.WriteLine("==== CHILD DT DID NOT MATCH TOKEN 2");
								}
							}
						}

						// Throw Exception If Nothing Matched
						if (!found) {
							// Catch Collection within Collection
							string[] collections = { "SQUAD{}", "MOB{}", "RANDOMCHOICE{}", "SQUAD", "MOB", "RANDOMCHOICE" };
							if (collections.Contains(token.ToUpper()) && collections.Contains(pt.CurrentValue[1].ToUpper())) {
								throw new Exception("NestedCollectionException");
							}
								
							throw new Exception("UnknownSymbolException");
						}

						// Set Look-Back Token; Avoid String Fragments
						if (!building_string) {
							look_back_token = token;
						}

						// Reset Global Values
						built_string = false;
					}
				}
				PrintColor.InfoLine("\tDone parsing Pop File - {f:Cyan}{0}{r}", pop_file_name);
			}
			catch (Exception ex) {

				if (ex.Message == "UnknownSymbolException") {
					Error.InvalidSymbol(global_line, global_token);
				}

				/* BadCommentException */
				// Lexer Exception : No space found before a "//" single-line comment token; only applies to collections
				else if (ex.Message == "CollectionBadCommentException") {
					Error.BadComment(global_line, global_token);
				}

				
				/* IllegalIdentifierException */
				// Lexer Exception : $ANY_VALID_STRING contains one of the following symbols { } " #base #include
				/*else if (ex.Message == "IllegalIdentifierException") {
					Error.InvalidName(global_line, global_token);
				}*/

				/* InvalidTypeException */
				// Grammar Exception : Datatype does not exist within IsDatatype()
				else if (ex.Message == "InvalidTypeException") {
					Error.InvalidValueType(global_line, global_token);
				}

				/* ParentNotFoundException */
				// Parse Tree Exception : Cursor attempted to move to null or illegal parent
				else if (ex.Message == "ParentNotFoundException") {
					Error.InvalidCloseCurly(global_line, global_token);
				}

				/* DatatypeNotFoundException */
				// Grammar Exception : Datatype does not exist as a special definitions file
				else if (ex.Message == "DatatypeNotFoundException") {
					Error.InvalidKey(global_line, global_token);
				}

				/* FileNotFoundException */
				// Grammar Exception : invalid grammar file path
				// Generic file not found exception
				else if (ex.Message == "FileNotFoundException") {
					PrintColor.Error("{f:Red}Invalid grammar file{r} selected.");
				}

				/*NestedCollectionException */
				// Parse Tree Exception : Collection within Collection
				else if (ex.Message == "NestedCollectionException") {
					Error.NestedCollection(global_line, global_token);
				}

				/* Exception */
				// Generic Exception : Unknown exception
				else {
					Error.Unknown(global_line, global_token, ex.Message);
				}
			}
			/* End */

			// Blank Line : Separate Warnings/Errors from Info Section
			Console.Write("\n");

			// Debug Terminator
			if (_IsDebug("bool_Print_Terminators")) {
				Console.WriteLine(">>>>>End of file | Debug Level ");
			}

			/* Ending Statement */
			// Error Occurred
			if (Error.ErrorOccurred) {
				PrintColor.ColorLinef("{f:Black}{b:Red}Finished with an error.{r}");
			}

			// No Error Occurred
			else {
				// Warning Occurred
				if (Warning.Warnings > 0) {
					PrintColor.ColorLinef("{f:Black}{b:Yellow}Finished with {0} warning(s).{r}", Warning.Warnings.ToString());
				}
				// No Warnings
				else {
					PrintColor.ColorLinef("{f:Black}{b:Green}Finished with 0 warning(s).{r}");
				}

				// Print Total Mission Currency
				PrintColor.InfoLine("Starting Credits: {f:Cyan}{0}{r}", p.StartingCurrency.ToString());
				PrintColor.InfoLine("Total Dropped Credits: {f:Cyan}{0}{r}", p.TotalCurrency.ToString());
				PrintColor.InfoLine("Total Bonus Credits: {f:Cyan}{0}{r}", p.TotalWaveBonus.ToString());
				PrintColor.InfoLine("Maximum Possible Credits: {f:Cyan}{0}{r}", (p.StartingCurrency + p.TotalCurrency + p.TotalWaveBonus).ToString());
			}

			// Blank Line : Separate Ending Statements from Further Option Choices
			Console.Write("\n");

			// Show Next Options
			PrintColor.Colorf("{b:White}{f:Black}F1{r} Show Credit Stats".PadRight(33 + 21)				+ "{b:White}{f:Black}F5{r} Reparse Pop File (Restart)".PadRight(33 + 21)	+ "{b:White}{f:Black}F9{r}  Update Attributes Database".PadRight(33 + 21)		+ "\n");
			PrintColor.Colorf("{b:White}{f:Black}F2{r} Show WaveSpawn Names".PadRight(33 + 21)			+ "{b:White}{f:Black}F6{r} -Unused-".PadRight(33 + 21)						+ "{b:White}{f:Black}F10{r} Set items_game.txt Target".PadRight(33 + 21)		+ "\n");
			PrintColor.Colorf("{b:White}{f:Black}F3{r} Show TFBot Template Names".PadRight(33 + 21)		+ "{b:White}{f:Black}F7{r} -Unused-".PadRight(33 + 21)						+ "{b:White}{f:Black}F11{r} Fullscreen (Windows Default)".PadRight(33 + 21)		+ "\n");
			PrintColor.Colorf("{b:White}{f:Black}F4{r} Show Custom Icons Required".PadRight(33 + 21)	+ "{b:White}{f:Black}F8{r} Open Map Analyzer (BETA)".PadRight(33 + 21)		+ "{b:White}{f:Black}F12{r} Open P3 Code Reference (PDF)".PadRight(33 + 21)		+ "\n");

			// Blank Line : Separate Quit with Options
			Console.Write("\n");

			// Any Key to Quit
			PrintColor.Colorf("{b:White}{f:Black}Any Key{r} Quit");

			// Options Menu Handling
			ConsoleKey key_pressed;
			while (true) { // You should never do this but I need a quick inverse.
				
				// Debug flag auto close after parsing
				if (auto_close) {
					break;
				}

				key_pressed = Console.ReadKey().Key;
				Console.Write("\n");

				// F1 Display Credit Stats
				if (key_pressed == ConsoleKey.F1) {
					PrintColor.InfoLine("===Credit Statistics===");
					p.WriteCreditStats();
				}

				// F2 Display WaveSpawn Stats
				else if (key_pressed == ConsoleKey.F2) {
					PrintColor.InfoLine("===WaveSpawn Names===");
					p.WriteWaveSpawnNames();
				}

				// F3 Display TFBot Templates
				else if (key_pressed == ConsoleKey.F3) {
					PrintColor.InfoLine("===TFBot Template Names===");
					PrintColor.InfoLine("Names are not case sensitive.");
					p.WriteTFBotTemplateNames();
				}

				// F4 Show Custom Icons
				else if (key_pressed == ConsoleKey.F4) {
					PrintColor.InfoLine("===Custom Icons Used===");
					PrintColor.InfoLine("Names are not case sensitive.");
					p.WriteCustomIcons();
				}

				// F5 Map Reparse
				else if (key_pressed == ConsoleKey.F5) {
					System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
					myProcess.StartInfo.FileName = P3_root + "P3.exe";
					myProcess.StartInfo.Arguments = "-pop " + file_path;
					myProcess.Start();
					break;
				}

				// F6 Unused
				else if (key_pressed == ConsoleKey.F6) {
				}

				// F7 Unused
				else if (key_pressed == ConsoleKey.F7) {
				}

				// F8 Map Analyzer
				else if (key_pressed == ConsoleKey.F8) {
					PrintColor.InfoLine("===Analyze Map (.bsp)===");
					PrintColor.InfoLine("Compressed maps are currently not able to be analyzed.");
					string map_path = "";
					try {
						OpenFileDialog ofd = new OpenFileDialog {
							InitialDirectory = Path.GetFullPath(pop_folder),
							Filter = "MvM Map Files|*.bsp"
						};
						ofd.ShowDialog();
						if (ofd.FileName.Length == 0) {
							throw new Exception("NoFile");
						}
						PrintColor.InfoLine("Map: " + ofd.FileName);
						map_path = ofd.FileName;
					}
					catch {
						Error.NoTrigger.FailedDialog();
					}
					if (map_path.Length > 0) {
						MapScraper.Scrape(map_path, out string[] bot_spawns, out string[] logic_relays, out string[] tank_tracks);
						PrintColor.InfoLine("Bot Spawns:");
						foreach (string location in bot_spawns.OrderBy(str => str)) { // List.OrderBy() returns sorted IEnumerable
							PrintColor.InfoLine("\t" + location);
						}
						PrintColor.InfoLine("Logic Relays:");
						foreach (string relay in logic_relays.OrderBy(str => str)) { // List.OrderBy() returns sorted IEnumerable
							PrintColor.InfoLine("\t" + relay);
						}
						PrintColor.InfoLine("Tank Nodes:");
						foreach (string relay in tank_tracks.OrderBy(str => str)) { // List.OrderBy() returns sorted IEnumerable
							PrintColor.InfoLine("\t" + relay);
						}
					}
				}

				// F9 Scrape Items and Attributes Database
				else if (key_pressed == ConsoleKey.F9) {

					string cfg_att_filepath = _INI.Read("items_source_file", "Global");

					// Verify Valid Configuration
					if (cfg_att_filepath.Length == 0 || !File.Exists(cfg_att_filepath)) {
						PrintColor.ErrorNoTrigger("Invalid items_game.txt");
						PrintColor.InfoLine("Please set your items_game.txt with F10");
					}
					else {
						// Attributes
						PrintColor.InfoLine("===Updating Databases===");
						using (AttributesScraper s = new AttributesScraper()) {

							PrintColor.InfoLine("> Attributes Database");
							PrintColor.InfoLine("Old version: {f:Yellow}{0}{r}", s.Version);
							s.Scrape(cfg_att_filepath);
							PrintColor.InfoLine("New version: {f:Green}{0}{r}", s.Version);
						}

						// Items
						using (ItemScraper s = new ItemScraper()) {

							PrintColor.InfoLine("> Items Database");
							PrintColor.InfoLine("Old version: {f:Yellow}{0}{r}", s.Version);
							s.Scrape(cfg_att_filepath);
							PrintColor.InfoLine("New version: {f:Green}{0}{r}", s.Version);
						}
					}
				}

				// F10 Set items_game.txt
				else if (key_pressed == ConsoleKey.F10) {
					PrintColor.InfoLine("===Set items_game.txt Location===");
					PrintColor.InfoLine(@"Please open your items_game.txt at");
					PrintColor.InfoLine(@".\steamapps\Team Fortress 2\tf\scripts\items\items_game.txt");

					// File Dialog
					try {
						OpenFileDialog ofd = new OpenFileDialog {
							InitialDirectory = Path.GetFullPath(pop_folder),
							Filter = "|items_game.txt"
						};
						ofd.ShowDialog();
						_INI.Write("items_source_file", "\"" + ofd.FileName + "\"", "Global");
						if (ofd.FileName.Length == 0) {
							throw new Exception("NoFile");
						}
						PrintColor.InfoLine("Path: " + ofd.FileName);
						PrintColor.InfoLine("Successfully set location!");
					}
					catch {
						Error.NoTrigger.FailedDialog();
					}
				}

				// F11 Fullscreen
				// Windows default key, not reimplemented. I can't change this

				// F12 Reference PDF
				else if (key_pressed == ConsoleKey.F12) {
					PrintColor.InfoLine("Opening P3 Reference PDF");
					System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
					myProcess.StartInfo.FileName = P3_root + "P3_Reference.pdf";
					myProcess.Start();
				}

				// Exit on Any Key
				else {
					break;
				}
			}

			{ } // Debug Breakpoint
		}

	}

}
