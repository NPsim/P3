using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PseudoPopParser {

	class Program {

		private static IniFile _INI = new IniFile(@"config.ini");
		private static Dictionary<string, string> _CONFIGURATION = new Dictionary<string, string>();

		static bool _IsDebug(string key) {;
			if (_CONFIGURATION.ContainsKey(key)) {
				return _CONFIGURATION[key] == "1";
			}
			else if (_INI.KeyExists(key, "Debug")) {
				_CONFIGURATION.Add(key, _INI.Read(key, "Debug"));
				return _INI.Read(key, "Debug") == "1";
			}
			return false;
		}

		static void Main(string[] args) {

			// Debug Terminator
			if (_IsDebug("Print_Terminators")) {
				Console.WriteLine(">>>>>Start of file | Debug Level ");
			}

			string file_path = "",
				grammar_file = AppDomain.CurrentDomain.BaseDirectory + "grammar.owo",
				datatypes_folder = AppDomain.CurrentDomain.BaseDirectory;

			string[] file = null;

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
			}

			// Init Parser
			PopParser p = new PopParser(datatypes_folder);
			ParseTree pt = new ParseTree(grammar_file);

			// Get file_path if not defined in launch
			if (file_path == null) {
				file_path = Console.ReadLine();
			}

			// Populate file[] var
			file = File.ReadAllLines(file_path);
			Console.WriteLine("Pop File: " + file_path);

			// Modify strings
			for (int i = 0; i < file.Length; i++) {
				file[i] = Regex.Replace(file[i], @"(\s|\/+|^)\/\/.*[\s]*", "");		// Remove Comments
				file[i] = Regex.Replace(file[i], @"{", " { ");						// Separate Open Curly Braces
				file[i] = Regex.Replace(file[i], @"}", " } ");						// Separate Close Curly Braces
				file[i] = Regex.Replace(file[i], "\"", " \" ");						// Separate Double Quotes
				file[i] = Regex.Replace(file[i], @"^\s+", "");						// Remove Indentation Whitespace
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
			try {
				for (int i = 0; i < token_list.Count; i++) {
					for (int j = 0; j < token_list[i].Length; j++) {
						if (!string.IsNullOrWhiteSpace(token_list[i][j])) {

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
							if (_IsDebug("Print_Tokens")) Console.WriteLine(token + "\t\t\t" + i + " " + j);

							// Debug Level 4
							if (_IsDebug("Print_Token_Operations")) {
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
									p.ParseCollectionEnd(pt.CurrentValue[1], line);

									pt.MoveUp();

									// Detect Possible Premature End of WaveSchedule : WaveSchedule closes in <99% of the total line count.
									if (i < token_list.Count * 99 / 100 && pt.Current.Value[2] == "NONE") {
										p.Warn("Possible premature end of WaveSchedule detected near ", global_line, global_token);
										p.PotentialFix("Remove additional lines after end of WaveSchedule");
										p.PotentialFix("Recount Curly Brackets");
									}

									if (_IsDebug("Print_PT_Cursor_Traversal")) {
										Console.WriteLine("==== UP CLOSE BRACE");
									}
									break;
								}

								// Collection Diving
								else if (look_ahead_open && token == "{") {

									found = true;
									if (_IsDebug("Print_PT_Cursor_Traversal")) {
										Console.WriteLine("==== DOWN LOOK AHEAD OPEN");
									}

									pt.Move(look_ahead_buffer_node);

									// Parse Collection After Diving
									p.ParseCollection(pt.CurrentValue[1], line);

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
										if (_IsDebug("Print_PT_Cursor_Traversal")) {
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

											if (_IsDebug("Print_PT_Cursor_Traversal")) {
												Console.WriteLine("SAW WHEN{}");
											}

											p.Warn("Using \"When\" with \"MinInterval\" and \"MaxInterval\" may stop spawning midwave", global_line);
											break;
										}

										if (_IsDebug("Print_PT_Cursor_Traversal")) {
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
											if (_IsDebug("Print_PT_Cursor_Traversal")) {
												Console.WriteLine("==== UP 1");
											}
											pt.MoveUp();
											break;
										}
										else if (_IsDebug("Print_PT_Cursor_Traversal")) {
											Console.WriteLine("==== CHILD DT DID NOT MATCH TOKEN 1");
										}
									}
								}

								else if (current.Value[0].ToUpper() == "KEY") { // Handle All Keys

									// Parse Key and Value
									p.ParseKeyValue(look_back_token, token, line, pt.ParentValue[1]);

                                    // Debug Token Lookback
                                    // Writes all readable key-value pairs
                                    if (_IsDebug("Print_Token_Lookback")) {
                                        Console.WriteLine("Key is: " + look_back_token);
                                        Console.WriteLine("\tValue is: " + token);
                                        Console.WriteLine("\tParent is: " + pt.ParentValue[1]);
                                    }

                                        if (current.Value[1] == "$char_attribute%") { // Special Case Character Attribute
										found = true;

										// Placeholder for item attribute verification
										// look_back : "damage bonus"
										// token : "1.0"

										if (_IsDebug("Print_PT_Cursor_Traversal")) {
											Console.WriteLine("==== UP C1: " + token);
										}

										pt.MoveUp();
										break;
									}

									else if (current.Value[1] == "$item_attribute%") { // Special Case Item Attribute
										found = true;

										// Placeholder for item attribute verification
										// look_back : "Attack not cancel charge"
										// token : "1"

										if (_IsDebug("Print_PT_Cursor_Traversal")) {
											Console.WriteLine("==== UP I1: " + token);
										}

										pt.MoveUp();
										break;
									}

									string child_datatype = child.Value[3];
									if (p.IsDatatype(child_datatype.ToUpper(), token, line)) {
										found = true;

										if (_IsDebug("Print_PT_Cursor_Traversal")) {
											Console.WriteLine("==== UP 2");
										}

										pt.MoveUp();
										break;
									}

									else if (_IsDebug("Print_PT_Cursor_Traversal")) {
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

			// Blank Line : Separate Warnings/Errors from Info Section
			Console.Write("\n");

			// Debug Terminator
			if (_IsDebug("Print_Terminators")) {
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
				p.InfoLine("Starting Credits: " + p.StartingCurrency );
				p.InfoLine("Total Dropped Credits: " + p.TotalCurrency );
				p.InfoLine("Total Bonus Credits: " + p.TotalWaveBonus );
				p.InfoLine("Maximum Possible Credits: " + (p.StartingCurrency + p.TotalCurrency + p.TotalWaveBonus) );
			}

			// Blank Line : Separate Ending Statements with Further Option Choices
			Console.Write("\n");

			// Show Next Options
			p.WriteColor("Any Key", ConsoleColor.White, ConsoleColor.Black);
			p.WriteLineColor(" Quit");

			// Dev message
			p.WriteColor("[ALPHA] P3 DEVELOPMENT BUILD", ConsoleColor.Green, ConsoleColor.Black);

			Console.WriteLine(" ");

			// Exit on any key
			Console.ReadKey();

		}

	}

}
