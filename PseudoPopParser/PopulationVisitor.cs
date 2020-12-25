using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace PseudoPopParser {

	public class PopulationVisitor : PopulationBaseVisitor<object> {

		private PopFile Pop;
		private readonly List<string> LookBack = new List<string>();

		internal PopFile GetPopFile() => Pop;

		public object Visit([NotNull] IParseTree tree, CommonTokenStream tokens) {
			foreach(IToken t in tokens.GetTokens()) {
				LookBack.Add(t.Text);
			}
			return base.Visit(tree);
		}

		public override object VisitKey([NotNull] PopulationParser.KeyContext context) {
			return base.VisitKey(context);
		}

		public override object VisitValue([NotNull] PopulationParser.ValueContext context) {
			return base.VisitValue(context);
		}

		public override object VisitGeneric_kv([NotNull] PopulationParser.Generic_kvContext context) {

			// Only write invalid KV when in safe mode.
			if (Program.GetSafetyLevel() == Program.ParserSafetyLevel.SAFE) {
				Error.Write("{f:Red}Unexpected keyvalue{r} found near '{f:Red}{$0}{r}'", context.Start.Line, 510, context.Start.Text);
			}
			return base.VisitGeneric_kv(context);
		}

		public override object VisitGeneric_collection([NotNull] PopulationParser.Generic_collectionContext context) {

			// Only write invalid collections when in safe mode.
			if (Program.GetSafetyLevel() == Program.ParserSafetyLevel.SAFE) {
				Error.Write("{f:Red}Unexpected collection{r} found near '{f:Red}{$0}{r}'", context.Start.Line, 510, context.Start.Text);
			}
			return base.VisitGeneric_collection(context);
		}

		public override object VisitGeneric_collection_body([NotNull] PopulationParser.Generic_collection_bodyContext context) {
			return base.VisitGeneric_collection_body(context);
		}

		public override object VisitPopfile(PopulationParser.PopfileContext context) {
			this.Pop = new PopFile();
			return base.VisitPopfile(context);
		}

		private readonly List<Tuple<string, int>> WaitForAllTracker = new List<Tuple<string, int>>();
		public override object VisitEndc([NotNull] PopulationParser.EndcContext context) {
			string Parent = LookBack[context.Parent.SourceInterval.a];
			int ParentLine = ((ParserRuleContext)context.Parent).Start.Line;
			switch (ValueParser.String(Parent.ToUpper())) {
				case "TFBOT": { // Track valid inventory
					ItemTracker.VerifyModifications();
					break;
				}
				case "ITEMATTRIBUTES": { // Ensure that every ItemAttributes contains an ItemName key

					// Context enforcement for modded collection reuse
					ParserRuleContext ContextPPP = (ParserRuleContext)context.Parent.Parent.Parent;
					if (ContextPPP is PopulationParser.Tfbot_bodyContext || ContextPPP is PopulationParser.Template_bodyContext || ContextPPP is PopulationParser.Eventattributes_bodyContext) {
						ItemAttributes Attributes = ItemAttributesTracker[((ParserRuleContext)context.Parent).SourceInterval.a];
						if (Attributes.ItemName == null || Attributes.ItemName.Length == 0) {
							Warning.Write("{f:Yellow}ItemAttributes{r} missing {f:Yellow}ItemName{r} key.", ParentLine, 216);
						}
					}
					break;
				}
				case "WAVE": { // Verify all WaitForAll* lead to a valid WaveSpawn inside wave
					Wave LastWave = Pop.Population.LastWave;

					// Get all Names
					List<string> Names = new List<string>();
					foreach(WaveSpawn WS in LastWave.WaveSpawns) {
						if (!string.IsNullOrEmpty(WS.Name)) {
							Names.Add(WS.Name.ToUpper());
						}
					}

					// Compare WaitForAll* tracker with Names list
					foreach(Tuple<string, int> WaitForAll in WaitForAllTracker) { // Tuple<string: WaitForAll* Name, int:Associated line number>
						if (!Names.Contains(WaitForAll.Item1.ToUpper()) && Program.Config.ReadBool("bool_warn_wait_for_all_not_found")) { // Compare case insensitive
							Warning.Write("{f:Yellow}WaitForAll*{r} name does not exist in wave: '{f:Yellow}{$0}{r}'", WaitForAll.Item2, 301, WaitForAll.Item1);
						}
					}

					// Calculate Total Wave Credits (Warning)
					int ConfigMultiple = Program.Config.ReadInt("int_warn_credits_multiple");
					uint WaveTotalCurrency = 0;
					foreach (WaveSpawn WaveSpawn in LastWave.WaveSpawns) {
						WaveTotalCurrency += WaveSpawn.TotalCurrency;
					}
					
					// Warn for wave total credit multiple
					if (WaveTotalCurrency % ConfigMultiple != 0) {
						Warning.Write("{f:Yellow}Wave " + (Pop.Population.Waves.Count) + "{r}'s credits is {f:Yellow}not multiple{r} of {f:Yellow}" + ConfigMultiple + "{r}: '{f:Yellow}" + WaveTotalCurrency + "{r}'", -1, 101);
					}
					break;
				}
				default: {
					if (((ParserRuleContext)context.Parent.Parent).Start.Text.ToUpper() == "TEMPLATES") { // Line is end of T_TFBot // Clear ItemTracker
						string TemplateName = ((ParserRuleContext)context.Parent).Start.Text;
						ItemTracker.StoreTemplateAndClear(TemplateName);
					}
					else if (context.Start.Text == ((ParserRuleContext)context.Parent.Parent).Start.Text) { // Line is the end of WaveSchedule // Calculate total popfile credits
						uint PopulationTotalCurrency = Pop.Population.StartingCurrency;
						foreach(Wave Wave in Pop.Population.Waves) {
							uint WaveTotalCurrency = 0;
							foreach (WaveSpawn WaveSpawn in Wave.WaveSpawns) {
								WaveTotalCurrency += WaveSpawn.TotalCurrency;
							}
							PopulationTotalCurrency += WaveTotalCurrency;
						}
						if (PopulationTotalCurrency > 30000 && Program.Config.ReadBool("bool_warn_credits_gr_30000")) {
							Warning.Write("{f:Yellow}Total Possible Credits{r} exceeds maximum possible reading of {f:Yellow}30000{r}: '{f:Yellow}" + PopulationTotalCurrency + "{r}'", -1, 102);
						}
					}

					break;
				}
			}
			return base.VisitEndc(context);
		}

		readonly string[] DefaultBases = { "ROBOT_STANDARD.POP", "ROBOT_GIANT.POP", "ROBOT_GATEBOT.POP" };
		public override object VisitDirective([NotNull] PopulationParser.DirectiveContext context) {
			string Key = context.Start.Text;
			string Value = context.Stop.Text;

			switch (Key.ToUpper()) {
				case "#BASE": {
					string BasePopFile = ValueParser.String(Value);
					string BasePopFileFullPath = Program.FullPopFileDirectory + @"\" + BasePopFile;
					Pop.Bases.Add(BasePopFile);
					PrintColor.InfoLine("Base File - {f:Cyan}{$0}{r}", BasePopFile);

					if (DefaultBases.Contains(BasePopFile.ToUpper())) {
						BasePopFileFullPath = AppDomain.CurrentDomain.BaseDirectory + "BaseTemplates\\" + BasePopFile;
					}

					try {
						AntlrInputStream inputstream = new AntlrInputStream(System.IO.File.ReadAllText(BasePopFileFullPath));
						Program.LineCount += System.IO.File.ReadLines(BasePopFileFullPath).Count();

						PopulationLexer lexer = new PopulationLexer(inputstream);
						lexer.RemoveErrorListeners();
						lexer.AddErrorListener(new PopulationLexerErrorListener<int>());

						CommonTokenStream tokenstream = new CommonTokenStream(lexer);
						PopulationParser parser = new PopulationParser(tokenstream);
						parser.RemoveErrorListeners();
						parser.AddErrorListener(new PopulationErrorListener());
						parser.ErrorHandler = new PopulationErrorStrategy();

						PopulationParser.PopfileContext base_context = parser.popfile();
						PopulationVisitor visitor = new PopulationVisitor();
						visitor.Visit(base_context, tokenstream);
						PopFile popfile = visitor.GetPopFile();

						Pop.Population = Pop.Population ?? new Population();
						Pop.Population.Templates = Pop.Population.Templates ?? new Dictionary<string, dynamic>();

						// Import templates from base popfile
						foreach (KeyValuePair<string, dynamic> item in popfile.Population.Templates) {
							Pop.Population.Templates[item.Key.ToUpper()] = item.Value;
						}
					}
					catch (System.IO.FileNotFoundException) {
						Error.Write("Base file not found: {f:Red}{$0}{r}", context.Stop.Line, 802, BasePopFileFullPath);
					}
					catch {
						Error.Write("Failed to parse Base file '{f:Red}{$0}{r}'", context.Stop.Line, 801, BasePopFile);
					}

					PrintColor.InfoLine("\tDone Parsing Base - {f:Cyan}{$0}{r}", BasePopFile);
					break;
				}
				case "#INCLUDE": {
					Pop.Includes.Add(ValueParser.String(Value));
					break;
				}
			}

			return base.VisitDirective(context);
		}

		public override object VisitPopulation([NotNull] PopulationParser.PopulationContext context) {
			string Key = context.Start.Text;
			Pop.Population = Pop.Population ?? new Population();
			Pop.Population.PopulationName = Key;
			return base.VisitPopulation(context);
		}

		public override object VisitPopulation_body([NotNull] PopulationParser.Population_bodyContext context) {
			Population Node = Pop.Population;
			string Key = context.Start.Text;
			string Value = context.Stop.Text;

			switch (Key.ToUpper()) {
				case "STARTINGCURRENCY":
					Node.StartingCurrency = ValueParser.UnsignedInteger(Value, context);
					break;
				case "FIXEDRESPAWNWAVETIME":
					Node.FixedRespawnWaveTime = ValueParser.Flag();
					break;
				case "ADVANCED":
					Node.Advanced = ValueParser.Flag();
					break;
				case "EVENTPOPFILE":
					Node.EventPopFile = ValueParser.String(Value);
					if (ValueParser.String(Value).ToUpper() != "HALLOWEEN") {
						Warning.Write("EventPopFile key only supports \"Halloween\"", context.Stop.Line, 303);
					}
					break;
				case "CANBOTSATTACKWHILEINSPAWNROOM":
					string[] AttackSpawnValues = { "NO", "FALSE" };
					Node.CanBotsAttackWhileInSpawnRoom = !AttackSpawnValues.Contains(ValueParser.String(Value).ToUpper());
					break;
				case "RESPAWNWAVETIME":
					Node.RespawnWaveTime = ValueParser.UnsignedInteger(Value, context);
					break;
				case "ISENDLESS":
					Node.IsEndless = ValueParser.Flag();
					break;
				case "ADDSENTRYBUSTERWHENDAMAGEDEALTEXCEEDS":
					Node.AddSentryBusterWhenDamageDealtExceeds = ValueParser.UnsignedInteger(Value, context);
					break;
				case "ADDSENTRYBUSTERWHENKILLCOUNTEXCEEDS":
					Node.AddsentryBusterWhenKillCountExceeds = ValueParser.UnsignedInteger(Value, context);
					break;
				case "TEMPLATES":
					Node.Templates = Node.Templates ?? new Dictionary<string, dynamic>();
					break;
				case "MISSION":
					Node.NewMission();
					break;
				case "WAVE":
					Node.NewWave();
					WaitForAllTracker.Clear();
					break;
				case "RANDOMPLACEMENT":
					Node.NewRandomPlacement();
					break;
				case "PERIODICSPAWN":
					Node.NewPeriodicSpawn();
					break;
			}
			return base.VisitPopulation_body(context);
		}

		public override object VisitTemplates_body([NotNull] PopulationParser.Templates_bodyContext context) {
			string TemplateName = context.Start.Text;
			Pop.Population.Templates[TemplateName.ToUpper()] = new T_Generic();
			return base.VisitTemplates_body(context);
		}

		private readonly string[] T_GenericKeys = { "NAME", "TEMPLATE" };
		private readonly string[] T_TFBotKeys = { "ATTRIBUTES", "CLASS", "CLASSICON", "HEALTH", "SCALE", "AUTOJUMPMIN", "AUTOJUMPMAX", "SKILL", "WEAPONRESTRICTIONS", "BEHAVIORMODIFIERS", "MAXVISIONRANGE", "ITEM", "TELEPORTWHERE", "TAG", "ITEMATTRIBUTES", "CHARACTERATTRIBUTES", "EVENTCHANGEATTRIBUTES" };
		private readonly string[] T_WaveSpawnKeys = { "TOTALCURRENCY", "TOTALCOUNT", "WHERE", "MAXACTIVE", "SPAWNCOUNT", "SUPPORT", "WAITFORALLDEAD", "WAITFORALLSPAWNED", "WAITBEFORESTARTING", "WAITBETWEENSPAWNS", "WAITBETWEENSPAWNSAFTERDEATH", "RANDOMSPAWN", "STARTWAVEWARNINGSOUND", "STARTWAVEOUTPUT", "FIRSTSPAWNWARNINGSOUND", "FIRSTSPAWNOUTPUT", "LASTSPAWNWARNINGSOUND", "LASTSPAWNOUTPUT", "DONEWARNINGSOUND", "DONEOUTPUT" };
		public override object VisitTemplate_body([NotNull] PopulationParser.Template_bodyContext context) {
			string TemplateName = LookBack[context.Parent.SourceInterval.a];
			T_Generic Template = Pop.Population.Templates[TemplateName.ToUpper()];
			string Key = ValueParser.String(context.Start.Text);
			string Value = context.Stop.Text;

			// Detect [Initial] Template Type from First Key
			Template.TemplateType = Template.TemplateType ?? (T_TFBotKeys.Contains(Key.ToUpper()) ? "TFBOT" : null);
			Template.TemplateType = Template.TemplateType ?? (T_WaveSpawnKeys.Contains(Key.ToUpper()) ? "WAVESPAWN" : null);

			// Generic Template Keys
			if (T_GenericKeys.Contains(Key.ToUpper())) {
				switch (Key.ToUpper()) {
					case "NAME":
						Template.Name = ValueParser.String(Value);
						break;
					case "TEMPLATE":
						Template.Template = ValueParser.String(Value);
						break;
				}

				return base.VisitTemplate_body(context);
			}

			// Specific Keys
			if (Template.TemplateType == "TFBOT") {
				switch (Key.ToUpper()) {
					case "ATTRIBUTES":
						string[] Attributes = { "REMOVEONDEATH", "AGGRESSIVE", "SUPPRESSFIRE", "DISABLEDODGE", "BECOMESPECTATORONDEATH", "RETAINBUILDINGS", "SPAWNWITHFULLCHARGE", "ALWAYSCRIT", "IGNOREENEMIES", "HOLDFIREUNTILFULLRELOAD", "ALWAYSFIREWEAPON", "TELEPORTTOHINT", "MINIBOSS", "USEBOSSHEALTHBAR", "IGNOREFLAG", "AUTOJUMP", "AIRCHARGEONLY", "VACCINATORBULLETS", "VACCINATORBLAST", "VACCINATORFIRE", "BULLETIMMUNE", "BLASTIMMUNE", "FIREIMMUNE", "PARACHUTE", "PROJECTILESHIELD" };
						Template.Attributes.Add(ValueParser.String(Value));
						if (!Attributes.Contains(ValueParser.String(Value).ToUpper())) {
							Warning.Write("{f:Yellow}Invalid Attribute{r} found: '{f:Yellow}{$0}{r}'", context.Stop.Line, 214, Value);
						}
						break;
					case "CLASS":
						string[] Classes = { "SCOUT", "SOLDIER", "PYRO", "DEMOMAN", "HEAVY", "ENGINEER", "MEDIC", "SNIPER", "SPY" };
						bool IsValidClass = false;
						foreach (string Class in Classes) {
							if (System.Text.RegularExpressions.Regex.IsMatch(ValueParser.String(Value).ToUpper(), "^" + Class, System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
								Template.Class = Class.First() + Class.Substring(1).ToLower();
								ItemTracker.SetupClass(Template.Class);
								Template.Name = Template.Name ?? Template.Class;
								IsValidClass = true;
								break;
							}
						}
						if (!IsValidClass) {
							Warning.Write("Unexpected {f:Yellow}Class{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 304, Value);
						}
						break;
					case "CLASSICON":
						Template.ClassIcon = ValueParser.String(Value);
						break;
					case "HEALTH":
						Template.Health = ValueParser.UnsignedInteger(Value, context);
						int ConfigTFBotHealthMultiple = Program.Config.ReadInt("int_bot_health_multiple");
						if (Template.Health % ConfigTFBotHealthMultiple != 0) { // W0205
							Warning.Write("TFBot Template {f:Yellow}Health{r} not {f:Yellow}multiple of {$0}{r}: '{f:Yellow}{$1}{r}'", context.Stop.Line, 205, ConfigTFBotHealthMultiple.ToString(), Template.Health.ToString());
						}
						break;
					case "SCALE":
						Template.Scale = ValueParser.Double(Value, context);
						break;
					case "AUTOJUMPMIN":
						Template.AutoJumpMin = ValueParser.Double(Value, context);
						break;
					case "AUTOJUMPMAX":
						Template.AutoJumpMax = ValueParser.Double(Value, context);
						break;
					case "SKILL":
						string[] Skills = { "EASY", "NORMAL", "HARD", "EXPERT" };
						Template.Skill = ValueParser.String(Value);
						if (!Skills.Contains(ValueParser.String(Value).ToUpper())) {
							Warning.Write("Unexpected {f:Yellow}Skill{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 305, Value);
						}
						break;
					case "WEAPONRESTRICTIONS":
						string[] WeaponRestrictions = { "MELEEONLY", "PRIMARYONLY", "SECONDARYONLY" };
						Template.WeaponRestrictions = ValueParser.String(Value);
						if (!WeaponRestrictions.Contains(ValueParser.String(Value).ToUpper())) {
							Warning.Write("Unexpected {f:Yellow}WeaponRestrictions{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 306, Value);
						}
						break;
					case "BEHAVIORMODIFIERS":
						string[] BehaviorModifiers = { "MOBBER", "PUSH" };
						Template.BehaviorModifiers = ValueParser.String(Value);
						if (!BehaviorModifiers.Contains(ValueParser.String(Value).ToUpper())) {
							Warning.Write("Unexpected {f:Yellow}BehaviorModifiers{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 307, Value);
						}
						break;
					case "MAXVISIONRANGE":
						Template.MaxVisionRange = ValueParser.Double(Value, context);
						break;
					case "ITEM": {
						string ItemName = ValueParser.String(Value);
						Template.Items.Add(ItemName);
						ItemTracker.Add(ItemName, context.Stop.Line, Template.Class);
						break;
					}
					case "TELEPORTWHERE":
						Template.TeleportWheres.Add(ValueParser.String(Value));
						break;
					case "TAG":
						Template.Tags.Add(ValueParser.String(Value));
						break;
					case "ITEMATTRIBUTES":
						ItemAttributes ItemAttributes = new ItemAttributes();
						ItemAttributesTracker[context.SourceInterval.a] = ItemAttributes;
						Template.ItemAttributes.Add(ItemAttributes);
						break;
					case "CHARACTERATTRIBUTES":
						CharacterAttributes CharacterAttributes = new CharacterAttributes();
						CharacterAttributesTracker[context.SourceInterval.a] = CharacterAttributes;
						Template.CharacterAttributes.Add(CharacterAttributes);
						break;
					case "EVENTCHANGEATTRIBUTES":
						Template.EventChangeAttributes = Template.EventChangeAttributes ?? new Dictionary<string, EventChangeAttributes>();
						break;
					default:
						if (Program.GetSafetyLevel() == Program.ParserSafetyLevel.SAFE) {
							Error.Write("Attempted to {f:Red}mix Template{r} TFBot and WaveSpawn keys: '{f:Red}{$0} {$1}{r}'", context.Start.Line, 803, Key, Value);
						}
						break;
				}
			}
			else if (Template.TemplateType == "WAVESPAWN") {
				int TokenIndex = context.SourceInterval.a;
				switch (Key.ToUpper()) {
					case "TOTALCURRENCY":
						Template.TotalCurrency = ValueParser.UnsignedInteger(Value, context);
						break;
					case "TOTALCOUNT":
						Template.TotalCount = ValueParser.UnsignedInteger(Value, context);
						break;
					case "WHERE":
						Template.Where.Add(ValueParser.String(Value));
						break;
					case "MAXACTIVE":
						Template.MaxActive = ValueParser.UnsignedInteger(Value, context);
						break;
					case "SPAWNCOUNT":
						Template.SpawnCount = ValueParser.UnsignedInteger(Value, context);
						break;
					case "SUPPORT":
						Template.Support = ValueParser.Flag();
						Template.SupportLimited = ValueParser.String(Value).ToUpper() == "LIMITED";
						break;
					case "WAITFORALLDEAD":
						Template.WaitForAllDead = ValueParser.String(Value);
						break;
					case "WAITFORALLSPAWNED":
						Template.WaitForAllSpawned = ValueParser.String(Value);
						break;
					case "WAITBEFORESTARTING":
						Template.WaitBeforeStarting = ValueParser.Double(Value, context);
						break;
					case "WAITBETWEENSPAWNS":
						Template.WaitBetweenSpawns = ValueParser.Double(Value, context);
						break;
					case "WAITBETWEENSPAWNSAFTERDEATH":
						Template.WaitBetweenSpawnsAfterDeath = ValueParser.Double(Value, context);
						break;
					case "RANDOMSPAWN":
						Template.RandomSpawn = ValueParser.Boolean(Value, context);
						break;
					case "STARTWAVEWARNINGSOUND":
						Template.StartWaveWarningSound = ValueParser.String(Value);
						break;
					case "STARTWAVEOUTPUT":
						Template.StartWaveOutput = new LogicOutput("StartWaveOutput");
						OutputTracker[TokenIndex] = Template.StartWaveOutput;
						break;
					case "FIRSTSPAWNWARNINGSOUND":
						Template.FirstSpawnWarningSound = ValueParser.String(Value);
						break;
					case "FIRSTSPAWNOUTPUT":
						Template.FirstSpawnOutput = new LogicOutput("FirstSpawnOutput");
						OutputTracker[TokenIndex] = Template.FirstSpawnOutput;
						break;
					case "LASTSPAWNWARNINGSOUND":
						Template.LastSpawnWarningSound = ValueParser.String(Value);
						break;
					case "LASTSPAWNOUTPUT":
						Template.LastSpawnOutput = new LogicOutput("LastSpawnOutput");
						OutputTracker[TokenIndex] = Template.LastSpawnOutput;
						break;
					case "DONEWARNINGSOUND":
						Template.DoneWarningSound = ValueParser.String(Value);
						break;
					case "DONEOUTPUT":
						Template.DoneOutput = new LogicOutput("DoneOutput");
						OutputTracker[TokenIndex] = Template.DoneOutput;
						break;
					case "TFBOT":
					case "TANK":
					case "SENTRYGUN":
					case "SQUAD":
					case "MOB":
					case "RANDOMCHOICE":
						break;
					default:
						if (Program.GetSafetyLevel() == Program.ParserSafetyLevel.SAFE) {
							Error.Write("Attempted to {f:Red}mix Template{r} TFBot and WaveSpawn keys: '{f:Red}{$0} {$1}{r}'", context.Start.Line, 803, Key, Value);
						}
						break;
				}
			}

			return base.VisitTemplate_body(context);
		}

		public override object VisitMission_body([NotNull] PopulationParser.Mission_bodyContext context) {
			Mission Node = Pop.Population.LastMission;
			string Key = context.Start.Text;
			string Value = context.Stop.Text;

			switch (Key.ToUpper()) {
				case "OBJECTIVE":
					string[] Objectives = { "DESTROYSENTRIES", "SEEKANDDESTROY", "SNIPER", "SPY", "ENGINEER" };
					Node.Objective = ValueParser.String(Value);
					if (!Objectives.Contains(ValueParser.String(Value).ToUpper())) {
						Warning.Write("Unexpected {f:Yellow}Objective{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 308, Value);
					}
					break;
				case "INITIALCOOLDOWN":
					Node.InitialCooldown = ValueParser.Double(Value, context);
					break;
				case "WHERE":
					Node.Where.Add(ValueParser.String(Value));
					break;
				case "BEGINATWAVE":
					Node.BeginAtWave = ValueParser.UnsignedInteger(Value, context);
					break;
				case "RUNFORTHISMANYWAVES":
					Node.RunForThisManyWaves = ValueParser.Integer(Value, context);
					break;
				case "COOLDOWNTIME":
					Node.CooldownTime = ValueParser.Double(Value, context);
					break;
				case "DESIREDCOUNT":
					Node.CooldownTime = ValueParser.UnsignedInteger(Value, context);
					break;
			}
			return base.VisitMission_body(context);
		}

		public override object VisitWave_body([NotNull] PopulationParser.Wave_bodyContext context) {
			Wave Node = Pop.Population.LastWave;
			string Key = context.Start.Text;
			string Value = context.stop.Text;
			int TokenIndex = context.SourceInterval.a;

			switch (Key.ToUpper()) {
				case "DESCRIPTION":
					Node.Description = ValueParser.String(Value);
					break;
				case "SOUND":
					Node.Sound = ValueParser.String(Value);
					break;
				case "WAITWHENDONE":
					Node.WaitWhenDone = ValueParser.Integer(Value, context);
					break;
				case "CHECKPOINT":
					Node.Checkpoint = ValueParser.Flag();
					break;
				case "STARTWAVEOUTPUT":
					Node.StartWaveOutput = new LogicOutput("StartWaveOutput");
					OutputTracker[TokenIndex] = Node.StartWaveOutput;
					break;
				case "INITWAVEOUTPUT":
					Node.InitWaveOutput = new LogicOutput("InitWaveOutput");
					OutputTracker[TokenIndex] = Node.InitWaveOutput;
					break;
				case "DONEOUTPUT":
					Node.DoneOutput = new LogicOutput("DoneOutput");
					OutputTracker[TokenIndex] = Node.DoneOutput;
					break;
				case "WAVESPAWN":
					Pop.Population.LastWave.NewWaveSpawn();
					break;
			}
			return base.VisitWave_body(context);
		}

		public override object VisitWavespawn_body([NotNull] PopulationParser.Wavespawn_bodyContext context) {
			WaveSpawn Node = Pop.Population.LastWave.LastWaveSpawn; // WaveSpawn
			string Key = ValueParser.String(context.Start.Text);
			string Value = context.Stop.Text;

			switch (Key.ToUpper()) {
				case "NAME":
					Node.Name = ValueParser.String(Value);
					break;
				case "TEMPLATE":
					if (Node.Template != null) {
						Warning.Write("Cannot use {f:Yellow}multiple Templates{r}.", context.Stop.Line, 215);
						break;
					}
					Node.Template = ValueParser.String(Value);
					try {
						T_Generic Template = Pop.Population.Templates[Node.Template.ToUpper()];
						Node.MergeTemplate(Template);
					}
					catch (Exception ex) {
						if (ex.Message == "IncorrectTemplateTypeException") {
							Warning.Write("Invalid {f:Yellow}template type{r} given: '{f:Yellow}{$0}{r}'", context.Stop.Line, 214, Value);
						}
						else if (Program.Config.ReadBool("bool_warn_bad_template")) {
							Warning.Write("{f:Yellow}Template{r} does not exist: '{f:Yellow}{$0}{r}'", context.Stop.Line, 211, Value);
						}
					}
					break;
				case "TOTALCURRENCY":
					Node.TotalCurrency = ValueParser.UnsignedInteger(Value, context);
					break;
				case "TOTALCOUNT":
					Node.TotalCount = ValueParser.UnsignedInteger(Value, context);
					break;
				case "WHERE":
					Node.Where.Add(ValueParser.String(Value));
					break;
				case "MAXACTIVE":
					Node.MaxActive = ValueParser.UnsignedInteger(Value, context);
					break;
				case "SPAWNCOUNT":
					Node.SpawnCount = ValueParser.UnsignedInteger(Value, context);
					break;
				case "SUPPORT":
					Node.Support = ValueParser.Flag();
					Node.SupportLimited = ValueParser.String(Value).ToUpper() == "LIMITED";
					break;
				case "WAITFORALLDEAD":
					Node.WaitForAllDead = ValueParser.String(Value);
					WaitForAllTracker.Add(new Tuple<string, int>(ValueParser.String(Value), context.Stop.Line));
					break;
				case "WAITFORALLSPAWNED":
					Node.WaitForAllSpawned = ValueParser.String(Value);
					WaitForAllTracker.Add(new Tuple<string, int>(ValueParser.String(Value), context.Stop.Line));
					break;
				case "WAITBEFORESTARTING":
					Node.WaitBeforeStarting = ValueParser.Double(Value, context);
					break;
				case "WAITBETWEENSPAWNS":
					Node.WaitBetweenSpawns = ValueParser.Double(Value, context);
					break;
				case "WAITBETWEENSPAWNSAFTERDEATH":
					Node.WaitBetweenSpawnsAfterDeath = ValueParser.Double(Value, context);
					break;
				case "RANDOMSPAWN":
					Node.RandomSpawn = ValueParser.Boolean(Value, context);
					break;
				case "STARTWAVEWARNINGSOUND":
					Node.StartWaveWarningSound = ValueParser.String(Value);
					break;
				case "STARTWAVEOUTPUT":
					Node.StartWaveOutput = new LogicOutput("StartWaveOutput");
					OutputTracker[context.SourceInterval.a] = Node.StartWaveOutput;
					break;
				case "FIRSTSPAWNWARNINGSOUND":
					Node.FirstSpawnWarningSound = ValueParser.String(Value);
					break;
				case "FIRSTSPAWNOUTPUT":
					Node.FirstSpawnOutput = new LogicOutput("FirstSpawnOutput");
					OutputTracker[context.SourceInterval.a] = Node.FirstSpawnOutput;
					break;
				case "LASTSPAWNWARNINGSOUND":
					Node.LastSpawnWarningSound = ValueParser.String(Value);
					break;
				case "LASTSPAWNOUTPUT":
					Node.LastSpawnOutput = new LogicOutput("LastSpawnOutput");
					OutputTracker[context.SourceInterval.a] = Node.LastSpawnOutput;
					break;
				case "DONEWARNINGSOUND":
					Node.DoneWarningSound = ValueParser.String(Value);
					break;
				case "DONEOUTPUT":
					Node.DoneOutput = new LogicOutput("DoneOutput");
					OutputTracker[context.SourceInterval.a] = Node.DoneOutput;
					break;
			}
			return base.VisitWavespawn_body(context);
		}

		private readonly Dictionary<int, dynamic> SpawnerTracker = new Dictionary<int, dynamic>();
		public override object VisitSpawners([NotNull] PopulationParser.SpawnersContext context) {
			string Key = context.Start.Text;
			int TokenIndex = context.SourceInterval.a;
			int ParentTokenIndex = context.Parent.Parent.SourceInterval.a;
			string ParentToken = LookBack[ParentTokenIndex];
			dynamic NewSpawner = new Spawner();

			switch (Key.ToUpper()) {
				case "TFBOT":
					NewSpawner = new TFBot();
					ItemTracker.Clear();
					break;
				case "TANK":
					NewSpawner = new Tank();
					break;
				case "SENTRYGUN":
					NewSpawner = new SentryGun();
					break;
				case "SQUAD":
					NewSpawner = new Squad();
					break;
				case "MOB":
					NewSpawner = new Mob();
					break;
				case "RANDOMCHOICE":
					NewSpawner = new RandomChoice();
					break;
			}

			switch (ParentToken.ToUpper()) {
				case "SQUAD":
				case "MOB":
				case "RANDOMCHOICE":
					SpawnerTracker[ParentTokenIndex].Spawners.Add(NewSpawner);
					break;
				case "WAVESPAWN":
					Pop.Population.LastWave.LastWaveSpawn.Spawner = NewSpawner;
					break;
				case "MISSION":
					Pop.Population.LastMission.Spawner = NewSpawner;
					break;
				case "PERIODICSPAWN":
					Pop.Population.LastPeriodicSpawn.Spawner = NewSpawner;
					break;
				case "RANDOMPLACEMENT":
					Pop.Population.LastRandomPlacement.Spawner = NewSpawner;
					break; 
				default: // Spawner is inside a T_WaveSpawn
					string TemplateName = LookBack[context.Parent.Parent.SourceInterval.a];
					Pop.Population.Templates[TemplateName.ToUpper()].Spawner = NewSpawner;
					break;
			}

			SpawnerTracker[TokenIndex] = NewSpawner;
			return base.VisitSpawners(context);
		}

		public override object VisitSquad_body([NotNull] PopulationParser.Squad_bodyContext context) {
			string Key = context.Start.Text;
			string Value = context.Stop.Text;
			int ParentTokenIndex = context.Parent.Parent.SourceInterval.a;
			Squad Squad = SpawnerTracker[ParentTokenIndex];

			switch (Key.ToUpper()) {
				case "SHOULDPRESERVESQUAD":
					Squad.ShouldPreserveSquad = ValueParser.Boolean(Value, context);
					break;
				case "FORMATIONSIZE":
					Squad.FormationSize = ValueParser.Double(Value, context);
					break;
			}

			return base.VisitSquad_body(context);
		}

		public override object VisitRandomchoice_body([NotNull] PopulationParser.Randomchoice_bodyContext context) {
			return base.VisitRandomchoice_body(context); // Intentionally Empty
		}

		public override object VisitMob_body([NotNull] PopulationParser.Mob_bodyContext context) {
			string Key = context.Start.Text;
			string Value = context.Stop.Text;
			int ParentTokenIndex = context.Parent.Parent.SourceInterval.a;
			Mob Mob = SpawnerTracker[ParentTokenIndex];

			switch (Key.ToUpper()) {
				case "COUNT":
					Mob.Count = ValueParser.UnsignedInteger(Value, context);
					break;
			}

			return base.VisitMob_body(context);
		}

		public override object VisitTfbot_body([NotNull] PopulationParser.Tfbot_bodyContext context) {
			TFBot Node = SpawnerTracker[context.Parent.Parent.SourceInterval.a]; // TFBot
			string Key = context.Start.Text;
			string Value = context.Stop.Text;

			switch (Key.ToUpper()) {
				case "NAME":
					Node.Name = ValueParser.String(Value);
					if (Program.Config.ReadBool("bool_warn_tfbot_bad_character") && ValueParser.String(Value).Contains('%')) {
						Warning.Write("{f:Yellow}Bot name{r} cannot display symbol: '{f:Yellow}%{r}'", context.Stop.Line, 212);
					}
					break;
				case "TEMPLATE": {
					if (Node.Template != null) {
						Warning.Write("Cannot use {f:Yellow}multiple Templates{r}.", context.Stop.Line, 215);
						break;
					}
					Node.Template = ValueParser.String(Value);
					ItemTracker.ImportTemplate(Node.Template);

					try {
						T_Generic Template = Pop.Population.Templates[Node.Template.ToUpper()];
						Node.MergeTemplate(Template);
					}
					catch (Exception ex) {
						if (ex.Message == "IncorrectTemplateTypeException") {
							Warning.Write("Invalid {f:Yellow}template type{r} given: '{f:Yellow}{$0}{r}'", context.Stop.Line, 214, Value);
						}
						else if (Program.Config.ReadBool("bool_warn_bad_template")) {
							Warning.Write("{f:Yellow}Template{r} does not exist: '{f:Yellow}{$0}{r}'", context.Stop.Line, 211, Value);
						}
					}
					break;
				}
				case "ATTRIBUTES":
					string[] Attributes = { "REMOVEONDEATH", "AGGRESSIVE", "SUPPRESSFIRE", "DISABLEDODGE", "BECOMESPECTATORONDEATH", "RETAINBUILDINGS", "SPAWNWITHFULLCHARGE", "ALWAYSCRIT", "IGNOREENEMIES", "HOLDFIREUNTILFULLRELOAD", "ALWAYSFIREWEAPON", "TELEPORTTOHINT", "MINIBOSS", "USEBOSSHEALTHBAR", "IGNOREFLAG", "AUTOJUMP", "AIRCHARGEONLY", "VACCINATORBULLETS", "VACCINATORBLAST", "VACCINATORFIRE", "BULLETIMMUNE", "BLASTIMMUNE", "FIREIMMUNE", "PARACHUTE", "PROJECTILESHIELD" };
					Node.Attributes.Add(ValueParser.String(Value));
					if (!Attributes.Contains(ValueParser.String(Value).ToUpper())) {
						Warning.Write("{f:Yellow}Invalid Attribute{r} found: '{f:Yellow}{$0}{r}'", context.Stop.Line, 214, Value);
					}
					break;
				case "CLASS":
					string[] Classes = { "SCOUT", "SOLDIER", "PYRO", "DEMOMAN", "HEAVY", "ENGINEER", "MEDIC", "SNIPER", "SPY" };
					bool IsValidClass = false;
					foreach (string Class in Classes) {
						if (System.Text.RegularExpressions.Regex.IsMatch(ValueParser.String(Value).ToUpper(), "^" + Class, System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
							Node.Class = Class.First() + Class.Substring(1).ToLower();
							ItemTracker.SetupClass(Node.Class);
							Node.Name = Node.Name ?? Node.Class;
							IsValidClass = true;
							break;
						}
					}
					if (!IsValidClass) {
						Warning.Write("Unexpected {f:Yellow}Class{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 304, Value);
					}
					break;
				case "CLASSICON":
					Node.ClassIcon = ValueParser.String(Value);
					break;
				case "HEALTH":
					Node.Health = ValueParser.UnsignedInteger(Value, context);
					int ConfigTFBotHealthMultiple = Program.Config.ReadInt("int_bot_health_multiple");
					if (Node.Health % ConfigTFBotHealthMultiple != 0) { // W0205
						Warning.Write("TFBot {f:Yellow}Health{r} not {f:Yellow}multiple of {$0}{r}: '{f:Yellow}{$1}{r}'", context.Stop.Line, 205, ConfigTFBotHealthMultiple.ToString(), Node.Health.ToString());
					}
					break;
				case "SCALE":
					Node.Scale = ValueParser.Double(Value, context);
					break;
				case "AUTOJUMPMIN":
					Node.AutoJumpMin = ValueParser.Double(Value, context);
					break;
				case "AUTOJUMPMAX":
					Node.AutoJumpMax = ValueParser.Double(Value, context);
					break;
				case "SKILL":
					string[] Skills = { "EASY", "NORMAL", "HARD", "EXPERT" };
					Node.Skill = ValueParser.String(Value);
					if (!Skills.Contains(ValueParser.String(Value).ToUpper())) {
						Warning.Write("Unexpected {f:Yellow}Skill{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 305, Value);
					}
					break;
				case "WEAPONRESTRICTIONS":
					string[] WeaponRestrictions = { "MELEEONLY", "PRIMARYONLY", "SECONDARYONLY" };
					Node.WeaponRestrictions = ValueParser.String(Value);
					if (!WeaponRestrictions.Contains(ValueParser.String(Value).ToUpper())) {
						Warning.Write("Unexpected {f:Yellow}WeaponRestrictions{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 306, Value);
					}
					break;
				case "BEHAVIORMODIFIERS":
					string[] BehaviorModifiers = { "MOBBER", "PUSH" };
					Node.BehaviorModifiers = ValueParser.String(Value);
					if (!BehaviorModifiers.Contains(ValueParser.String(Value).ToUpper())) {
						Warning.Write("Unexpected {f:Yellow}BehaviorModifiers{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 307, Value);
					}
					break;
				case "MAXVISIONRANGE":
					Node.MaxVisionRange = ValueParser.Double(Value, context);
					break;
				case "ITEM":
					string ItemName = ValueParser.String(Value);
					Node.Items.Add(ItemName);
					ItemTracker.Add(ItemName, context.Stop.Line, Node.Class);
					break;
				case "TELEPORTWHERE":
					Node.TeleportWheres.Add(ValueParser.String(Value));
					break;
				case "TAG":
					Node.Tags.Add(ValueParser.String(Value));
					break;
				case "ITEMATTRIBUTES":
					ItemAttributes ItemAttributes = new ItemAttributes();
					ItemAttributesTracker[context.SourceInterval.a] = ItemAttributes;
					Node.ItemAttributes.Add(ItemAttributes);
					break;
				case "CHARACTERATTRIBUTES":
					CharacterAttributes CharacterAttributes = new CharacterAttributes();
					CharacterAttributesTracker[context.SourceInterval.a] = CharacterAttributes;
					Node.CharacterAttributes.Add(CharacterAttributes);
					break;
				case "EVENTCHANGEATTRIBUTES":
					Node.EventChangeAttributes = Node.EventChangeAttributes ?? new Dictionary<string, EventChangeAttributes>();
					break;
			}
			return base.VisitTfbot_body(context);
		}

		public override object VisitTank_body([NotNull] PopulationParser.Tank_bodyContext context) {
			Tank Node = SpawnerTracker[context.Parent.Parent.SourceInterval.a];
			string Key = context.Start.Text;
			string Value = context.Stop.Text;

			switch(Key.ToUpper()) {
				case "HEALTH":
					Node.Health = ValueParser.UnsignedInteger(Value, context);
					int ConfigTankHealthMultiple = Program.Config.ReadInt("int_tank_health_multiple");
					int ConfigTankHealthMaxLimit = Program.Config.ReadInt("int_tank_warn_maximum");
					int ConfigTankHealthMinLimit = Program.Config.ReadInt("int_tank_warn_minimum");
					if (Node.Health % ConfigTankHealthMultiple != 0) { // W0206
						Warning.Write("Tank {f:Yellow}Health{r} not {f:Yellow}multiple of {$0}{r}: '{f:Yellow}{$1}{r}'", context.Stop.Line, 206, ConfigTankHealthMultiple.ToString(), Node.Health.ToString());
					}
					if (Node.Health > ConfigTankHealthMaxLimit) { // W0207
						Warning.Write("Tank {f:Yellow}Health{r} {f:Yellow}exceeds maximum{r} warning [{f:Yellow}{$0}{r}]: '{f:Yellow}{$1}{r}'", context.Stop.Line, 207, ConfigTankHealthMaxLimit.ToString(), Node.Health.ToString());
					}
					if (Node.Health < ConfigTankHealthMinLimit) { // W0208
						Warning.Write("Tank {f:Yellow}Health{r} is {f:Yellow}below minimum{r} warning [{f:Yellow}{$0}{r}]: '{f:Yellow}{$1}{r}'", context.Stop.Line, 208, ConfigTankHealthMinLimit.ToString(), Node.Health.ToString());

					}
					break;
				case "SPEED":
					Node.Speed = ValueParser.Double(Value, context);
					break;
				case "NAME":
					Node.Name = ValueParser.String(Value);
					if (Program.Config.ReadBool("bool_warn_tank_name_tankboss") && ValueParser.String(Value).ToUpper() != "TANKBOSS") {
						Warning.Write("{f:Yellow}Tank{r} not named '{f:Yellow}TankBoss{r}' {f:Yellow}does not explode{r} on deployment: '{f:Yellow}{$0}{r}'", context.Stop.Line, 213, Value);
					}
					break;
				case "SKIN":
					Node.Skin = ValueParser.Integer(Value, context);
					break;
				case "STARTINGPATHTRACKNODE":
					Node.StartingPathTrackNode = ValueParser.String(Value);
					break;
				case "ONKILLEDOUTPUT":
					Node.OnKilledOutput = new LogicOutput("OnKilledOutput");
					OutputTracker[context.SourceInterval.a] = Node.OnKilledOutput;
					break;
				case "ONBOMBDROPPEDOUTPUT":
					Node.OnBombDroppedOutput = new LogicOutput("OnBombDroppedOutput");
					OutputTracker[context.SourceInterval.a] = Node.OnBombDroppedOutput;
					break;
			}

			return base.VisitTank_body(context);
		}

		public override object VisitSentrygun_body([NotNull] PopulationParser.Sentrygun_bodyContext context) {
			SentryGun Node = SpawnerTracker[context.Parent.Parent.SourceInterval.a];
			string Key = context.Start.Text;
			string Value = context.Stop.Text;
			if (Key.ToUpper() == "LEVEL") {
				Node.Level = ValueParser.Integer(Value, context);
			}
			return base.VisitSentrygun_body(context);
		}

		private readonly Dictionary<int, ItemAttributes> ItemAttributesTracker = new Dictionary<int, ItemAttributes>();
		public override object VisitItemattributes_body([NotNull] PopulationParser.Itemattributes_bodyContext context) {
			string Key = ValueParser.String(context.Start.Text);
			string Value = ValueParser.String(context.Stop.Text);
			int ParentTokenIndex = context.Parent.SourceInterval.a;
			ItemAttributes Node = ItemAttributesTracker[ParentTokenIndex];
			if (Key.ToUpper() == "ITEMNAME") {
				string ItemName = Value;
				if (string.IsNullOrEmpty(Node.ItemName)) {
					Node.ItemName = ItemName;
					ItemTracker.AddModifier(ItemName, context.Stop.Line);
				}
				else {
					Warning.Write("{f:Yellow}Multiple ItemName{r} keys found: '{f:Yellow}{$0}{r}'", context.Stop.Line, 217, ItemName);
				}
			}
			else { // Default: KeyValue is an Attribute
				Node.Attributes[Key] = Value;
				AttributeDatabase.Verify(Key, context);
			}
			return base.VisitItemattributes_body(context);
		}

		private readonly Dictionary<int, CharacterAttributes> CharacterAttributesTracker = new Dictionary<int, CharacterAttributes>();
		public override object VisitCharacterattributes_body([NotNull] PopulationParser.Characterattributes_bodyContext context) {
			CharacterAttributes Node = CharacterAttributesTracker[context.Parent.SourceInterval.a];
			string Key = ValueParser.String(context.Start.Text);
			string Value = ValueParser.String(context.Stop.Text);
			Node.Attributes[Key] = Value;
			AttributeDatabase.Verify(Key, context);
			return base.VisitCharacterattributes_body(context);
		}

		private readonly Dictionary<int, LogicOutput> OutputTracker = new Dictionary<int, LogicOutput>();
		public override object VisitOutput_body([NotNull] PopulationParser.Output_bodyContext context) {
			LogicOutput Node = OutputTracker[context.Parent.SourceInterval.a];
			string Key = context.Start.Text;
			string Value = context.Stop.Text;

			switch (Key.ToUpper()) {
				case "TARGET":
					Node.Target = ValueParser.String(Value);
					break;
				case "ACTION":
					Node.Action = ValueParser.String(Value);
					break;
			}
			return base.VisitOutput_body(context);
		}

		public override object VisitEventchangeattributes_body([NotNull] PopulationParser.Eventchangeattributes_bodyContext context) {
			string EventName = context.Start.Text;

			if (context.Parent.Parent.Parent.Parent.Parent.SourceInterval.a == 0) { // This is a template original 4
				T_Generic Template = Pop.Population.Templates[LookBack[context.Parent.Parent.SourceInterval.a].ToUpper()];
				Template.EventChangeAttributes[EventName] = new EventChangeAttributes();
			}
			else {
				TFBot TFBot = SpawnerTracker[context.Parent.Parent.SourceInterval.a];
				TFBot.EventChangeAttributes[EventName] = new EventChangeAttributes();
			}
			return base.VisitEventchangeattributes_body(context);
		}

		public override object VisitEventattributes_body([NotNull] PopulationParser.Eventattributes_bodyContext context) {
			string Key = context.Start.Text;
			string Value = context.Stop.Text;
			string EventName = LookBack[context.Parent.SourceInterval.a];
			EventChangeAttributes Node;
			if (context.Parent.Parent.Parent.Parent.Parent.Parent.SourceInterval.a == 0) { // This is a template original 5
				Node = Pop.Population.Templates[LookBack[context.Parent.Parent.Parent.SourceInterval.a].ToUpper()].EventChangeAttributes[EventName];
			}
			else {
				Node = SpawnerTracker[context.Parent.Parent.Parent.SourceInterval.a].EventChangeAttributes[EventName];
			}

			switch(Key.ToUpper()) {
				case "ATTRIBUTES":
					string[] Attributes = { "REMOVEONDEATH", "AGGRESSIVE", "SUPPRESSFIRE", "DISABLEDODGE", "BECOMESPECTATORONDEATH", "RETAINBUILDINGS", "SPAWNWITHFULLCHARGE", "ALWAYSCRIT", "IGNOREENEMIES", "HOLDFIREUNTILFULLRELOAD", "ALWAYSFIREWEAPON", "TELEPORTTOHINT", "MINIBOSS", "USEBOSSHEALTHBAR", "IGNOREFLAG", "AUTOJUMP", "AIRCHARGEONLY", "VACCINATORBULLETS", "VACCINATORBLAST", "VACCINATORFIRE", "BULLETIMMUNE", "BLASTIMMUNE", "FIREIMMUNE", "PARACHUTE", "PROJECTILESHIELD" };
					Node.Attributes.Add(ValueParser.String(Value));
					if (!Attributes.Contains(ValueParser.String(Value).ToUpper())) {
						Warning.Write("{f:Yellow}Invalid Attribute{r} found: '{f:Yellow}{$0}{r}'", context.Stop.Line, 214, Value);
					}
					break;
				case "SKILL":
					string[] Skills = { "EASY", "NORMAL", "HARD", "EXPERT" };
					Node.Skill = ValueParser.String(Value);
					if (!Skills.Contains(ValueParser.String(Value).ToUpper())) {
						Warning.Write("Unexpected {f:Yellow}Skill{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 305, Value);
					}
					break;
				case "WEAPONRESTRICTIONS":
					string[] WeaponRestrictions = { "MELEEONLY", "PRIMARYONLY", "SECONDARYONLY" };
					Node.WeaponRestrictions = ValueParser.String(Value);
					if (!WeaponRestrictions.Contains(ValueParser.String(Value).ToUpper())) {
						Warning.Write("Unexpected {f:Yellow}WeaponRestrictions{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 306, Value);
					}
					break;
				case "BEHAVIORMODIFIERS":
					string[] BehaviorModifiers = { "MOBBER", "PUSH" };
					Node.BehaviorModifiers = ValueParser.String(Value);
					if (!BehaviorModifiers.Contains(ValueParser.String(Value).ToUpper())) {
						Warning.Write("Unexpected {f:Yellow}BehaviorModifiers{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 307, Value);
					}
					break;
				case "ITEM":
					string ItemName = ValueParser.String(Value);
					Node.Items.Add(ItemName);
					ItemTracker.Add(ItemName, context.Stop.Line);
					break;
				case "TAG":
					Node.Tags.Add(ValueParser.String(Value));
					break;
				case "ITEMATTRIBUTES":
					ItemAttributes ItemAttributes = new ItemAttributes();
					ItemAttributesTracker[context.SourceInterval.a] = ItemAttributes;
					Node.ItemAttributes.Add(ItemAttributes);
					break;
				case "CHARACTERATTRIBUTES":
					CharacterAttributes CharacterAttributes = new CharacterAttributes();
					CharacterAttributesTracker[context.SourceInterval.a] = CharacterAttributes;
					Node.CharacterAttributes.Add(CharacterAttributes);
					break;
			}
			return base.VisitEventattributes_body(context);
		}

		public override object VisitRandomplacement_body([NotNull] PopulationParser.Randomplacement_bodyContext context) {
			RandomPlacement Node = Pop.Population.LastRandomPlacement;
			string Key = ValueParser.String(context.Start.Text);
			string Value = context.Stop.Text;
			switch (Key.ToUpper()) {
				case "COUNT":
					Node.Count = ValueParser.UnsignedInteger(Value, context);
					break;
				case "MINIMUMSEPARATION":
					Node.MinimumSeparation = ValueParser.UnsignedInteger(Value, context);
					break;
				case "NAVAREAFILTER":
					Node.NavFilterArea = ValueParser.String(Value);
					string[] NavFilters = { "SNIPER_SPOT", "SENTRY_SPOT" };
					if (!NavFilters.Contains(ValueParser.String(Value).ToUpper())) {
						Warning.Write("Unexpected {f:Yellow}NavAreaFilter{r} value: '{f:Yellow}{$0}{r}'", context.Stop.Line, 309, Value);
					}
					break;
			}
			return base.VisitRandomplacement_body(context);
		}

		public override object VisitPeriodicspawn_body([NotNull] PopulationParser.Periodicspawn_bodyContext context) {
			PeriodicSpawn Node = Pop.Population.LastPeriodicSpawn;
			string Key = ValueParser.String(context.Start.Text);
			string Value = context.Stop.Text;
			switch (Key.ToUpper()) {
				case "WHERE":
					Node.Where.Add(ValueParser.String(Value));
					break;
				case "WHEN": // TODO Implement
					//Console.WriteLine("No instructions for PeriodicSpawn When!");
					break;
			}
			return base.VisitPeriodicspawn_body(context);
		}

		public override object VisitErrorNode([NotNull] IErrorNode node) {
			return base.VisitErrorNode(node);
		}

	}
}
