﻿using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PseudoPopParser {
	public class ValueParser {
		private static string FirstTerm(string Value) => Regex.Split(Value.Trim('"').Trim(), @"\s")[0];

		public static bool Flag() => true;
		public static string String(string Value) => Value.Trim('"');

		public static double Double(string Value, ParserRuleContext context) {
			string Original = Value;
			double Cast = 0.0d;
			try {
				Cast = Convert.ToDouble(FirstTerm(Value));
			}
			catch {
				Cast = 0.0d;
			}

			if (Convert.ToDouble(Cast).ToString() != Original.Trim('"') && !Original.Trim('"').EndsWith("0")) { // Input doesn't match cast and input doesn't end with a zero (cast removes lowest placevalue zeros)
				Warning.Write("{f:Yellow}Incompatible value type{r}. Expecting <{f:Yellow}Floating Point{r}>: '{f:Yellow}{$0}{r}'", context.Stop.Line, 310, Value);

			}
			return Cast;
		}

		// TODO Determine correct boolean value
		public static bool Boolean(string Value, ParserRuleContext context) {
			string Original = Value;
			bool Cast = false;
			try {
				Cast = FirstTerm(Value) == "1" ? true : false;
			}
			catch {
				Cast = false;
			}

			if (Convert.ToInt32(Cast).ToString() != Original.Trim('"')) {
				Warning.Write("{f:Yellow}Incompatible value type{r}. Expecting <{f:Yellow}Boolean{r}>: '{f:Yellow}{$0}{r}'", context.Stop.Line, 310, Value);

			}
			return Cast;
		}

		public static uint UnsignedInteger(string Value, ParserRuleContext context) {
			string Original = Value;
			uint Cast = 0;
			try {	
				double d = Convert.ToDouble(FirstTerm(Value));
				if (d < 0) Cast = 0;
				else Cast = (uint)d;
			}
			catch {
				Cast = 0;
			}
			if (Cast.ToString() != Value.Trim('"')) {
				Warning.Write("{f:Yellow}Incompatible value type{r}. Expecting <{f:Yellow}Unsigned Integer{r}>: '{f:Yellow}{$0}{r}'", context.Stop.Line, 310, Value);
			}
			return Cast;
		}

		public static int Integer(string Value, ParserRuleContext context) {
			string Original = Value;
			int Cast = 0;
			try {
				double d = Convert.ToDouble(FirstTerm(Value));
				Cast = (int)d;
			}
			catch {
				Cast = 0;
			}
			if (Cast.ToString() != Original.Trim('"')) {
				Warning.Write("{f:Yellow}Incompatible value type{r}. Expecting <{f:Yellow}Integer{r}>: '{f:Yellow}{$0}{r}'", context.Stop.Line, 310, Value);

			}
			return Cast;
		}
	}
}
