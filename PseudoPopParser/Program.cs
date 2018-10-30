using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PseudoPopParser {

	class Program {

		private static IniFile _INI;
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

			// Version Message
			PrintColor.InfoLine("P3 v1.1.0");

			string P3_root = AppDomain.CurrentDomain.BaseDirectory;
			_INI = new IniFile(P3_root + @"config.ini");
			string file_path = "";
			string grammar_file = P3_root + "grammar.owo";
			string datatypes_folder = P3_root;
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
			}

			// Get file_path if not defined in launch
			while (!Regex.IsMatch(file_path, @"\.pop$")) {
				Console.Write("Enter the path to your Pop file: ");
				file_path = Console.ReadLine();

				if (!Regex.IsMatch(file_path, @"\.pop$")) {
					PrintColor.ErrorNoTrigger("Pop file must have *.pop file extension.");
				}
			}

			// Get pop file's containing directory
			string pop_folder = Regex.Match(file_path, @"^.*[\/\\]").ToString(); // Regex: Match everything up to last / or \

			// Get pop file's name
			string pop_file_name = Regex.Match(file_path, @"[\w-]+\.pop").ToString();

			// Init Parser
			PopParser p = new PopParser(datatypes_folder, pop_folder);
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
				PrintColor.ErrorNoTrigger("Could not open Pop file.");
				PrintColor.ColorLinef("Press Any Key to quit.");
				Console.ReadKey();
				return;
			}

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
									if (p.ConfigReadBool("bool_early_end_wave_schedule", "Global") && (i < token_list.Count * 99 / 100 && pt.Current.Value[2] == "NONE")) {
										PrintColor.Warn("Possible premature end of WaveSchedule detected near '{f:Yellow}{0}{r}'", global_line, global_token);
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

											PrintColor.Warn("Using {f:Cyan}When{r} with {f:Cyan}MinInterval{r} and {f:Cyan}MaxInterval{r} may stop spawning midwave", global_line);
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
									throw new Exception("CollectionWithinCollectionException");
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
				}
				PrintColor.InfoLine("\tDone parsing Pop File - {f:Cyan}{0}{r}", pop_file_name);
			}
			catch (Exception ex) {

				if (ex.Message == "UnknownSymbolException") {

					// Lexer Exception : No space found before a "//" single-line comment token
					if (Regex.IsMatch(global_token, @"\/\/.*[\s]*")) {
						PrintColor.Error("Bad comment found near '{f:Red}{0}{r}'", global_line, global_token);
						p.PotentialFix("Insert a space between \"" + Regex.Replace(global_token, @"\/\/.*[\s]*", "") + "\" and \"//\"");
					}

					// Generic unknown symbol exception
					else {
						PrintColor.Error("{f:Red}Invalid symbol{r} found near '{f:Red}{0}{r}'", global_line, global_token);
					}

				}

				/* IllegalIdentifierException */
				// Lexer Exception : $ANY_VALID_STRING contains one of the following symbols { } " #base #include
				else if (ex.Message == "IllegalIdentifierException") {
					PrintColor.Error("{f:Red}Invalid name{r} found near '{f:Red}{0}{r}'", global_line, global_token);
				}

				/* InvalidTypeException */
				// Grammar Exception : Datatype does not exist within IsDatatype()
				else if (ex.Message == "InvalidTypeException") {
					PrintColor.Error("{f:Red}Invalid value{r} found near '{f:Red}{0}{r}'", global_line, global_token);
				}

				/* ParentNotFoundException */
				// Parse Tree Exception : Cursor attempted to move to null or illegal parent
				else if (ex.Message == "ParentNotFoundException") {
					PrintColor.Error("{f:Red}Invalid Closing Curly Bracket{r} found: '{f:Red}{0}{r}'", global_line, global_token);
					//p.PotentialFix("Recount and remove excess Close Curly Brackets");
				}

				/* DatatypeNotFoundException */
				// Grammar Exception : Datatype does not exist as a special definitions file
				else if (ex.Message == "DatatypeNotFoundException") {
					PrintColor.Error("{f:Red}Invalid key{r} found near '{f:Red}{0}{r}'", global_line, global_token);
				}

				/* FileNotFoundException */
				// Grammar Exception : invalid grammar file path
				// Generic file not found exception
				else if (ex.Message == "FileNotFoundException") {
					PrintColor.Error("{f:Red}Invalid grammar file{r} selected.");
				}

				/* CollectionWithinCollectionException */
				// Parse Tree Exception : Collection within Collection
				else if (ex.Message == "CollectionWithinCollectionException") {
					PrintColor.Error("Cannot have Collection within Collection: '{f:Red}{0}{r}'", global_line, global_token);
					p.PotentialFix("Cannot have Squad, Mob, or RandomChoice within a Squad, Mob, or RandomChoice.");
					p.PotentialFix("Valve has not implemented a recursive spawner.");
				}

				/* Exception */
				// Generic Exception : Unknown exception
				else {
					PrintColor.Error("{f:Cyan}Unknown{r} exception '{f:Red}{1}{r}' near '{f:Red} {0}{r}'", global_line, global_token, ex.Message);
					PrintColor.WriteLineColor("Please contact the developer regarding this error", ConsoleColor.Blue);
					PrintColor.WriteLineColor("Contact info can be found in the README", ConsoleColor.Blue);
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
				PrintColor.ColorLinef("{f:Black}{b:Red}Finished with an error.{r}");
			}

			// No Error Occurred
			else {
				// Warning Occurred
				if (p.Warnings > 0) {
					PrintColor.ColorLinef("{f:Black}{b:Yellow}Finished with {0} warning(s).{r}", p.Warnings.ToString());
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

			// Blank Line : Separate Ending Statements with Further Option Choices
			Console.Write("\n");

			// Show Next Options
			PrintColor.WriteColor("F1", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" Show Credit Stats");

			PrintColor.WriteColor("F2", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" Show WaveSpawn Names");

			PrintColor.WriteColor("F3", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" Update Attributes");

			PrintColor.WriteColor("F4", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" Update Items");

			PrintColor.WriteColor("F5", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" Show TFBot Template Names");

			PrintColor.WriteColor("F6", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" How to Calculate Credits");

			PrintColor.WriteColor("F7", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" Show Used Custom Icons");

			PrintColor.WriteColor("F10", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" Set items_game.txt Location");

			PrintColor.WriteColor("F11", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" Enter Fullscreen (Windows Default)");

			PrintColor.WriteColor("F12", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" Common Conventions Enforcement");

			// Blank Line Separates Quit with Options
			Console.Write("\n");

			PrintColor.WriteColor("Any Key", ConsoleColor.White, ConsoleColor.Black);
			PrintColor.WriteLineColor(" Quit");

			// Options Menu Handling
			ConsoleKey key_pressed;
			while (true) { // You should never do this but I need a quick inverse.
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

				// F3 Scrape Attributes | Update item_attributes.owo
				else if (key_pressed == ConsoleKey.F3) {
					PrintColor.InfoLine("===Updating Attributes Database===");
					using (Scraper s = new Scraper()) {
						string cfg_att_filepath = _INI.Read("items_source_file", "Global");

						// Verify Valid Configuration
						if (cfg_att_filepath.Length == 0 || !File.Exists(cfg_att_filepath)) {
							PrintColor.ErrorNoTrigger("Invalid items_game.txt");
							PrintColor.InfoLine("Please set your items_game.txt with F10");
						}

						// Scraper Operations
						else {
							PrintColor.InfoLine("Old version is " + s.VersionAttr);
							s.ScrapeAttributes(cfg_att_filepath);
						}
					}
				}

				// F4 Scrape Items | Update item_db.owo
				else if (key_pressed == ConsoleKey.F4) {
					PrintColor.InfoLine("===Updating Item Database===");
					using (Scraper s = new Scraper()) {
						string cfg_att_filepath = _INI.Read("items_source_file", "Global");

						// Verify Valid Configuration
						if (cfg_att_filepath.Length == 0 || !File.Exists(cfg_att_filepath)) {
							PrintColor.ErrorNoTrigger("Invalid items_game.txt");
							PrintColor.InfoLine("Please set your items_game.txt with F10");
						}

						// Scraper Operations
						else {
							PrintColor.InfoLine("Old version is " + s.VersionItem);
							s.ScrapeItems(cfg_att_filepath);
						}
					}
				}

				// F5 Display TFBot Templates
				else if (key_pressed == ConsoleKey.F5) {
					PrintColor.InfoLine("===TFBot Template Names===");
					PrintColor.InfoLine("Names are not case sensitive.");
					p.WriteTFBotTemplateNames();
				}

				// F6 How to Calculate Total Credits
				else if (key_pressed == ConsoleKey.F6) {
					PrintColor.InfoLine("===How to Calculate Total Credits===");
					PrintColor.InfoLine("Calculate credits {f:Cyan}dropped{r} by adding up");
					PrintColor.InfoLine("  every WaveSpawn's {f:Cyan}TotalCurrency{r} in a Wave.");
					PrintColor.InfoLine("A bonus of maximum {f:Cyan}$100{r} is awarded on completion of wave if");
					PrintColor.InfoLine("  it is {f:cyan}not the final wave{r} and {f:cyan}all credits{r} are picked up.");
					PrintColor.InfoLine("A half bonus of {f:Cyan}$50{r} is awarded on completion of wave if it is");
					PrintColor.InfoLine("  {f:cyan}not the final wave{r} and missed credits is between {f:cyan}$1 and $50{r}.");
				}

				// F7 Show Used Custom Icons
				else if (key_pressed == ConsoleKey.F7) {
					PrintColor.InfoLine("===Custom Icons Used===");
					PrintColor.InfoLine("Names are not case sensitive.");
					p.WriteCustomIcons();
				}

				// F10 Set items_game.txt
				else if (key_pressed == ConsoleKey.F10) {
					PrintColor.InfoLine("===Set items_game.txt Location===");
					PrintColor.InfoLine(@"Please open your items_game.txt at");
					PrintColor.InfoLine(@".\Team Fortress 2\tf\scripts\items\items_game.txt");

					// File Dialog
					try {
						OpenFileDialog ofd = new OpenFileDialog();
						ofd.ShowDialog();
						_INI.Write("items_source_file", "\"" + ofd.FileName + "\"", "Global");
						if (ofd.FileName.Length == 0) {
							throw new Exception("NoFile");
						}
						PrintColor.InfoLine("Path: " + ofd.FileName);
					}
					catch {
						PrintColor.Error("Failed to get file by dialog.");
					}
				}

				// F12 Notice: Conventions
				else if (key_pressed == ConsoleKey.F12) {
					PrintColor.InfoLine("===Common Conventions Enforcement===");
					PrintColor.InfoLine("P3 enforces {f:Cyan}common conventions{r}. The list of conventions is as follows.");
					PrintColor.InfoLine("Templates defined {f:Cyan}{0} usage{r}.", "before");
					PrintColor.InfoLine("Templates imported {f:Cyan}{0} ItemAttribute modification{r}.", "before");
					PrintColor.InfoLine("Items must be given to TFBot {f:Cyan}{0} ItemAttributes modification{r}.", "before");
					PrintColor.InfoLine("Do not use {f:Cyan}WaveSpawn Templates{r}.");
					PrintColor.InfoLine("TotalCurrency must be {f:Cyan}greater than 0{r}. {f:Yellow}(Configurable){r}");
					PrintColor.InfoLine("Support cannot be {f:Cyan}limited{r}.");
					PrintColor.InfoLine("Warnings (or possibly errors) are given when a convention is broken.");
					PrintColor.InfoLine("This list may shrink as P3 updates to accomodate outlier creators.");
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
