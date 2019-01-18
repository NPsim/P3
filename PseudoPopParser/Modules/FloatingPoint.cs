using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {
	internal class FloatingPoint {

		// UNUSED MODULE

		public static void Map(double value) {
			long l = BitConverter.DoubleToInt64Bits(value);
			string binary = Convert.ToString(l, 2).PadLeft(64, '0');

			Console.WriteLine("Value");
			Console.WriteLine(value);
			Console.WriteLine("Sign");
			Console.WriteLine("" + binary.Substring(0, 1));
			Console.WriteLine("Exponent");
			Console.WriteLine(" " + binary.Substring(1, 11));
			Console.WriteLine("Mantissa");
			Console.WriteLine("            " + binary.Substring(12));
			Console.WriteLine("Full");
			Console.WriteLine(binary);
		}

		public static string BinaryDouble(double value) {
			// Convert to long then binary string
			long l = BitConverter.DoubleToInt64Bits(value);
			return Convert.ToString(l, 2).PadLeft(64, '0');
		}

		public static bool Sign(double value) {
			return value < 0;
		}

		public static bool[] Exponent(double value) {
			string exp_str = BinaryDouble(value).Substring(1, 11);
			return exp_str.Select(b => b == '1').ToArray();
		}

		public static bool[] Mantissa(double value) {
			string exp_str = BinaryDouble(value).Substring(12);
			return exp_str.Select(b => b == '1').ToArray();
		}

		public static int Precision(dynamic value) {
			string[] split = Regex.Split(value.ToString(), @"\.");
			if (split.Count() == 1) { // No precision
				return 0;
			}
			else if (split.Count() > 2 || split[1].Length == 0) { // Multiple decimal points
				throw new Exception("Invalid decimal precision value. Not a decimal.");
			}
			return split[1].Length;
		}

		public static double Trail(double value) {
			int whole = (int)value;
			return value - whole;
		}

		public static double Normalize(double value) {
			while (value >= 10) {
				value /= 10;
			}
			return value;
		}

		public static double Overflow(double value) {

			// Return if value is whole
			if (Trail(value) == 0) {
				return value;
			}

			double normal = Normalize(value);
			//TODO: Midrange approximation
			if (Trail(value) > 0.9 || Trail(value) < 0.1) { // Must be near overflow to actually overflow
				bool[] mant = Mantissa(normal);
				if (mant[23]) { // Check Mantissa bit 29
					return Math.Round(normal); // Return rounded number to whole number due to overflow
				}
			}
			return value; // Return input value due to no overflow
		}

		public static bool IsOverflow(double value, out double actual) {
			actual = Overflow(value);
			return actual != value;
		}

		public static bool IsOverflow(double value) {
			return IsOverflow(value, out double actual);
		}

		public static int IntegerInterpCast(string token) {
			Double.TryParse(token, out double d);
			return (int)Overflow(d);
		}
	}
}
