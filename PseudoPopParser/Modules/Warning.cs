using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {

	class Warning {

		private static string ReadString(string s) { return Program._INI.Read(s); }
		private static bool ReadBool(string s) { return Program._INI.ReadBool(s); }
		private static int ReadInt(string s) { return Program._INI.ReadInt(s); }
		private static double ReadDouble(string s) { return Program._INI.ReadDouble(s); }
		private static uint WarningCount = 0;

		public static uint Warnings { get { return WarningCount; } }
		public static void IncrementWarnings() { WarningCount++; }

		public static void CreditMultiple(int credits, int waves, int multiple) {
			string config_key = "int_warn_credits_multiple";
			uint code = 0101;
			if (ReadInt(config_key) <= 0) return;
			PrintColor.Warn("{f:Cyan}Wave {1}{r}'s credits is {f:Yellow}not a multiple{r} of {f:Cyan}{2}{r}: '{f:Yellow}{0}{r}'", -1, code, credits.ToString(), waves.ToString(), multiple.ToString());
		}

		public static void TotalCreditGreater30000(int credits) {
			string config_key = "bool_warn_credits_gr_30000";
			uint code = 0102;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}Total Possible Credits{r} exceeds maximum possible reading of {f:Cyan}30000{r}: '{f:Yellow}{0}{r}'", -1, code, credits.ToString());
		}

		public static void WaitForAllMissing(int line, string expected_name) {
			string config_key = "bool_warn_wait_for_all_not_found";
			uint code = 0301;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}WaitForAll*{r} name does not exist: '{f:Yellow}{0}{r}'", line.ToString(), code, expected_name);
		}

		public static void InvalidAttributeKey(int line, string key) {
			string config_key = "bool_warn_invalid_item_char_attribute";
			uint code = 0201;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Yellow}Invalid{r} {f:Cyan}Attribute Name{r} found: '{f:Yellow}{0}{r}'", line, code, key);
		}

		// Unused
		public static void InvalidNumberValue(int line, string key, string value) {
			string config_key = "bool_warn_expected_decimal";
			uint code = 0401;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}Attribute{r} has invalid {f:Cyan}number value{r}: '{f:Yellow}{0} {1}{r}'", line, code, key, value);
		}

		public static void AttributeAdd0(int line, string key, string value) {
			string config_key = "bool_warn_attribute_value_type_scan";
			uint code = 0202;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f.Cyan}Attribute{r} does nothing: '{f:Yellow}{0} {1}{r}'", line, code, key, value);

			if (ReadBool(("bool_show_potential_fix"))) {
				PrintColor.PotentialFix("Value adds 0 to attribute");
			}
		}

		public static void AttributeMultiply1(int line, string key, string value) {
			string config_key = "bool_warn_attribute_value_type_scan";
			uint code = 0202;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f.Cyan}Attribute does nothing: '{f:Yellow}{0} {1}{r}'", line, code, key, value);

			if (ReadBool(("bool_show_potential_fix"))) {
				PrintColor.PotentialFix("Value multiplies attribute by 1.00");
			}
		}

		public static void AttributeOnly1Or0(int line, string key, string value) {
			string config_key = "bool_warn_attribute_value_type_scan";
			uint code = 0204;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}Attribute{r} can only be values {f:Cyan}0 or 1{r}: '{f:Yellow}{0}{r}'", line, code, key, value); // TODO BADVALUE
		}

		public static void TotalCurrencyEqual0(int line, string value) {
			string config_key = "bool_warn_totalcurrency_0";
			uint code = 0103;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}TotalCurrency{r} value {f:Yellow}equal to 0{r} drops nothing: '{f:Yellow}{0}{r}'", line, code, value);
		}

		public static void TFBotHealthMultiple(int line, int health, int multiple_of) {
			string config_key = "int_bot_health_multiple";
			uint code = 0205;
			if (ReadInt(config_key) == -1) return;
			PrintColor.Warn("{f:Cyan}TFBot Health{r} is {f:Yellow}not a multiple{r} of {f:Cyan}{1}{r}: '{f:Yellow}{0}{r}'", line, code, health.ToString(), multiple_of.ToString());
		}

		public static void TankHealthMultiple(int line, int health, int multiple_of) {
			string config_key = "int_tank_health_multiple";
			uint code = 0206;
			if (ReadInt(config_key) == -1) return;
			PrintColor.Warn("{f:Cyan}Tank Health{r} is {f:Yellow}not a multiple{r} of {f:Cyan}{1}{r}: '{f:Yellow}{0}{r}'", line, code, health.ToString(), multiple_of.ToString());
		}

		public static void TankHealthExceed(int line, int health, int max) {
			string config_key = "int_tank_warn_maximum";
			uint code = 0207;
			if (ReadInt(config_key) == -1) return;
			PrintColor.Warn("{f:Cyan}Tank Health{r} {f:Yellow}exceeds maximum{r} warning [{f:Cyan}{1}{r}]: '{f:Yellow}{0}{r}'", line, code, health.ToString(), max.ToString());
		}

		public static void TankHealthBelow(int line, int health, int min) {
			string config_key = "int_tank_warn_minimum";
			uint code = 0208;
			if (ReadInt(config_key) == -1) return;
			PrintColor.Warn("{f:Cyan}Tank Health{r} is {f:Yellow}{2} minimum{r} warning [{f:Cyan}{1}{r}]: '{f:Yellow}{0}{r}'", line, code, health.ToString(), min.ToString(), "below");
		}

		public static void TankDeploy(int line, string name) {
			string config_key = "bool_warn_tank_name_tankboss";
			uint code = 0213;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}Tank{r} not named '{f:Green}TankBoss{r}' {f:Yellow}does not explode{r} on {f:Yellow}deployment{r}: '{f:Yellow}{0}{r}'", line, code, name);
		}

		public static void ItemInvalid(int line, string name) {
			string config_key = "bool_warn_invalid_item_name";
			uint code = 0209;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Yellow}Invalid{r} TF2 {f:Cyan}Item Name{r}: '{f:Yellow}{0}{r}'", line, code, name);
		}

		public static void ItemMissing(int line, string name) {
			string config_key = "bool_warn_tfbot_missing_item";
			uint code = 0210;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}TFBot{r} does not have {f:Cyan}item{r}: '{f:Yellow}{0}{r}'", line, code, name);
		}

		public static void TemplateInvalid(int line, string name) {
			string config_key = "bool_warn_bad_template";
			uint code = 0211;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}Template{r} does not exist: '{f:Yellow}{0}{r}'", line, code, name);
		}

		// Deprecated - Default OFF
		public static void SupportLimited(int line) {
			string config_key = "bool_warn_support_limited";
			uint code = 0304;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}Support{r} '{f:Yellow}limited{r}' {f:Yellow}disables infinite{r} spawn", line, code);
		}

		// Unused
		public static void FloatBadDecimal(int line, string token, double actual) {
			string config_key = "bool_warn_bad_float";
			uint code = 0401;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("Bad decimal value: '{f:Yellow}{0}{r}' will be interpreted as '{f:Yellow}{1}{r}'", line, code, token, actual.ToString());
		}

		// Unused
		public static void FloatReinterpretedFromTo(int line, string from, int to) {
			string config_key = "bool_warn_bad_float_reinterpret";
			uint code = 0402;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("Decimal value will be reinterpreted from '{f:Yellow}{0}{r}' to '{f:Yellow}{1}{r}'", line, code, from, to.ToString());
		}

		public static void NegativeInterpreted0(int line, string token) {
			string config_key = "bool_warn_negative_value_becomes_0";
			uint code = 0403;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}Negative value{r} will be interpreted as{f:Yellow} 0{r}: '{f:Yellow}{0}{r}'", line, code, token);
		}

		public static void PrematureEndWaveSchedule(int line, string token) {
			string config_key = "bool_warn_early_end_wave_schedule";
			uint code = 0302;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("Possible {f:Yellow}premature end{r} of {f:Cyan}WaveSchedule{r} detected near '{f:Yellow}{0}{r}'", line, code, token);

			if (ReadBool(("bool_show_potential_fix"))) {
				PrintColor.PotentialFix("Remove additional lines after end of WaveSchedule");
				PrintColor.PotentialFix("Recount Curly Brackets");
			}
		}

		public static void MinMaxIntervalStopSpawn(int line) {
			string config_key = "bool_warn_min_max_interval_stop_spawn";
			uint code = 0303;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("Using {f:Cyan}When{r} with {f:Cyan}MinInterval{r} and {f:Cyan}MaxInterval{r} may stop spawning midwave", line, code);
		}

		public static void TFBotNameBadCharacter(int line) {
			string config_key = "bool_warn_tfbot_bad_character";
			uint code = 0212;
			if (!ReadBool(config_key)) return;
			PrintColor.Warn("{f:Cyan}Bot name{r} cannot not display symbol: '{f:Yellow}%{r}'", line, code);

			if (ReadBool(("bool_show_potential_fix"))) {
				PrintColor.PotentialFix("HUD cannot display this symbol.");
			}
		}
	}
}
