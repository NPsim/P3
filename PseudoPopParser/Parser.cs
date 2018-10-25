using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {

	class PopParser {

		// TODO organize this
		private static IniFile _INI = new IniFile(@"config.ini");
		private static string pop_directory = "";
		private static int number_of_warnings = 0;
		private static bool error_occurred = false;
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
		private static List<string> item_list = new List<string>();
		private static List<string> tfbot_items = new List<string>();
		private static bool suppress_write_main = false;

		// TODO Fix spaces vs tabs issue
		/* Purpose of Class:
		 * Check and return token types
		 * Throw exceptions if invalid token recieved
		 */

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
		public PopParser(string datatypes_folder, string pop_folder) {
			datatypes_folder_path = datatypes_folder;
			pop_directory = pop_folder;
			SetupAttributes();
			SetupItems();
		}

		// Setup Attributes Database
		private void SetupAttributes() {
			string db_file = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\item_attributes.owo";
			if (!File.Exists(db_file)) {
				Error("Item attributes db does not exist!");
				return;
			}
			string[] db = File.ReadAllLines(db_file);
			for (int line = 2; line < db.Length; line += 3) {
				attributes_list.Add(new string[] {
					db[line],
					db[line + 1].Substring(1, db[line + 1].Length - 1),
					db[line + 2].Substring(1, db[line + 2].Length - 1)
				});
			}
		}

		// Setup Items Database
		private void SetupItems() {
			string db_file = AppDomain.CurrentDomain.BaseDirectory + @"\datatypes\item_db.owo";
			if (!File.Exists(db_file)) {
				Error("Items db does not exist!");
				return;
			}
			string[] db = File.ReadAllLines(db_file);
			for (int i = 2; i < db.Count(); i++) {
				string line = db[i]; // TODO make use of all db data
				if (Regex.IsMatch(line, @"^\S")) {
					item_list.Add(line);
				}
			}
		}

		// Read config.ini
		private int ConfigRead(string key, string section = "Global") {
			if (_INI.KeyExists(key, section)) {
				return Int32.Parse(_INI.Read(key, section));
			}
			return -1;
		}

		// Read config.ini boolean
		private bool ConfigReadBool(string key, string section = "Global") {
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
					if (member == value) {
						return true;
					}
				}
			}
			return false;
		}

		// Print Formatted Contents of Credits List
		public void WriteCreditStats() {
			InfoLine("Credit Stats:");
			for (int i = 1; i <= wave_credits_list.Count; i++) {
				List<int> wave_credits = wave_credits_list[i - 1];
				InfoLine("\tW" + i + ": " + wave_credits.Sum() + " dropped during this wave");
				foreach (int credits in wave_credits) {
					InfoLine("\t\t" + credits);
				}
			}

			Console.Write("\n");
		}

		// Print Formatted Contents of WaveSpawn Names List
		public void WriteWaveSpawnNames() {
			InfoLine("Subwave Names:");
			for (int i = 1; i <= wave_wavespawn_names.Count; i++) {
				List<string> wave_names = wave_wavespawn_names[i - 1];
				InfoLine("\tW" + i + ": ");
				foreach (string name in wave_names) {
					InfoLine("\t\t" + name);
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

					if (parent == "Templates{}") {
						List<string> new_template = new List<string> {
							any_string_token.ToUpper()
						};
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
					int credits_multiple = ConfigRead("currency_multiple_warning");
					if (!IsMultiple(wave_credits, credits_multiple)) {
						Warn("Wave " + total_waves + "'s credits is not a multiple of " + credits_multiple + ": ", -1, wave_credits.ToString());
					}

					break;

				case "ANY_VALID_STRING{}%": // End of Entire Schedule Second Pass Parsing
					// Second pass parsing does not require look ahead or look behind due to everything's already looked at.

					// Warn physical money counter credits >30000
					int total_credits = StartingCurrency + TotalCurrency + TotalWaveBonus;
					if (total_credits > 30000) {
						Warn("Credits counter physically cannot exceed reading of 30000: ", -1, total_credits.ToString());
					}

					// Compare WaveSpawn Names
					//foreach(string waitforname in used_wavespawn_names) {
					for(int i = 0; i < used_wavespawn_names.Count(); i++) {
						string waitforname = used_wavespawn_names[i];
						if (!ExistsTwoDimension(wave_wavespawn_names, waitforname)) {
							Warn("WaitForAll* name does not exist: ", used_wavespawn_lines[i], waitforname);
						}
					}

					break;

				case "TFBot{}":

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
				Warn("Invalid attribute name found: ", line, key);
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
			if (type == "FLOAT" && !Regex.IsMatch(UnQuote(value), @"^(-|)(\d*)(\.|)(\d*|)$")) {
				Warn("Attribute has invalid number value: ", line, key + " " + value);
			}
			else if (type == "STRING" && !Regex.IsMatch(value, "(\\\".*\\\")")) {
				Warn("Attribute value must be surrounded by quotes: ", line, value); // Pending Deletion
			}

			/* Check Bad Float Values */

			if (type == "FLOAT") {

				// Adding 0
				if (value_double == 0.0 && (form == "value_is_additive" || form == "value_is_additive_percentage")) {
					Warn("Attribute does nothing: ", line, key + " " + value);
					PotentialFix("Value adds 0 to attribute");
				}
				// Multiplying by 100%
				else if (value_double == 1.0 && (form == "value_is_percentage" || form == "value_is_inverted_percentage")) {
					Warn("Attribute does nothing: ", line, key + " " + value);
					PotentialFix("Value multiplies attribute by 1.00");
				}
				// Invalid Boolean
				else if (!(value == "0" || value == "1") && form == "value_is_or") {
					Warn("Attribute can only be values 0 or 1: ", line, key + " " + value);
				}
			}
		}

		// Parse Template Pop FIle
		public void ParseBase(string base_file, bool default_template = false) { // I know it's terrible.
			string template_file_name = Regex.Match(base_file, @"[\w-]+\.pop").ToString();
			try {
				InfoLine("Template File - " + template_file_name);

				// Do not warn for issues regarding a default template.
				if (default_template) {
					suppress_write_main = true;
				}

				/* Begin */

				{ } // Debug Breakpoint

				List<string[]> token_list = new List<string[]> {
					new string[] { } // No 0 indexing
				};
				string grammar_file = datatypes_folder_path + "grammar.owo";
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

									// Detect Possible Premature End of WaveSchedule : WaveSchedule closes in <99% of the total line count.
									if (i < token_list.Count * 99 / 100 && template_pt.Current.Value[2] == "NONE") {
										Warn("Possible premature end of WaveSchedule detected near ", global_line, global_token);
										PotentialFix("Remove additional lines after end of WaveSchedule");
										PotentialFix("Recount Curly Brackets");
									}

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

											Warn("Using \"When\" with \"MinInterval\" and \"MaxInterval\" may stop spawning midwave", global_line);
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
				InfoLine("Successfully parsed " + template_file_name);
			}
			catch (Exception err) {
				suppress_write_main = false;
				Error("Could not parse Template file: ", -1, template_file_name);
				Error(err.Message);
			}
		}

		// Parse Key Value
		public void ParseKeyValue(string key, string value, int line = -1, string parent = "", string any_string_name = "") {
			key = key.ToUpper();
			parent = RemoveCurly(parent.ToUpper());

			// Debug Print Key and Value (look back parsing, parsed on value scan)
			if (ConfigReadBool("bool_Print_Parse_Key_Value", "Debug")) {
				DebugLine("Key: " + key);
				if (any_string_name.Length > 0) {
					DebugLine("\tAnyStringName: " + any_string_name);
				}
				DebugLine("\tValue: " + value);
				DebugLine("\tParent: " + parent);
				DebugLine("\tLine: " + line);
			}

			switch (key) {
				case "TOTALCURRENCY":
					int credits = Int32.Parse(value);

					// Warn negative or zero value
					if (credits > 0) {
						wave_credits_list.Last().Add(credits);
					}
					else if (credits == 0) {
						Warn("TotalCurrency value equal to 0 drops nothing: ", line, value);
					}
					else if (credits < 0) {
						Warn("TotalCurrency value less than 0 drops nothing: ", line, value);
					}
					break;

				case "STARTINGCURRENCY":
					attribute_pairs["STARTINGCURRENCY"] = Math.Max(0, Int32.Parse(value)).ToString();
					break;

				case "HEALTH":
					int health = Int32.Parse(value);
					int tank_warn_max = ConfigRead("tank_warn_maximum");
					int tank_warn_min = ConfigRead("tank_warn_minimum");
					int bot_health_multiple = ConfigRead("bot_health_multiple");
					int tank_health_multiple = ConfigRead("tank_health_multiple");

					if (parent == "TFBOT") {

						// Warn multiple
						if (!IsMultiple(health, bot_health_multiple)) {
							Warn("TFBot Health is not a multiple of " + bot_health_multiple.ToString() + ": ", line, value); //TODO Add line number and token
						}
					}
					else if (parent == "TANK") {

						if (!IsMultiple(health, tank_health_multiple)) {
							Warn("Tank Health is not a multiple of " + tank_health_multiple.ToString() + ": ", line, value); //TODO Add line number and token
						}

						// Warn exceeds boundaries
						if (health > tank_warn_max) {
							Warn("Tank Health exceeds maximum warning: " + tank_warn_max + " < ", line, value);
							// PotentialFix("Did you add too many zeros?");
						}
						else if (health < tank_warn_min) {
							Warn("Tank Health below minimum warning: " + tank_warn_min + " > ", line, value);
							// PotentialFix("Are you missing any zeros?");
						}

					}
					break;

				case "NAME":

					// Create new wavespawn name entry
					if (parent == "WAVESPAWN") {
						wave_wavespawn_names.Last().Add(value);
					}

					// Configurable: Warn for non 'TankBoss' name
					else if (parent == "TANK" && value.ToUpper() != "TANKBOSS" && ConfigReadBool("bool_tank_name_tankboss")) {
						Warn("Tank not named 'TankBoss' does not explode on deployment: ", line, value);
					}
					break;

				case "WAITFORALLSPAWNED":
					/*if (!ExistsTwoDimension(wave_wavespawn_names, value)) {
						Warn("WaitForAllSpawned name does not exist: ", line, value);
					}*/

					used_wavespawn_names.Add(value); // TODO Major Refactor
					used_wavespawn_lines.Add(line);
					break;
				case "WAITFORALLDEAD":
					/*if (!ExistsTwoDimension(wave_wavespawn_names, value)) {
						Warn("WaitForAllDead name does not exist: ", line, value);
					}*/
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
					foreach (string find in item_list) {
						if (value.ToUpper() == find.ToUpper()) {
							item_exists = true;
							break;
						}
					}

					// Item does not exist in database
					if (!item_exists) {
						Warn("Invalid item name: ", line, value);
						return;
					}

					break;

				case "ITEMNAME":
					bool bot_has_item = false;

					// Search for item owned by TFBot
					foreach (string item in tfbot_items) {
						if (item == value) {
							bot_has_item = true;
						}
					}

					if (!bot_has_item & !Regex.IsMatch(value, "TF_")) {
						Warn("TFBot does not have item: ", line, value);
					}
					break;

				case "TEMPLATE":

					// Find template value item list
					foreach(List<string> template_item_list in tfbot_template_items) {
						if (value.ToUpper() == template_item_list[0]) {

							// Import template items into current bot items
							for(int i = 1; i < template_item_list.Count; i++) {
								tfbot_items.Add(template_item_list[i]);
							}
						}
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
				// Catch KeyNotFoundException, return $0 starting credits
				catch {
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

		// Returns primitive datatype of token
		// Pending Removal
		public string GetDataTypePrimitive(string token) {
			if (Regex.IsMatch(token, @"^\d+.\d+$")) return "FLOAT";
			else if (Regex.IsMatch(token, @"^\d+$")) return "UNSIGNED INTEGER";
			else if (Regex.IsMatch(token, @"^(-?)\d+$")) return "INTEGER";
			else if (Regex.IsMatch(token, @"^[false|true|yes|no|1|0]$")) return "BOOLEAN";
			else return "STRING";
		}


		// Simple Print Color
		public void WriteMain(string message, string header, int line = -1, ConsoleColor background = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.Gray) {
			if (suppress_write_main) {
				return;
			}

			Console.BackgroundColor = background;
			Console.ForegroundColor = foreground;

			// Write Header
			Console.Write(header);
			Console.ResetColor();

			// Line Number and Message
			if (line > 0) { // Line Number exists
				Console.Write(":" + line + "\t" + message);
			}
			else { // Line number does not exist
				Console.Write("\t" + message);
			}
		}

		public void WriteMainLine(string message, string header, int line = -1, ConsoleColor background = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.Gray) {
			if (suppress_write_main) {
				return;
			}
			WriteMain(message, header, line, background, foreground);
			Console.Write("\n");
		}

		public void WriteColor(string message, ConsoleColor background = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.White) {
			Console.BackgroundColor = background;
			Console.ForegroundColor = foreground;
			Console.Write(message);
			Console.ResetColor();
		}

		// Simple Print Color Line
		public void WriteLineColor(string message, ConsoleColor background = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.Gray) {
			WriteColor(message, background, foreground);
			Console.Write("\n");
		}

		// Simple Print Potential Fix
		public void PotentialFix(string message, bool false_positive = false) {
			if (suppress_write_main) {
				return;
			}
			Console.Write("\t");
			Console.BackgroundColor = ConsoleColor.Gray;
			Console.ForegroundColor = ConsoleColor.Black;
			if (false_positive) {
				WriteMainLine(message, "[Ptl. False Positive]", -1, ConsoleColor.Gray, ConsoleColor.Black);
			}
			else {
				WriteMainLine(message, "[Potential Fix]", -1, ConsoleColor.Gray, ConsoleColor.Black);
			}
		}

		// Simple Print Warning
		public void Warn(string message, int line = -1, string token = "") {
			if (suppress_write_main) {
				return;
			}
			number_of_warnings++;
			ConsoleColor background = ConsoleColor.Yellow;
			ConsoleColor foreground = ConsoleColor.Black;

			WriteMain(message, "[Warning]", line, background, foreground);

			if (token.Length > 0) {
				Console.Write("'");
				WriteColor(token, ConsoleColor.Black, background);
				Console.Write("'\n");
			}
			else {
				Console.Write("\n");
			}
		}

		// Simple Print Error
		public void Error(string message, int line = -1, string token = "") {
			if (suppress_write_main) {
				return;
			}
			error_occurred = true;
			ConsoleColor background = ConsoleColor.Red;
			ConsoleColor foreground = ConsoleColor.Black;

			WriteMain(message, "[ERROR]", line, background, foreground);

			if (token.Length > 0) {
				Console.Write("'");
				WriteColor(token, ConsoleColor.Black, background);
				Console.Write("'\n");
			}
			else {
				Console.Write("\n");
			}
		}

		// Simple Print Info
		public void Info(string message) {
			if (suppress_write_main) {
				return;
			}
			ConsoleColor background = ConsoleColor.DarkCyan;
			ConsoleColor foreground = ConsoleColor.Black;

			WriteMain(message, "[Info]", -1, background, foreground);
		}
		public void InfoLine(string message) {
			if (suppress_write_main) {
				return;
			}
			ConsoleColor background = ConsoleColor.DarkCyan;
			ConsoleColor foreground = ConsoleColor.Black;

			WriteMainLine(message, "[Info]", -1, background, foreground);
		}

		// Simple Print Debug Line
		public void DebugLine(string message) {
			ConsoleColor background = ConsoleColor.Magenta;
			ConsoleColor foreground = ConsoleColor.Black;

			WriteMainLine(message, "[DEBUG]", -1, background, foreground);
		}

		// Get number of warnings issued
		public int Warnings {
			get {
				return number_of_warnings;
			}
		}

		// Get if an error ever happened
		public bool ErrorOccurred {
			get {
				return error_occurred;
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
					// Check primitives first
					// TODO Change this to a switch statement
					if (type.ToUpper() == "BOOLEAN") {
						return Regex.IsMatch(token, @"^(false|true|yes|no|1|0)$", RegexOptions.IgnoreCase);
					}
					else if (type.ToUpper() == "FLOAT") {
						if (IsDatatype("INTEGER", token, line_number)) { // Float can be interpreted as Integer
							return IsDatatype("INTEGER", token, line_number);
						}
						return Regex.IsMatch(token, @"^\d+.\d+$");
					}
					else if (type.ToUpper() == "UNSIGNED INTEGER") {

						// Warn for Negative Values
						if (Regex.IsMatch(token, "-")) {
							Warn("Negative value will be interpreted as 0: ", line_number, token);
							return IsDatatype("INTEGER", token, line_number);
						}

						return Regex.IsMatch(token, @"^\d+$");
					}
					else if (type.ToUpper() == "INTEGER") {
						return Regex.IsMatch(token, @"^(-?)\d+$");
					}
					else if (type.ToUpper() == "STRING") {
						return !String.IsNullOrWhiteSpace(token); // return false on empty or blank string; should always 
					}
					else if (type.ToUpper() == "SENTRYGUN LEVEL") {
						return Regex.IsMatch(token, @"^[1|2|3]$");
					}
					else if (type.ToUpper() == @"$ANY_VALID_STRING") {
						return !Regex.IsMatch(token, "^(\\S|\\s|)*(\"|\\s|#base|#include)(\\S|\\s|)*$", RegexOptions.IgnoreCase);
					}
					else if (type.ToUpper() == "BOT NAME") { // Bot Name
						if (Regex.IsMatch(token, "%"))
							Warn("Invalid symbol \"%\"", line_number);
						if (Regex.IsMatch(token, "#"))
							Warn("Invalid symbol \"#\"", line_number);
						return true;
					}
					else if (type.ToUpper() == "ITEM NAME") {
						return IsDatatype("STRING", token, line_number); // fall back to any string for now
					}

					else if (type.ToUpper() == "WHERE") {
						return IsDatatype("STRING", token, line_number); // fall back to any string for now
					}
					else if (type.ToUpper() == "TEMPLATE NAME") {
						return IsDatatype("STRING", token, line_number); // fall back to any string for now
					}

					else if (type.ToUpper() == "SUPPORT TYPE") {

						// Warn 'limited' support is equal to having no support key
						if (token.ToUpper() == "LIMITED") {
							Warn("Support value disables infinite spawn: ", line_number, token);
							PotentialFix("Support 'limited' is the same as no support.");
						}
						return IsDatatype("STRING", token, line_number);
					}
					else if (type.ToUpper() == "FILE") {
						return Regex.IsMatch(token, @"\S+\.pop$", RegexOptions.IgnoreCase);
					}
					else if (type.ToUpper() == "CLASS NAME") {
						type = Regex.Replace(type, " ", "_");
						string[] datatype_file = File.ReadAllLines(datatypes_folder_path + "\\datatypes\\" + type + ".txt");
						foreach (string line in datatype_file) {
							if (Regex.IsMatch(token, line, RegexOptions.IgnoreCase)) {
								return true;
							}
						}
						return false;
					}

					// Check special datatypes according to datatypes definitions folder
					else {
						try {
							type = Regex.Replace(type, " ", "_");
							string[] datatype_file = File.ReadAllLines(datatypes_folder_path + "\\datatypes\\" + type + ".txt");
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
			throw new Exception("InvalidTypeException");
		}
	}

}