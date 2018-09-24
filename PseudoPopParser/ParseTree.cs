using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PseudoPopParser {
	class ParseTree {
		/* Purpose of Class:
		 * Build a Parse Tree to validate the structure of the input file
		 * Throw exception if invalid token path
		 */

		private static TreeNode<string[]> tree = new TreeNode<string[]>( // Main Parse Tree
			new string[] { "Tree", "Tree", "NONE", "Tree", "-1", "FALSE", "TRUE" }
		);
		private static TreeNode<string[]> current_node = tree; // Cursor Node

		private List<string> node_path = new List<string>();

		// Get rid of prefix and suffix whitespace
		private string RemovePolarWhitespace(string str) {
			return Regex.Replace(str, @"(^\s+|\s+$)", "");
		}

		// Special Constructor
		/* Throws
		 * ParentNotFoundException
		 * HeadSubParentNotFoundException
		 * SubParentNotFoundException
		 */
		public ParseTree(string read_from_file, int DEBUG_LEVEL = 0) {
			string[] grammar = File.ReadAllLines(read_from_file);
			bool is_subtree = false;
			List<List<string>> available_subtrees = new List<List<string>>();

			for (int i = 0; i < grammar.Length; i++) {
				string type, name, parent_name, datatype, any_name, required;
				int layer = 0;

				// Remove Comments
				grammar[i] = Regex.Replace(grammar[i], @"\/\/.*[\s]*$", "");

				// Get Tree Level
				layer = Regex.Matches(grammar[i], @"\t").Count;

				// Remove Trailing Whitespace
				grammar[i] = Regex.Replace(grammar[i], @"\s+$", "");

				// Skip if blank line
				if (grammar[i] == "") {
					continue;
				}

				/* Subtree Operations */

				// Check Subtree
				if (Regex.IsMatch(grammar[i], "@DEFINE_")) {
					grammar[i] = Regex.Replace(grammar[i], "@DEFINE_", "");

					if (grammar[i] == "STARTSUBTREE") {
						is_subtree = true;
						List<string> subtree = new List<string> { };
						available_subtrees.Add(subtree);
						continue;
					}
					else if (grammar[i] == "ENDSUBTREE") {
						is_subtree = false;
						continue;
					}
				}

				// Subtree routine
				if (is_subtree) {
					available_subtrees.Last().Add(grammar[i]);
					continue;
				}

				/* Main Operations */

				// Special Case: $ symbol
				if (Regex.IsMatch(RemovePolarWhitespace(grammar[i]), @"^\$")) {
					grammar[i] = Regex.Replace(grammar[i], @"\$", "");
				}

				// Type - Index 0
				if (Regex.IsMatch(grammar[i], @"{}")) {
					type = "Collection";
				}
				else if (grammar[i] == grammar[i].ToUpper()) {
					type = "Value";
				}
				else {
					type = "Key";
				}

				// Name - Index 1
				name = RemovePolarWhitespace(grammar[i]);

				// Parent Name - Index 2
				if (layer == 0) {
					parent_name = "0";
				}
				else if (type == "Value") {
					parent_name = RemovePolarWhitespace(grammar[i - 1]);
				}
				else {
					parent_name = "UNKNOWN";

					for (int j = i; j > 0; j--) {

						// Skip if blank line
						if (grammar[j] == "") {
							continue;
						}

						// Correct parent has 1 less indentation
						if (Regex.Matches(grammar[j], @"\t").Count == layer - 1) {
							parent_name = RemovePolarWhitespace(grammar[j]);
							break;
						}
					}
					if (parent_name == "UNKNOWN") {
						throw new Exception("ParentNotFoundException");
					}
				}

				// Datatype - Index 3
				if (type == "Key" || type == "Collection") {
					datatype = "STRING";
				}
				else {
					datatype = RemovePolarWhitespace(grammar[i]); // Directly assign Value
				}

				// Layer - Index 4
				// Pending removal

				// Any Name - Index 5
				if (Regex.IsMatch(grammar[i], @"%")) {
					any_name = "TRUE";
				}
				else {
					any_name = "FALSE";
				}

				// Required - Index 6
				if (Regex.IsMatch(name, @"ItemName") && parent_name == "ItemAttributes{}") {
					required = "TRUE";
				}
				else {
					required = "FALSE";
				}

				// Special Case: Subtree Operations
				if (Regex.IsMatch(name, @"@")) {

					string subtree_name = Regex.Replace(name, @"@", "");

					foreach (List<string> subtree in available_subtrees) {

						// Find correct subtree to use
						if (subtree[0] == subtree_name) {

							// Scan through subtree
							for (int q = 0; q < subtree.Count; q++) {

								// Remove Comments
								subtree[q] = Regex.Replace(subtree[q], @"\/\/.*[\s]*$", "");

								// Get Subtree Level
								int sub_layer = Regex.Matches(subtree[q], @"\t").Count;

								// Remove Trailing Whitespace
								subtree[q] = Regex.Replace(subtree[q], @"\s+$", "");

								// Skip if blank line
								if (subtree[q] == "") {
									continue;
								}

								// Subtree Type - Index 0
								if (Regex.IsMatch(subtree[q], @"{}")) {
									type = "Collection";
								}
								else if (subtree[q] == subtree[q].ToUpper()) {
									type = "Value";
								}
								else {
									type = "Key";
								}

								// Subtree Name - Index 1
								name = RemovePolarWhitespace(subtree[q]);

								// Subtree Parent Name - Index 2
								if (name == RemovePolarWhitespace(subtree[0])) {
									parent_name = "SUB UNKNOWN HEAD PARENT";

									// Look back in main tree
									for (int j = i; j > 0; j--) {

										// Skip Blank Lines
										if (grammar[j] == "") {
											continue;
										}

										// Correct parent has 1 less indentation
										if (Regex.Matches(grammar[j], @"\t").Count == layer - 1) {
											parent_name = RemovePolarWhitespace(grammar[j]);
											break;
										}
									}

									if (parent_name == "SUB UNKNOWN HEAD PARENT") {
										throw new Exception("HeadSubParentNotFoundException");
									}
								}
								else if (type == "Value") {
									parent_name = RemovePolarWhitespace(subtree[q - 1]);
								}
								else {
									parent_name = "SUB UNKNOWN PARENT";

									// Look back in sub tree
									for (int j = q; j >= 0; j--) {

										// Skip Blank Lines
										if (subtree[j] == "") {
											continue;
										}

										// Correct parent has 1 less indentation
										if (Regex.Matches(subtree[j], @"\t").Count == sub_layer - 1) {
											parent_name = RemovePolarWhitespace(subtree[j]);
											break;
										}
									}

									if (parent_name == "SUB UNKNOWN PARENT") {
										throw new Exception("SubParentNotFoundException");
									}
								}

								// Subtree Datatype - Index 3
								if (type == "Key" || type == "Collection") {
									datatype = "STRING";
								}
								else {
									datatype = RemovePolarWhitespace(subtree[q]); // Directly assign Value
								}

								// Subtree Layer - Index 4
								// layer + sublayer;

								// Subtree Any Name - Index 5
								if (Regex.IsMatch(subtree[q], @"%")) {
									any_name = "TRUE";
								}
								else {
									any_name = "FALSE";
								}

								// Subtree Required - Index 6
								if (Regex.IsMatch(name, @"ItemName") && parent_name == "ItemAttributes{}") {
									required = "TRUE";
								}
								else {
									required = "FALSE";
								}

								/* Node Operations via Sub Layer */

								// Move to parent, no limit
								if (Int32.Parse(current_node.Value[4]) >= (layer + sub_layer)) {
									int move_up = Int32.Parse(current_node.Value[4]) - (layer + sub_layer) + 1;

									for (int t = 0; t < move_up; t++) {
										current_node = current_node.Parent;
									}
								}
								// Move cursor to child, limit 1
								else if (Int32.Parse(current_node.Value[4]) < (layer + sub_layer) - 1) {
									current_node = current_node.Children.Last();
								}

								// Add Node to Parse Tree
								current_node.Add(new TreeNode<string[]>(new string[] { type, name, parent_name, datatype, (layer + sub_layer).ToString(), any_name, required }));

							}
						}
					}
					continue;
				}

				/* Node Operations */

				// Check if need to move cursor
				// Can only move deeper at most 1 at a time
				// Can move up unlimited


				// Move to parent, no limit
				if (Int32.Parse(current_node.Value[4]) >= layer) {
					int move_up = Int32.Parse(current_node.Value[4]) - layer + 1;
					for (int t = 0; t < move_up; t++) {
						current_node = current_node.Parent;
					}
				}
				// Move cursor to child, limit 1
				else if (Int32.Parse(current_node.Value[4]) < layer - 1) {
					current_node = current_node.Children.Last();
				}

				// Add Node to Parse Tree
				current_node.Add(new TreeNode<string[]>(new string[] { type, name, parent_name, datatype, layer.ToString(), any_name, required }));
			}

			// Move to top of tree
			int go_up = Int32.Parse(current_node.Value[4]);
			for (int r = 0; r <= go_up; r++) {
				current_node = current_node.Parent;
			}
		}

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

		// Moves cursor node to child by index
		/* Throws
		 * ChildNotFoundException
		 */
		public void Move(int index) {
			if (index >= current_node.Children.Count) {
				throw new Exception("ChildNotFoundException");
			}
			current_node = current_node.Children[index];
		}

		// Moves cursor node to child by child Value[1]|Name
		/* Throws
		 * ChildNotFoundException
		 */
		public void Move(string child_name) {
			bool moved = false;
		
			foreach (TreeNode<string[]> node in current_node.Children) {
				if (node.Value[1].ToUpper() == child_name.ToUpper()) {
					current_node = node;
					moved = true;
					break;
				}
			}

			if (!moved) {
				throw new Exception("ChildNotFoundException");
			}
		}

		public void MoveUp() {
			try {
				current_node = current_node.Parent;
			}
			catch {
				throw new Exception("ParentNotFoundException");
			}
		}

		public List<string> ChildrenValues() {
			List<string> c = new List<string>();
			foreach (TreeNode<string[]> child in current_node.Children) {
				if (child.Value[0] == "Value") {
					c.Add(child.Value[1]); // Name of current_node's child
				}
			}
			return c;
		}

		public List<string> ChildrenNames() {
			List<string> c = new List<string>();
			foreach (TreeNode<string[]> child in current_node.Children) {
				c.Add(child.Value[1]); // Name of current_node's child
			}
			return c;
		}

		public TreeNode<string[]> Current {
			get {
				return current_node;
			}
		}

		public string[] CurrentValue {
			get {
				return current_node.Value;
			}
		}

		public TreeNode<string[]> Parent {
			get {
				return current_node.Parent;
			}
		}

		public string[] ParentValue {
			get {
				return current_node.Parent.Value;
			}
		}
	}
}
