using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;

namespace PseudoPopParser {

	class PopulationErrorListener : BaseErrorListener {

		public override void ReportAmbiguity([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, bool exact, [Nullable] BitSet ambigAlts, [NotNull] ATNConfigSet configs) {
			base.ReportAmbiguity(recognizer, dfa, startIndex, stopIndex, exact, ambigAlts, configs);
		}

		public override void ReportAttemptingFullContext([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, [Nullable] BitSet conflictingAlts, [NotNull] SimulatorState conflictState) {
			base.ReportAttemptingFullContext(recognizer, dfa, startIndex, stopIndex, conflictingAlts, conflictState);
		}

		public override void ReportContextSensitivity([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, int prediction, [NotNull] SimulatorState acceptState) {
			base.ReportContextSensitivity(recognizer, dfa, startIndex, stopIndex, prediction, acceptState);
		}

		public override void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e) {
			uint code;
			if (e != null) {
				uint.TryParse(e.HelpLink, out code);
			}
			else {
				code = 999;
			}
			string issue = offendingSymbol.Text.Replace("\n", @"\n").Replace("\r", @"\r").Replace("\t", @"\t");
			Error.Write(msg, offendingSymbol.Line, code, issue);
		}

	}

	public class PopulationLexerErrorListener<Symbol> : ConsoleErrorListener<Symbol> {
		public override void SyntaxError(IRecognizer recognizer, Symbol offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
			uint code = 506; // Lexer Error
			string issue = msg.Substring(29).Trim('\''); // msg = @"token recognition error at: 'BADTOKEN'
			Error.Write("{f:Red}{b:DarkMagenta}Invalid Token{r}{b:DarkMagenta} at '{f:Red}{$0}{r}{b:DarkMagenta}'{r}", line, code, issue);
		}
	}
}
