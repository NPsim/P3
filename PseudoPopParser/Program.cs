using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PseudoPopParser {

	class Program {

		private static IniFile _INI = new IniFile(@"config.ini");
		private static Dictionary<string, string> _CONFIGURATION = new Dictionary<string, string>();

		static bool _IsDebug(string key) {
			string[] true_values = { "1", "YES", "TRUE" };
			if (_CONFIGURATION.ContainsKey(key)) {
				return true_values.Contains(_CONFIGURATION[key].ToUpper());
			}
			else if (_INI.KeyExists(key, "Debug")) {
				_CONFIGURATION.Add(key, _INI.Read(key, "Debug"));
				return true_values.Contains(_INI.Read(key, "Debug").ToUpper());
			}
			return false;
		}

		[STAThread]
		static void Main(string[] args) {

			// Debug Terminator
			if (_IsDebug("bool_Print_Terminators")) {
				Console.WriteLine(">>>>>Start of file | Debug Level ");
			}
			string P3_root = AppDomain.CurrentDomain.BaseDirectory;
			string file_path = "";
			string grammar_file = P3_root + "grammar.owo";
			string datatypes_folder = P3_root;
			string[] file = null;
			bool bypass_print_config = false;

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
			}

			// Get pop file's containing directory
			string pop_folder = Regex.Match(file_path, @"^.*[\/\\]").ToString(); // Regex: Match everything up to last / or \

			// Init Parser
			PopParser p = new PopParser(datatypes_folder, pop_folder);
			ParseTree pt = new ParseTree(grammar_file);

			// Debug Print Config
			if (_IsDebug("bool_Print_Config") || bypass_print_config) {

				// TODO: Actually make a string[] Key() method for IniFile

				// Global Config
				p.DebugLine("\t[Global]");
				p.DebugLine("items_source_file : " + _INI.Read("items_source_file", "Global"));
				p.DebugLine("currency_multiple_warning : " + _INI.Read("currency_multiple_warning", "Global"));
				p.DebugLine("tank_warn_maximum : " + _INI.Read("tank_warn_maximum", "Global"));
				p.DebugLine("tank_warn_minimum : " + _INI.Read("tank_warn_minimum", "Global"));
				p.DebugLine("bot_health_multiple : " + _INI.Read("bot_health_multiple", "Global"));
				p.DebugLine("tank_health_multiple : " + _INI.Read("tank_health_multiple", "Global"));
				p.DebugLine("bool_tank_name_tankboss : " + _INI.Read("bool_tank_name_tankboss", "Global"));

				// Debug Config
				p.DebugLine("\t[Debug]");
				p.DebugLine("bool_Print_Config : " + _INI.Read("bool_Print_Config", "Debug"));
				p.DebugLine("bool_Print_Token_Lookback : " + _INI.Read("bool_Print_Token_Lookback", "Debug"));
				p.DebugLine("bool_Print_Terminators : " + _INI.Read("bool_Print_Terminators", "Debug"));
				p.DebugLine("bool_Print_Tokens : " + _INI.Read("bool_Print_Tokens", "Debug"));
				p.DebugLine("bool_Print_Token_Operations : " + _INI.Read("bool_Print_Token_Operations", "Debug"));
				p.DebugLine("bool_Print_PT_Cursor_Traversal : " + _INI.Read("bool_Print_PT_Cursor_Traversal", "Debug"));

				// End of Configuration
				p.DebugLine("\tEnd Config");
			}

			// Get file_path if not defined in launch
			if (file_path == "") {
				Console.Write("Enter the path to your popfile: ");
				file_path = Console.ReadLine();
			}

			/* Begin */

			// Populate file[] var
			file = File.ReadAllLines(file_path);
			p.InfoLine("Pop File - " + file_path);

			// Modify strings
			for (int i = 0; i < file.Length; i++) {
				file[i] = Regex.Replace(file[i], @"(\s|\/+|^)\/\/.*[\s]*", "");     // Remove Comments
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
			string look_back_token = "";
			string look_ahead_buffer_node = "";
			string template_name = "";
			try {

				// Iterate by Line List
				for (int i = 0; i < token_list.Count; i++) {

					// Iterate by Token List of Line
					for (int j = 0; j < token_list[i].Length; j++) {

						// Only Scan Real Tokens
						if (!string.IsNullOrWhiteSpace(token_list[i][j])) {

							// TODO Add WaveSpawn template redirection; current catastrophic failure when parsing wavespawn template
							/* TreeNode<string[]>.Value = {
							*		Index 0 : Type : "Collection", "Key", "Value"
							*		Index 1 : Name : "WaveSchedule", "Attribute", "AlwaysCrit"
							*		Index 2 : Parent Name : "Top Level", "WaveSchedule", "Attribute"
							*		Index 3 : Datatype : "STRING", "UNSIGNED INTEGER", "SKILL"
							*		Index 4 : Layer : "0", "2", "3"
							*		Index 5 : Can Be Any Name : "TRUE" , "FALSE"
							*		Index 6 : Required : "TRUE", "FALSE"
							* }
							*/

							// Tokenized Information
							int line = i;
							global_line = i; // Redundant
							string token = token_list[i][j];
							global_token = token_list[i][j]; // Redundant
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
									string_builder.Add(token);
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
										p.Warn("Possible premature end of WaveSchedule detected near ", global_line, global_token);
										p.PotentialFix("Remove additional lines after end of WaveSchedule");
										p.PotentialFix("Recount Curly Brackets");
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

											p.Warn("Using \"When\" with \"MinInterval\" and \"MaxInterval\" may stop spawning midwave", global_line);
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
										
										// Detect Default Template
										string[] default_templates = { "ROBOT_STANDARD.POP", "ROBOT_GIANT.POP", "ROBOT_GATEBOT.POP" };
										bool is_default = false;
										if (default_templates.Contains(token.ToUpper())) {
											token = P3_root + "base_templates\\" + token;
											is_default = true;
										}

										p.ParseBase(token, is_default);
									}
									// Parse Key and Value
									else {
										p.ParseKeyValue(look_back_token, token, line, pt.ParentValue[1], template_name);
									}

									{ }

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
				}
			}
			catch (Exception ex) {

				if (ex.Message == "UnknownSymbolException") {

					// Lexer Exception : No space found before a "//" single-line comment token
					if (Regex.IsMatch(global_token, @"\/\/.*[\s]*")) {
						p.Error("Bad comment found near ", global_line, global_token);
						p.PotentialFix("Insert a space between \"" + Regex.Replace(global_token, @"\/\/.*[\s]*", "") + "\" and \"//\"");
					}

					// Generic unknown symbol exception
					else {
						p.Error("Unknown symbol found near ", global_line, global_token);
					}

				}

				/* IllegalIdentifierException */
				// Lexer Exception : $ANY_VALID_STRING contains one of the following symbols { } " #base #include
				else if (ex.Message == "IllegalIdentifierException") {
					p.Error("Invalid name found near ", global_line, global_token);
				}

				/* InvalidTypeException */
				// Grammar Exception : Datatype does not exist within IsDatatype()
				else if (ex.Message == "InvalidTypeException") {
					p.Error("Invalid value found near ", global_line, global_token);
				}

				/* ParentNotFoundException */
				// Parse Tree Exception : Cursor attempted to move to null or illegal parent
				else if (ex.Message == "ParentNotFoundException") {
					p.Error("Invalid Closing Curly Bracket found: ", global_line, global_token);
					p.PotentialFix("Recount and remove excess Close Curly Brackets");
				}

				/* DatatypeNotFoundException */
				// Grammar Exception : Datatype does not exist as a special definitions file
				else if (ex.Message == "DatatypeNotFoundException") {
					p.Error("Invalid key found near ", global_line, global_token);
				}

				/* FileNotFoundException */
				// Grammar Exception : invalid grammar file path
				// Generic file not found exception
				else if (ex.Message == "FileNotFoundException") {
					p.Error("Invalid grammar file selected.");
				}

				/* Exception */
				// Generic Exception : Unknown exception
				else {
					p.Error("Unknown exception \'" + ex.Message + "\' near ", global_line, global_token);
					p.WriteLineColor("Please contact the developer regarding this error", ConsoleColor.Blue);
					p.WriteLineColor("Contact info can be found in the README", ConsoleColor.Blue);
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
			if (p.ErrorOccurred) {
				p.WriteLineColor("Finished with an error.", ConsoleColor.Red, ConsoleColor.Black);
			}

			// No Error Occurred
			else {
				ConsoleColor any_warning_back = ConsoleColor.Green;
				ConsoleColor any_warning_fore = ConsoleColor.Black;

				// Warning Occurred
				if (p.Warnings > 0) {
					any_warning_back = ConsoleColor.Yellow;
					any_warning_fore = ConsoleColor.Black;
				}

				// Print Total Warnings
				p.WriteLineColor("Finished with " + p.Warnings + " warning(s).", any_warning_back, any_warning_fore);

				// Print Total Mission Currency
				p.InfoLine("Starting Credits: " + p.StartingCurrency);
				p.InfoLine("Total Dropped Credits: " + p.TotalCurrency);
				p.InfoLine("Total Bonus Credits: " + p.TotalWaveBonus);
				p.InfoLine("Maximum Possible Credits: " + (p.StartingCurrency + p.TotalCurrency + p.TotalWaveBonus));
			}

			// Blank Line : Separate Ending Statements with Further Option Choices
			Console.Write("\n");

			// Show Next Options
			p.WriteColor("F1", ConsoleColor.White, ConsoleColor.Black);
			p.WriteLineColor(" Credit Stats");

			p.WriteColor("F2", ConsoleColor.White, ConsoleColor.Black);
			p.WriteLineColor(" WaveSpawn Names");

			p.WriteColor("F3", ConsoleColor.White, ConsoleColor.Black);
			p.WriteLineColor(" Update Attributes");

			p.WriteColor("F4", ConsoleColor.White, ConsoleColor.Black);
			p.WriteLineColor(" Update Items");

			// Blank Line Separates Quit with Options
			Console.Write("\n");

			p.WriteColor("Any Key", ConsoleColor.White, ConsoleColor.Black);
			p.WriteLineColor(" Quit");

			// Dev message
			p.WriteLineColor("[ALPHA] P3 DEVELOPMENT BUILD", ConsoleColor.Green, ConsoleColor.Black);

			// Options Menu Handling
			ConsoleKey key_pressed;
			while (true) { // You should never do this but I need a quick inverse.
				key_pressed = Console.ReadKey().Key;
				Console.Write("\n");

				// F1 Display Credit Stats
				if (key_pressed == ConsoleKey.F1) {
					p.InfoLine("===Writing Credit Statistics===");
					p.WriteCreditStats();
				}

				// F2 Display WaveSpawn Stats
				else if (key_pressed == ConsoleKey.F2) {
					p.InfoLine("===Writing WaveSpawn Statistics===");
					p.WriteWaveSpawnNames();
				}

				// F3 Scrape Attributes
				else if (key_pressed == ConsoleKey.F3) {
					p.InfoLine("===Updating Attributes Database===");
					using (Scraper s = new Scraper(p)) {
						string cfg_att_filepath = _INI.Read("items_source_file", "Global");

						{ } // Debug Breakpoint

						// Verify Valid Configuration
						if (cfg_att_filepath.Length == 0 || !File.Exists(cfg_att_filepath)) {

							p.InfoLine("Please specify your items_game.txt");

							try {
								OpenFileDialog ofd = new OpenFileDialog();
								ofd.ShowDialog();
								cfg_att_filepath = ofd.FileName;
								_INI.Write("items_source_file", "\"" + ofd.FileName + "\"", "Global");
								p.InfoLine("Input by Dialog: " + ofd.FileName);
							}
							catch {
								p.Error("Failed to get file by dialog.");
								p.Info("Input your path to items_game.txt: ");
								cfg_att_filepath = Console.ReadLine();
							}
						}

						// Scraper Operations
						if (File.Exists(cfg_att_filepath)) {
							p.InfoLine("Old version is " + s.Version);
							s.ScrapeAttributes(cfg_att_filepath);
						}
						else {
							p.Error("File does not exist");
						}
					}
				}

				// F4 Scrape Items
				else if (key_pressed == ConsoleKey.F4) {
					p.InfoLine("===Updating Item Database===");
					using (Scraper s = new Scraper(p)) {
						string cfg_att_filepath = _INI.Read("items_source_file", "Global");

						Console.Write("");

						// Verify Valid Configuration
						if (cfg_att_filepath.Length == 0 || !File.Exists(cfg_att_filepath)) {

							p.InfoLine("Please specify your items_game.txt");

							try {
								OpenFileDialog ofd = new OpenFileDialog();
								ofd.ShowDialog();
								cfg_att_filepath = ofd.FileName;
								_INI.Write("items_source_file", "\"" + ofd.FileName + "\"", "Global");
								p.InfoLine("Input by Dialog: " + ofd.FileName);
							}
							catch {
								p.Error("Failed to get file by dialog.");
								p.Info("Input your path to items_game.txt: ");
								cfg_att_filepath = Console.ReadLine();
							}
						}

						// Scraper Operations
						if (File.Exists(cfg_att_filepath)) {
							p.InfoLine("Old version is " + s.Version);
							s.ScrapeItems(cfg_att_filepath);
						}
						else {
							p.Error("File does not exist");
						}
					}
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
