using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {

	class PopParser {

		// TODO organize this, pls
		private static IniFile _INI;
		private static string pop_directory = "";
		private static string pop_name = "";
		private static string base_name = "";
		private static string datatypes_folder_path = "";
		private static List<List<int>> wave_credits_list = new List<List<int>>();
		private static int total_waves = 0;
		private static Dictionary<string, string> attribute_pairs = new Dictionary<string, string>();
		private static List<List<string>> tfbot_template_items = new List<List<string>>();
		private static List<string> wavespawn_templates = new List<string>();
		private static List<List<string>> wave_wavespawn_names = new List<List<string>>();
		private static List<string> used_wavespawn_names = new List<string>();
		private static List<int> used_wavespawn_lines = new List<int>();
		private static List<string[]> attributes_list = new List<string[]>();
		private static List<string> tfbot_items = new List<string>();
		private static bool suppress_write_main = false;
		private static List<string> icons_list = new List<string>();

		private static string[] DATATYPES = {
			"$any_valid_string",		// Special
			"SUPPORT TYPE",				// Collection, special case 1
			"BOT NAME",					// Cannot use special characters
			"STRING",					// Primitive
			"INTEGER",					// Primitive
			"FLOAT",					// Primitive
			"SKILL",					// Collection
			"WEAPON RESTRICTION",		// Collection
			"BEHAVIOR MODIFIER",		// Collection
			"UNSIGNED INTEGER",			// Primitive
			"WHERE",
			"TEMPLATE NAME",
			"$item_attribute",
			"$item_attribute_value",
			"$char_attribute",
			"$char_attribute_value",
			"ITEM NAME",
			"ATTRIBUTE NAME",			// Collection
			"CLASS NAME",				// Collection
			"SENTRYGUN LEVEL",			// Collection
			"FILE",
			"EVENT POP FILE",			// Collection
			"BOOLEAN",					// Primitive Collection
			"MISSION OBJECTIVE",		// Collection
			"NAV FILTER AREA",			// Collection
		};

		// Constructors
		public PopParser() { }
		public PopParser(string datatypes_folder, string pop_folder, string pop_file_name) {
			datatypes_folder_path = datatypes_folder;
			_INI = new IniFile(datatypes_folder + @"config.ini");
			pop_name = pop_file_name;
			pop_directory = pop_folder;
			SetupAttributes();
			SetupItems();
		}

		// Setup Attributes Database
		private void SetupAttributes() {
			string db_file = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\item_attributes.uwu";
			if (!File.Exists(db_file)) {
				//PrintColor.Error("Item attributes db does not exist!");
				Error.NoTrigger.MissingDatabase();
				return;
			}
			string[] db = File.ReadAllLines(db_file);
			for (int line = 1; line < db.Length; line += 3) {
				attributes_list.Add(new string[] {
					db[line],
					db[line + 1].Substring(1, db[line + 1].Length - 1),
					db[line + 2].Substring(1, db[line + 2].Length - 1)
				});
			}
		}

		// Setup Items Database
		private void SetupItems() {
			string db_file = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\item_db.uwu";
			if (!File.Exists(db_file)) {
				//PrintColor.Error("Items db does not exist!");
				Error.NoTrigger.MissingDatabase();
				return;
			}
			string[] db = File.ReadAllLines(db_file);
			for (int i = 1; i < db.Count(); i++) {
				string line = db[i]; // TODO make use of all db data
				if (Regex.IsMatch(line, @"^\S")) {
					ItemDatabase.Add(line);
				}
			}
		}

		// Read config.ini
		public int ConfigRead(string key, string section = "Global") {
			if (_INI.KeyExists(key, section)) {
				return Int32.Parse(_INI.Read(key, section));
			}
			return -1;
		}

		// Read config.ini boolean
		public bool ConfigReadBool(string key, string section = "Global") {
			string[] true_values = { "1", "YES", "TRUE" };
			return true_values.Contains(_INI.Read(key, section).ToUpper());
		}

		private bool _IsTemplateDebug(string key) {
			return ConfigReadBool(key, "Debug");
		}

		// Exists in 2D String List
		private bool ExistsTwoDimension(List<List<string>> list, string value) {
			foreach (List<string> sublist in list) {
				foreach (string member in sublist) {
					if (member.ToUpper() == value.ToUpper()) {
						return true;
					}
				}
			}
			return false;
		}

		// Print Formatted Contents of Credits List
		public void WriteCreditStats() {
			PrintColor.InfoLine("Bonus not included in calculation.");
			for (int i = 1; i <= wave_credits_list.Count; i++) {
				List<int> wave_credits = wave_credits_list[i - 1];
				string write_string = "";
				foreach (int credits in wave_credits) {
					write_string += " + " + credits.ToString();
				}
				PrintColor.InfoLine("W" + i + ": " + wave_credits.Sum() + " = " + write_string.Substring(Math.Min(write_string.Length, 3)));
			}
			Console.Write("\n");
		}

		// Print Formatted Contents of WaveSpawn Names List
		public void WriteWaveSpawnNames() {
			for (int i = 1; i <= wave_wavespawn_names.Count; i++) {
				PrintColor.InfoLine("Wave " + i);

				List<string> wave_names = wave_wavespawn_names[i - 1];
				foreach (string name in wave_names) {
					PrintColor.InfoLine("\t" + name);
				}
			}
		}

		// Print Formatted Contents of TFBot_Template_Items
		public void WriteTFBotTemplateNames() {
			// Build lists to sort
			string current_file = "";
			List<string> base_names = new List<string>();
			List<List<string>> base_templates = new List<List<string>>();
			foreach (List<string> items_list in tfbot_template_items) { // List.OrderBy() returns sorted IEnumerable
				if (current_file != items_list[1]) {
					current_file = items_list[1];
					base_names.Add(items_list[1]);
					base_templates.Add(new List<string> {
						items_list[0]
					});
				}
				else {
					base_templates.Last().Add(items_list[0]);
				}
			}
			
			// Print sorted lists
			for(int i = 0; i < base_templates.Count; i++) {
				PrintColor.InfoLine("File: " + base_names[i]);
				List<string> template_list = base_templates[i];
				foreach (string template in template_list.OrderBy(str => str)) {
					PrintColor.InfoLine("\t" + template);
				}
			}
		}

		// Print Used Custom Icons
		public void WriteCustomIcons() {
			List<string> icons_list_distinct = icons_list.Distinct().ToList();

			// Remove default icons
			string file_path = datatypes_folder_path + "base_templates\\default_icons.txt";
			List<string> default_icons = new List<string>(File.ReadAllLines(file_path));
			foreach(string icon in default_icons) {
				icons_list_distinct.Remove(icon);
			}

			// Print Contents
			foreach(string icon in icons_list_distinct.OrderBy(str => str)) { // List.OrderBy() returns sorted IEnumerable
				if (icon == "scout_sunstick") {
					PrintColor.InfoLine("\t{0} - used in Valve template but has no default icon", icon);
				}
				else {
					PrintColor.InfoLine("\t{0}", icon);
				}
			}
		}

		// Parse Collections
		public void ParseCollection(string token, int line = -1, string parent = "null", string any_string_token = "null") {
			token = token.ToUpper();
			switch (token) {
				case "WAVE{}":

					// Increment wave counter
					total_waves++;

					// New credits list
					wave_credits_list.Add(new List<int>());

					// Separate wavespawn names
					wave_wavespawn_names.Add(new List<string>());

					break;

				case "TFBOT{}%":

					// Add TFBot Template Name to Local Database
					if (parent == "Templates{}") {
						List<string> new_template = new List<string> {
							any_string_token.ToUpper()
						};
						
						// Record base name
						if (base_name.Length > 0) {
							new_template.Add(base_name);
						}
						else {
							new_template.Add(pop_name);
						}

						tfbot_template_items.Add(new_template);
					}

					break;

					// TODO : Add more cases
			}
		}

		// Trigger Collection End
		public void ParseCollectionEnd(string token, int line = -1, string parent = "null") {
			token = token.ToUpper();
			switch (token) {
				case "WAVE{}":

					// Warn wave credits nonmultiple
					int wave_credits = wave_credits_list.Last().Sum();
					int credits_multiple = ConfigRead("int_warn_credits_multiple");
					if (!IsMultiple(wave_credits, credits_multiple)) {
						Warning.CreditMultiple(wave_credits, total_waves, credits_multiple);
					}

					break;

				case "ANY_VALID_STRING{}%": // End of Entire Schedule Second Pass Parsing
					// Second pass parsing does not require look ahead or look behind due to everything's already looked at.

					// Warn physical money counter credits >30000
					int total_credits = StartingCurrency + TotalCurrency + TotalWaveBonus;
					if (total_credits > 30000) {
						Warning.TotalCreditGreater30000(total_credits);
					}

					// Compare WaveSpawn Names
					for(int i = 0; i < used_wavespawn_names.Count(); i++) {
						string waitforname = used_wavespawn_names[i];
						if (!ExistsTwoDimension(wave_wavespawn_names, waitforname)) {
							Warning.WaitForAllMissing(used_wavespawn_lines[i], waitforname);
						}
					}

					break;

				case "TFBOT{}%":

					// Refresh Given Items
					tfbot_items.Clear();

					break;

					// TODO : Add more cases
			}
		}

		private string UnQuote(string token) {
			if (Regex.IsMatch(token, "(\\\".*\\\")")) { // If token is surrounded in quotes, must be both start and end.
				return Regex.Replace(token, "\"", "");
			}
			return token;
		}

		// Parse Attribute
		public void ParseAttribute(string key, string value, int line = -1) {

			Double.TryParse(value, out double value_double);
			string[] sentinel_array = { "-1", "-1", "-1" };
			string[] attribute = sentinel_array;

			// Search database for key
			foreach (string[] find in attributes_list) {
				if (key.ToUpper() == find[0].ToUpper()) {
					attribute = find;
					break;
				}
			}

			// Key does not exist in database
			if (Array.Equals(attribute, sentinel_array)) {
				Warning.InvalidAttributeKey(line, key);
				return;
			}

			// Lint
			string name = attribute[0];
			string form = attribute[1];
			string type = attribute[2];

			/*	Forms
			 * null_value
			 * value_is_additive
             * value_is_additive_percentage
             * value_is_percentage
             * value_is_inverted_percentage
             * value_is_or // This is an integer boolean
             * value_is_particle_index
             * value_is_date
             * value_is_account_id
             * value_is_item_def
             * value_is_from_lookup_table
             * value_is_killstreakeffect_index
             * value_is_killstreak_idleeffect_index
			 * */

			/*	Types
			 * FLOAT
			 * STRING
			 * null_type
			 * */

			// Check Type
			/*if (type == "FLOAT" && !Regex.IsMatch(UnQuote(value), @"^(-|)(\d*)(\.|)(\d*|)$")) { // Regex !match decimal number
				Warning.InvalidNumberValue(line, key, value);
			}*/

			/* Check Bad Float Values */

			if (type == "FLOAT") {

				// Adding 0
				if (value_double == 0.0 && (form == "value_is_additive" || form == "value_is_additive_percentage")) {
					Warning.AttributeAdd0(line, key, value);
				}
				// Multiplying by 100%
				else if (value_double == 1.0 && (form == "value_is_percentage" || form == "value_is_inverted_percentage")) {
					Warning.AttributeMultiply1(line, key, value);
				}
				// Invalid Boolean
				else if (!(value == "0" || value == "1") && form == "value_is_or") {
					Warning.AttributeOnly1Or0(line, key, value);
				}
			}
		}

		// Parse Template Pop FIle
		public void ParseBase(string base_file, bool default_template = false) { // I know it's terrible.
			string template_file_name = Regex.Match(base_file, @"[\w-]+\.pop").ToString();
			base_name = template_file_name;
			try {
				if (ConfigReadBool("bool_skip_base_template")) {
					PrintColor.InfoLine("Skipping Base - {f:Cyan}{0}{r}", template_file_name);
					return;
				}

				PrintColor.InfoLine("Base File - {f:Cyan}{0}{r}", template_file_name);

				// Do not warn for issues regarding a default template.
				if (default_template) {
					suppress_write_main = true;
				}

				/* Begin */

				{ } // Debug Breakpoint

				List<string[]> token_list = new List<string[]> {
					new string[] { } // No 0 indexing
				};
				string grammar_file = datatypes_folder_path + @"\datatypes\grammar.twt";
				PopParser p = this; // new PopParser(datatypes_folder_path, pop_directory);
				ParseTree template_pt = new ParseTree(grammar_file);

				// Populate file[] var
				string[] file = File.ReadAllLines(base_file);

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
				//try {

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
							int token_line = i;
							global_line = i; // Redundant
							string token = token_list[i][j];
							global_token = token_list[i][j]; // Redundant
							TreeNode<string[]> current = template_pt.Current;
							List<TreeNode<string[]>> children = template_pt.Current.Children;

							// Debug Level 2
							if (_IsTemplateDebug("bool_Print_Tokens")) Console.WriteLine(token + "\t\t\t" + i + " " + j);

							{ } // Debug Breakpoint

							// Debug Level 4
							if (_IsTemplateDebug("bool_Print_Token_Operations")) {
								Console.WriteLine("->token:" + token);

								if (building_string) Console.WriteLine("====STRINGBUILDER ACTIVE====");

								Console.Write("->currentvalue:");
								foreach (string vindex in current.Value) Console.Write(vindex + "|");
								Console.WriteLine("");

								Console.Write("->currentchildren:");
								foreach (TreeNode<string[]> cindex in template_pt.Current.Children) Console.Write(cindex.Value[1] + "|");
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
									ParseCollectionEnd(template_pt.CurrentValue[1], token_line, template_pt.ParentValue[1]);

									// Reset Template Name Tracker
									if (template_pt.ParentValue[1] == "Templates{}") {
										template_name = "";
									}

									template_pt.MoveUp();

									if (_IsTemplateDebug("bool_Print_PT_Cursor_Traversal")) {
										Console.WriteLine("==== UP CLOSE BRACE");
									}
									break;
								}

								// Collection Diving
								else if (look_ahead_open && token == "{") {

									found = true;
									if (_IsTemplateDebug("bool_Print_PT_Cursor_Traversal")) {
										Console.WriteLine("==== DOWN LOOK AHEAD OPEN");
									}

									template_pt.Move(look_ahead_buffer_node);

									// Parse Collection After Diving
									ParseCollection(template_pt.CurrentValue[1], token_line, template_pt.ParentValue[1], look_back_token);

									// Save Template Name for Parsing Later
									if (template_pt.ParentValue[1] == "Templates{}") {
										template_name = look_back_token;
									}

									look_ahead_buffer_node = "";
									look_ahead_open = false;
									break;
								}

								// Handles collections and % (any valid string)
								else if (token.ToUpper() == RemoveCurly(child.Value[1]).ToUpper() || Regex.IsMatch(child.Value[1], @"%")) {
									found = true;

									// Check for Valid String
									if (Regex.IsMatch(child.Value[1], @"%") && !IsDatatype("$ANY_VALID_STRING", token) && !built_string) {
										throw new Exception("IllegalIdentifierException");
									}

									// Move Down If Token Is Collection
									if (child.Value[0].ToUpper() == "COLLECTION") {
										if (_IsTemplateDebug("bool_Print_PT_Cursor_Traversal")) {
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

											if (_IsTemplateDebug("bool_Print_PT_Cursor_Traversal")) {
												Console.WriteLine("SAW WHEN{}");
											}

											Warning.MinMaxIntervalStopSpawn(global_line);
											break;
										}

										if (_IsTemplateDebug("bool_Print_PT_Cursor_Traversal")) {
											Console.WriteLine("==== DOWN " + token);
										}

										if (Regex.IsMatch(child.Value[1], @"%")) {
											template_pt.Move(child.Value[1]);
										}
										else {
											template_pt.Move(token);
										}

										break;
									}

									// Handles Values; assume token must be value string (catch by IsDatatype)
									else {

										string child_datatype = child.Value[3];

										if (IsDatatype(child_datatype.ToUpper(), token, token_line)) {
											if (_IsTemplateDebug("bool_Print_PT_Cursor_Traversal")) {
												Console.WriteLine("==== UP 1");
											}
											template_pt.MoveUp();
											break;
										}
										else if (_IsTemplateDebug("bool_Print_PT_Cursor_Traversal")) {
											Console.WriteLine("==== CHILD DT DID NOT MATCH TOKEN 1");
										}
									}
								}

								else if (current.Value[0].ToUpper() == "KEY") { // Handle All Keys

									// Parse Key and Value
									ParseKeyValue(look_back_token, token, token_line, template_pt.ParentValue[1], template_name);

									// Debug Token Lookback
									// Writes all readable key-value pairs
									if (_IsTemplateDebug("bool_Print_Token_Lookback")) {
										Console.WriteLine("Key is: " + look_back_token);
										Console.WriteLine("\tValue is: " + token);
										Console.WriteLine("\tParent is: " + template_pt.ParentValue[1]);
									}

									if (current.Value[1] == "$char_attribute%" || current.Value[1] == "$item_attribute%") { // Special Case Item/Character Attribute
										found = true;

										// Placeholder for item attribute verification
										// look_back_token : "damage bonus"
										// token : "1.0"

										if (_IsTemplateDebug("bool_Print_PT_Cursor_Traversal")) {
											Console.WriteLine("==== UP C1: " + token);
										}

										ParseAttribute(look_back_token, token, token_line);

										template_pt.MoveUp();
										break;
									}

									string child_datatype = child.Value[3];
									if (IsDatatype(child_datatype.ToUpper(), token, token_line)) {
										found = true;

										if (_IsTemplateDebug("bool_Print_PT_Cursor_Traversal")) {
											Console.WriteLine("==== UP 2");
										}

										template_pt.MoveUp();
										break;
									}

									else if (_IsTemplateDebug("bool_Print_PT_Cursor_Traversal")) {
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
				suppress_write_main = false;
				PrintColor.InfoLine("\tDone parsing Base File - {f:Cyan}{0}{r}", template_file_name);
			}
			catch (Exception) {
				suppress_write_main = false;
				Error.NoTrigger.FailedParseTemplate(template_file_name, base_file);
			}
			base_name = "";
		}

		// Parse Key Value
		public void ParseKeyValue(string key, string value, int line = -1, string parent = "", string any_string_name = "") {
			Int32.TryParse(value, out int value_int);
			key = key.ToUpper();
			parent = RemoveCurly(parent.ToUpper());

			// Debug Print Key and Value (look back parsing, parsed on value scan)
			if (ConfigReadBool("bool_Print_Parse_Key_Value", "Debug")) {
				PrintColor.DebugLine("Key: " + key);
				if (any_string_name.Length > 0) {
					PrintColor.DebugLine("\tAnyStringName: " + any_string_name);
				}
				PrintColor.DebugLine("\tValue: " + value);
				PrintColor.DebugLine("\tParent: " + parent);
				PrintColor.DebugLine("\tLine: " + line);
			}

			switch (key) {
				case "TOTALCURRENCY":
					//int credits = Int32.Parse(Regex.Match(value, @"^\d*").ToString());
					Int32.TryParse(value, out int credits);
					{ }
					// Warn negative or zero value
					if (credits > 0) {
						wave_credits_list.Last().Add(credits);
					}
					else if (credits == 0) {
						Warning.TotalCurrencyEqual0(line, value);
					}
					break;

				case "STARTINGCURRENCY":
					attribute_pairs["STARTINGCURRENCY"] = Math.Max(0, Int32.Parse(value)).ToString();
					break;

				case "HEALTH":
					int health = Int32.Parse(Regex.Match(value, @"^\d*").ToString());
					int tank_warn_max = ConfigRead("int_tank_warn_maximum");
					int tank_warn_min = ConfigRead("int_tank_warn_minimum");
					int bot_health_multiple = ConfigRead("int_bot_health_multiple");
					int tank_health_multiple = ConfigRead("int_tank_health_multiple");

					if (parent == "TFBOT") {

						// Warn multiple
						if (!IsMultiple(health, bot_health_multiple)) {
							Warning.TFBotHealthMultiple(line, value_int, bot_health_multiple);
						}
					}
					else if (parent == "TANK") {

						if (!IsMultiple(health, tank_health_multiple)) {
							Warning.TankHealthMultiple(line, value_int, tank_health_multiple);
						}

						// Warn exceeds boundaries
						if (health > tank_warn_max) {
							Warning.TankHealthExceed(line, value_int, tank_warn_max);
						}
						else if (health < tank_warn_min) {
							Warning.TankHealthBelow(line, value_int, tank_warn_min);
						}

					}
					break;

				case "NAME":

					// Create new wavespawn name entry
					if (parent == "WAVESPAWN") {
						wave_wavespawn_names.Last().Add(value);
					}

					// Configurable: Warn for non 'TankBoss' name
					else if (parent == "TANK" && value.ToUpper() != "TANKBOSS") {
						Warning.TankDeploy(line, value);
					}
					break;

				case "WAITFORALLSPAWNED":
					used_wavespawn_names.Add(value); // TODO Major Refactor
					used_wavespawn_lines.Add(line);
					break;

				case "WAITFORALLDEAD":
					used_wavespawn_names.Add(value); // TODO Major Refactor
					used_wavespawn_lines.Add(line);
					break;

				case "ITEM":
					tfbot_items.Add(value);
					bool item_exists = false;

					// Add item to corresponding TFBot template item list
					for (int i = tfbot_template_items.Count - 1; i >= 0; i--) {
						if (tfbot_template_items[i][0] == any_string_name.ToUpper()) { // Items in list should already be in upper
							tfbot_template_items[i].Add(value);
						}
					}

					// Search database for key
					foreach (string find in ItemDatabase.List) {
					if (value.ToUpper() == find.ToUpper()) {
							item_exists = true;
							break;
						}
					}

					// Item does not exist in database
					if (!item_exists) {
						Warning.ItemInvalid(line, value);
						return;
					}

					break;

				case "ITEMNAME":
					bool bot_has_item = false;

					// Search for item owned by TFBot
					foreach (string item in tfbot_items) {
						if (item.ToUpper() == value.ToUpper()) {
							bot_has_item = true;
						}
					}

					if (!bot_has_item & !Regex.IsMatch(value, "TF_", RegexOptions.IgnoreCase)) {
						Warning.ItemMissing(line, value);
					}
					break;

				case "TEMPLATE":

					{ }

					// Check if Template Name exists
					bool found = false;
					foreach (List<string> template_item_list in tfbot_template_items) {
						if (value.ToUpper() == template_item_list[0].ToUpper()) {
							found = true;
						}
					}

					if (!found) {
						Warning.TemplateInvalid(line, value);
						break;
					}

					// Find template value item list
					foreach(List<string> template_item_list in tfbot_template_items) {
						if (value.ToUpper() == template_item_list[0]) {

							// Import template items into current bot items
							for(int i = 2; i < template_item_list.Count; i++) { // Index 0 is tfbot anystring name, Index 1 is origin file name
								tfbot_items.Add(template_item_list[i]);
							}
						}
					}

					// Add Template to Template
					if (any_string_name.Length > 0) { // Check if KeyValue was called in a Template
						// Get new template
						foreach(List<string> tfbot_template_new in tfbot_template_items) {
							if (any_string_name.ToUpper() == tfbot_template_new[0].ToUpper()) {

								// Find Correct Template to add items to
								foreach (List<string> tfbot_template_add in tfbot_template_items) {
									if (value.ToUpper() == tfbot_template_add[0].ToUpper()) {

										// Add found template's items to new template (new template's name is any_string_name)
										for (int i = 1; i < tfbot_template_add.Count(); i++) { // Skip 0th index; [0] is template name
											tfbot_template_new.Add(tfbot_template_add[i]);
										}
									}
								}
							}
						}
					}

					break;

				case "CLASSICON": // Icon Scanning
					icons_list.Add(value.ToLower());

					break;

				case "SUPPORT":
					if (value.ToUpper() == "LIMITED") {
						Warning.SupportLimited(line);
					}

					break;

					// TODO : Add more cases
			}
		}

		// Check if Integer is a multiple of Value
		public bool IsMultiple(int value, int multiple) {
			return value % multiple == 0;
		}

		// Get Starting Currency
		public int StartingCurrency {
			get {
				try {
					return Int32.Parse(attribute_pairs["STARTINGCURRENCY"]);
				}
				catch { // Catch KeyNotFoundException, return $0 starting credits
					return 0;
				}
			}
		}

		// Get Total Dropped Credits
		public int TotalCurrency {
			get {
				int total_currency = 0;
				foreach (List<int> credits_list in wave_credits_list) {
					total_currency += credits_list.Sum();
				}
				return total_currency;
			}
		}

		// Get Total Possible Bonus Credits
		// Note: Total Possible Bonus != #waves * 100
		// Note: A bonus is not rewarded if the wave drops 0 credits.
		// Note: A bonus is not rewarded on the final wave.
		public int TotalWaveBonus {
			get {
				int total_bonus = 0;
				for (int i = 0; i < wave_credits_list.Count; i++) {
					List<int> credits_list = wave_credits_list[i];
					
					// Final wave does not give bonus
					if (i == wave_credits_list.Count - 1) {
						break;
					}

					// Only give bonus if the wave drops any credits
					if (credits_list.Count > 0) {
						total_bonus += 100;
					}
				}
				return total_bonus;
			}
		}

		// Get Number of Waves
		public int TotalWaves {
			get {
				return total_waves;
			}
		}

		// Returns name of collection token
		public string RemoveCurly (string collection_token) {
			return Regex.Replace(collection_token, "({})", ""); // Must be exactly "{}" sequence
		}

		// Get warning suppression status
		public bool SuppressPrint {
			get {
				return suppress_write_main;
			}
		}

		// Returns if token falls under specified datatype
		/* Throws
		 * DatatypeNotFoundException
		 * InvalidTypeException
		 */
		public bool IsDatatype(string type, string token, int line_number = -1) {
			// Check valid datatype according to DATATYPES array
			if (Regex.IsMatch(type, "%")) {
				type = "$ANY_VALID_STRING";
			}
			foreach (string datatype in DATATYPES) {
				if (type.ToLower() == datatype.ToLower()) {
					string[] datatype_file;
					// Check primitives first
					switch (type.ToUpper()) {
						case "BOOLEAN":
							return Regex.IsMatch(token, @"^(false|true|yes|no|1|0)$", RegexOptions.IgnoreCase);

						case "FLOAT":
							Double.TryParse(token, out double d);
							/*if (FloatingPoint.IsOverflow(d, out double actual)) {
								//PrintColor.Warn("Bad decimal value: '{f:Yellow}{0}{r}' will be interpreted as '{f:Yellow}{1}{r}'", line_number, token, actual.ToString());
								Warning.FloatBadDecimal(line_number, token, actual); // TODO: FloatingPoint
							}*/

							if (Regex.IsMatch(token, @"^(-?)\d+$")) { // Float can be interpreted as Integer
								return IsDatatype("INTEGER", token, line_number);
							}
							return Regex.IsMatch(token, @"^\d*\.\d+$");

						case "UNSIGNED INTEGER":
							/*if (Regex.IsMatch(token, @"^(-|)\d+\.\d*$")) {
								//PrintColor.Warn("Decimal value will be reinterpreted from '{f:Yellow}{0}{r}' to '{f:Yellow}{1}{r}'", line_number, token, FloatingPoint.IntegerInterpCast(token).ToString());
								Warning.FloatReinterpretedFromTo(line_number, token, FloatingPoint.IntegerInterpCast(token)); // TODO: FloatingPoint
								return true;
							}*/

							// Warn for Negative Values
							if (Regex.IsMatch(token, "-")) {
								Warning.NegativeInterpreted0(line_number, token);
								return IsDatatype("INTEGER", token, line_number);
							}
							return Regex.IsMatch(token, @"^\d+$");

						case "INTEGER": 
							/*if (Regex.IsMatch(token, @"^\d+\.\d+$")) {
								//PrintColor.Warn("Decimal value will be reinterpreted from '{f:Yellow}{0}{r}' to '{f:Yellow}{1}{r}'", line_number, token, FloatingPoint.IntegerInterpCast(token).ToString());
								Warning.FloatReinterpretedFromTo(line_number, token, FloatingPoint.IntegerInterpCast(token)); // TODO: FloatingPoint
								return true;
							}*/
							return Regex.IsMatch(token, @"^(-?)\d+$");

						case "STRING":
							return !String.IsNullOrWhiteSpace(token); // return false on empty or blank string; should always 

						case "SENTRYGUN LEVEL":
							return Regex.IsMatch(token, @"^[1|2|3]$");

						case @"$ANY_VALID_STRING":
							return !Regex.IsMatch(token, "^.*(\"|\\s|#base|#include).*$", RegexOptions.IgnoreCase);

						case "BOT NAME":
							if (Regex.IsMatch(token, "%")) {
								Warning.TFBotNameBadCharacter(line_number);
							}
							return true;

						case "ITEM NAME":
							return IsDatatype("STRING", token, line_number); // Accepts only strings

						case "WHERE":
							return IsDatatype("STRING", token, line_number); // Accepts only strings

						case "TEMPLATE NAME":
							return IsDatatype("STRING", token, line_number); // Accepts only strings

						case "SUPPORT TYPE":
							return IsDatatype("STRING", token, line_number);

						case "FILE":
							return Regex.IsMatch(token, @"\S+\.pop$", RegexOptions.IgnoreCase);

						case "CLASS NAME":
							datatype_file = File.ReadAllLines(datatypes_folder_path + "\\datatypes\\CLASS_NAME.owo");
							foreach (string line in datatype_file) {
								if (Regex.IsMatch(token, line, RegexOptions.IgnoreCase)) {
									return true;
								}
							}
							return false;
						
						// Check special datatypes according to datatypes definitions folder
						default:
							try {
								type = Regex.Replace(type, " ", "_");
								datatype_file = File.ReadAllLines(datatypes_folder_path + "\\datatypes\\" + type + ".owo");
								foreach (string line in datatype_file) {
									if (line.ToUpper() == token.ToUpper()) {
										return true;
									}
								}
								return false;
							}
							catch (FileNotFoundException) {
								throw new Exception("DatatypeNotFoundException");
							}
					}
				}
			}
			
			// Datatype was not found. 'type' did not match anything
			throw new Exception("InvalidTypeException");
		}
	}

}