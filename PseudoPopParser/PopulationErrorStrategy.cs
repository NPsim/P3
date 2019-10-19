using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;

namespace PseudoPopParser {
	class PopulationErrorStrategy : DefaultErrorStrategy {
		public override bool InErrorRecoveryMode(Parser recognizer) {
			return base.InErrorRecoveryMode(recognizer);
		}

		public override void Recover(Parser recognizer, RecognitionException e) {
			//DoubleTokenDeletion(recognizer);
			//ReportUnwantedToken(recognizer);
			base.Recover(recognizer, e);
			//Sync(recognizer);
		}

		public override IToken RecoverInline(Parser recognizer) {
			// SINGLE TOKEN DELETION
            IToken matchedSymbol = SingleTokenDeletion(recognizer);
            if (matchedSymbol != null)
            {
				// we have deleted the extra token.
				// now, move past ttype token as if all were ok
				////Console.WriteLine("2Consuming: " + recognizer.CurrentToken.Text);
				////recognizer.Consume();
				IntervalSet expecting = recognizer.GetExpectedTokens();
				IntervalSet whatFollowsLoopIterationOrRule = expecting.Or(GetErrorRecoverySet(recognizer));
				Console.WriteLine("From Recover");
				ConsumeUntil(recognizer, whatFollowsLoopIterationOrRule);
				return matchedSymbol;
            }
            // SINGLE TOKEN INSERTION
            if (SingleTokenInsertion(recognizer))
            {
                return GetMissingSymbol(recognizer);
            }
            // even that didn't work; must throw the exception
            throw new InputMismatchException(recognizer);
		}

		public override void ReportError(Parser recognizer, RecognitionException e) {
			// if we've already reported an error and have not matched a token
			// yet successfully, don't report any errors.
			if (InErrorRecoveryMode(recognizer)) {
				//			System.err.print("[SPURIOUS] ");
				return;
			}
			// don't report spurious errors
			BeginErrorCondition(recognizer);
			if (e is NoViableAltException) {
				//ReportNoViableAlternative(recognizer, (NoViableAltException)e);
				Error.Write("{b:Red}{f:White}SYNTAX ERROR: {$0}{r}", e.OffendingToken.Line, 999, e.GetType().FullName);
				NotifyErrorListeners(recognizer, e.Message, e);
			}
			else {
				if (e is InputMismatchException) {
					//ReportInputMismatch(recognizer, (InputMismatchException)e);
				}
				else {
					if (e is FailedPredicateException) {
						//ReportFailedPredicate(recognizer, (FailedPredicateException)e);
						Error.Write("{b:Red}{f:White}SYNTAX ERROR: {$0}{r}", e.OffendingToken.Line, 999, e.GetType().FullName);
						NotifyErrorListeners(recognizer, e.Message, e);
					}
					else {
						Error.Write("{b:Red}{f:White}SYNTAX ERROR: {$0}{r}", e.OffendingToken.Line, 999, e.GetType().FullName);
						NotifyErrorListeners(recognizer, e.Message, e);
					}
				}
			}
		}

		public override void ReportMatch(Parser recognizer) {
			base.ReportMatch(recognizer);
		}

		public override void Reset(Parser recognizer) {
			base.Reset(recognizer);
		}

		public override void Sync(Parser recognizer) {
			//Console.WriteLine("\t" + recognizer.CurrentToken.Text);
			ATNState s = recognizer.Interpreter.atn.states[recognizer.State];
			//		System.err.println("sync @ "+s.stateNumber+"="+s.getClass().getSimpleName());
			// If already recovering, don't try to sync
			if (InErrorRecoveryMode(recognizer)) {
				return;
			}
			ITokenStream tokens = ((ITokenStream)recognizer.InputStream);
			int la = tokens.La(1);
			// try cheaper subset first; might get lucky. seems to shave a wee bit off
			var nextTokens = recognizer.Atn.NextTokens(s);
			if (nextTokens.Contains(TokenConstants.Epsilon) || nextTokens.Contains(la)) {
				return;
			}
			switch (s.StateType) {
				case StateType.BlockStart:
				case StateType.StarBlockStart:
				case StateType.PlusBlockStart:
				case StateType.StarLoopEntry: /*{ No special treatment for the first token
					// report error and recover if possible
					if (SingleTokenDeletion(recognizer) != null) {
						return;
					}
					throw new InputMismatchException(recognizer);
				}*/

				case StateType.PlusLoopBack:
				case StateType.StarLoopBack: {
					//			System.err.println("at loop back: "+s.getClass().getSimpleName());
					//ReportUnwantedToken(recognizer);
					IntervalSet expecting = recognizer.GetExpectedTokens();
					IntervalSet whatFollowsLoopIterationOrRule = expecting.Or(GetErrorRecoverySet(recognizer));
					ConsumeUntil(recognizer, expecting);
					break;
				}

				default: {
					// do nothing if we can't identify the exact kind of ATN state
					break;
				}
			}
		}

		protected override void BeginErrorCondition([NotNull] Parser recognizer) {
			base.BeginErrorCondition(recognizer);
		}

		protected override IToken ConstructToken(ITokenSource tokenSource, int expectedTokenType, string tokenText, IToken current) {
			return base.ConstructToken(tokenSource, expectedTokenType, tokenText, current);
		}

		static private string PreviousToken = "";
		protected override void ConsumeUntil([NotNull] Parser recognizer, [NotNull] IntervalSet set) {
			//Console.WriteLine("6");
			int ttype = ((ITokenStream)recognizer.InputStream).La(1);
			while (ttype != TokenConstants.Eof && !set.Contains(ttype)) {
				//Console.WriteLine("6Consuming: " + recognizer.CurrentToken.Text);
				if (PreviousToken.Length == 0 || true) {
					//recognizer.NotifyErrorListeners(recognizer.CurrentToken, "default", null);
					ReportInputMismatch(recognizer, new InputMismatchException(recognizer));
					PreviousToken = recognizer.CurrentToken.Text;
				}
				else {
					PreviousToken = "";
				}
				recognizer.Consume();
				ttype = ((ITokenStream)recognizer.InputStream).La(1);
			}
			PreviousToken = "";
		}

		protected override void EndErrorCondition([NotNull] Parser recognizer) {
			//Console.WriteLine("7");
			base.EndErrorCondition(recognizer);
		}

		[return: NotNull]
		protected override string EscapeWSAndQuote([NotNull] string s) {
			return base.EscapeWSAndQuote(s);
		}

		[return: NotNull]
		protected override IntervalSet GetErrorRecoverySet([NotNull] Parser recognizer) {
			return base.GetErrorRecoverySet(recognizer);
		}

		[return: NotNull]
		protected override IntervalSet GetExpectedTokens([NotNull] Parser recognizer) {
			return base.GetExpectedTokens(recognizer);
		}

		[return: NotNull]
		protected override IToken GetMissingSymbol([NotNull] Parser recognizer) {
			return base.GetMissingSymbol(recognizer);
		}

		protected override string GetSymbolText([NotNull] IToken symbol) {
			return base.GetSymbolText(symbol);
		}

		protected override int GetSymbolType([NotNull] IToken symbol) {
			return base.GetSymbolType(symbol);
		}

		protected override string GetTokenErrorDisplay(IToken t) {
			return base.GetTokenErrorDisplay(t);
		}

		protected override void NotifyErrorListeners([NotNull] Parser recognizer, string message, RecognitionException e) {
			base.NotifyErrorListeners(recognizer, message, e);
		}

		protected override void ReportFailedPredicate([NotNull] Parser recognizer, [NotNull] FailedPredicateException e) {
			base.ReportFailedPredicate(recognizer, e);
		}

		protected override void ReportInputMismatch([NotNull] Parser recognizer, [NotNull] InputMismatchException e) {
			string msg = "{f:Red}Unexpected symbol{r} found near '{f:Red}{$0}{r}'";
			e.HelpLink = "501";
			//Console.WriteLine("\tExpecting: " + e.GetExpectedTokens().ToString(recognizer.Vocabulary));
			if (e.GetExpectedTokens().ToString(recognizer.Vocabulary) == "<EOF>") {
				msg = "Expected {f:Red}End of File{r} but '{f:Red}{$0}{r}' was found.";
				e.HelpLink = "504";
				if (!Program.Config.ReadBool("bool_error_expected_eof")) {
					return;
				}
			}
			NotifyErrorListeners(recognizer, msg, e);
		}

		protected override void ReportMissingToken([NotNull] Parser recognizer) {
			if (InErrorRecoveryMode(recognizer)) {
				return;
			}
			BeginErrorCondition(recognizer);
			IToken t = recognizer.CurrentToken;
			IntervalSet expecting = GetExpectedTokens(recognizer);
			string msg = "Expected {f:Red}<" + expecting.ToString(recognizer.Vocabulary).Trim('\'') + ">{r} at '{f:Red}" + GetTokenErrorDisplay(t).Trim('\'') + "{r}'";
			NoViableAltException e = new NoViableAltException(recognizer) {
				HelpLink = "505"
			};
			NotifyErrorListeners(recognizer, msg, e);
		}

		protected override void ReportNoViableAlternative([NotNull] Parser recognizer, [NotNull] NoViableAltException e) {
			base.ReportNoViableAlternative(recognizer, e);
		}

		/*protected override void ReportUnwantedToken([NotNull] Parser recognizer) {
			base.ReportUnwantedToken(recognizer);
		}*/

		[return: Nullable]
		protected override IToken SingleTokenDeletion([NotNull] Parser recognizer) {
			return base.SingleTokenDeletion(recognizer);
		}

		protected override bool SingleTokenInsertion([NotNull] Parser recognizer) {
			return base.SingleTokenInsertion(recognizer);
		}

		//[return: Nullable]
		/*protected IToken DoubleTokenDeletion([NotNull] Parser recognizer) {
			int nextTokenType = ((ITokenStream)recognizer.InputStream).La(2);
			IntervalSet expecting = GetExpectedTokens(recognizer);
			//if (expecting.Contains(nextTokenType)) {
				ReportUnwantedToken(recognizer);
				recognizer.Consume();
				ReportUnwantedToken(recognizer);
				recognizer.Consume();
				// simply delete extra token
				// we want to return the token we're actually matching
				IToken matchedSymbol = recognizer.CurrentToken;
				ReportMatch(recognizer);
			// we know current token is correct
			Console.WriteLine("Next Up: " + matchedSymbol.Text);
				return matchedSymbol;
			//}
			//return null;
		}*/
	}
}
