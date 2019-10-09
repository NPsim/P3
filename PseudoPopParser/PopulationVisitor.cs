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

		public override object VisitPopfile(PopulationParser.PopfileContext context) {
			this.Pop = new PopFile();
			return base.VisitPopfile(context);
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
						BasePopFileFullPath = AppDomain.CurrentDomain.BaseDirectory + "base_templates\\" + BasePopFile;
					}

					try {
						AntlrInputStream inputstream = new AntlrInputStream(System.IO.File.ReadAllText(BasePopFileFullPath));
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
							Pop.Population.Templates[item.Key] = item.Value;
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
						// TODO Warn Halloween
						Console.WriteLine("WARN | Key 'Event' only supports value 'Halloween'");
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
			Pop.Population.Templates[TemplateName] = new T_Generic();
			return base.VisitTemplates_body(context);
		}

		private readonly string[] T_GenericKeys = { "NAME", "TEMPLATE" };
		private readonly string[] T_TFBotKeys = { "ATTRIBUTES", "CLASS", "CLASSICON", "HEALTH", "SCALE", "AUTOJUMPMIN", "AUTOJUMPMAX", "SKILL", "WEAPONRESTRICTIONS", "BEHAVIORMODIFIERS", "MAXVISIONRANGE", "ITEM", "TELEPORTWHERE", "TAG", "ITEMATTRIBUTES", "CHARACTERATTRIBUTES" };
		private readonly string[] T_WaveSpawnKeys = { "TOTALCURRENCY", "TOTALCOUNT", "WHERE", "MAXACTIVE", "SPAWNCOUNT", "SUPPORT", "WAITFORALLDEAD", "WAITFORALLSPAWNED", "WAITBEFORESTARTING", "WAITBETWEENSPAWNS", "WAITBETWEENSPAWNSAFTERDEATH", "RANDOMSPAWN", "STARTWAVEWARNINGSOUND", "STARTWAVEOUTPUT", "FIRSTSPAWNWARNINGSOUND", "FIRSTSPAWNOUTPUT", "LASTSPAWNWARNINGSOUND", "LASTSPAWNOUTPUT", "DONEWARNINGSOUND", "DONEOUTPUT" };
		public override object VisitTemplate_body([NotNull] PopulationParser.Template_bodyContext context) {
			string TemplateName = LookBack[context.Parent.SourceInterval.a];
			T_Generic Template = Pop.Population.Templates[TemplateName];
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
								IsValidClass = true;
								break;
							}
						}
						if (!IsValidClass) {
							// TODO Warn Bad Class Value
							Console.WriteLine("WARN | Invalid Class");
						}
						break;
					case "CLASSICON":
						Template.ClassIcon = ValueParser.String(Value);
						break;
					case "HEALTH":
						Template.Health = ValueParser.UnsignedInteger(Value, context);
						int ConfigTFBotHealthMultiple = Program.Config.ReadInt("int_bot_health_multiple");
						if (Template.Health % ConfigTFBotHealthMultiple != 0) { // W0205
							Warning.Write("TFBot Template {f:Cyan}Health{r} not {f:Yellow}multiple of {$0}{r}: '{f:Yellow}{$1}{r}'", context.Stop.Line, 205, ConfigTFBotHealthMultiple.ToString(), Template.Health.ToString());
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
							// TODO Warn Invalid Skill
							Console.WriteLine("WARN | Invalid Skill");
						}
						break;
					case "WEAPONRESTRICTIONS":
						string[] WeaponRestrictions = { "MELEEONLY", "PRIMARYONLY", "SECONDARYONLY" };
						Template.WeaponRestrictions = ValueParser.String(Value);
						if (!WeaponRestrictions.Contains(ValueParser.String(Value).ToUpper())) {
							// TODO Warn Invalid Restriction Value
							Console.WriteLine("WARN | Invalid Skill");
						}
						break;
					case "BEHAVIORMODIFIERS":
						string[] BehaviorModifiers = { "MOBBER", "PUSH" };
						Template.BehaviorModifiers = ValueParser.String(Value);
						if (!BehaviorModifiers.Contains(ValueParser.String(Value).ToUpper())) {
							// TODO Warn Invalid Modifer
							Console.WriteLine("WARN | Invalid Skill");
						}
						break;
					case "MAXVISIONRANGE":
						Template.MaxVisionRange = ValueParser.Double(Value, context);
						break;
					case "ITEM":
						Template.Items.Add(ValueParser.String(Value));
						break;
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
						// TODO Check item existence after template importing
						break;
					case "CHARACTERATTRIBUTES": // TODO Investigate multiple char attributes
						CharacterAttributes CharacterAttributes = new CharacterAttributes();
						CharacterAttributesTracker[context.SourceInterval.a] = CharacterAttributes;
						Template.CharacterAttributes.Add(CharacterAttributes);
						break;
					case "EVENTCHANGEATTRIBUTES":
						Template.EventChangeAttributes = Template.EventChangeAttributes ?? new Dictionary<string, EventChangeAttributes>();
						break;
					default:
						// TODO Mixed template (expected TFBot)
						Console.WriteLine("ERROR | Attempted to add WaveSpawn key to TFBot template: " + Key);
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
						// TODO Mixed template (expected WaveSpawn)
						Console.WriteLine("ERROR | Attempted to add TFBot key to WaveSpawn template: " + Key);
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
						// TODO Warn Invalid Objective
						Console.WriteLine("WARN | Invalid Objective");
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
						T_Generic Template = Pop.Population.Templates[Node.Template];
						Node.MergeTemplate(Template);
					}
					catch (Exception ex) {
						if (ex.Message == "IncorrectTemplateTypeException") {
							Warning.Write("Invalid {f:Yellow}template type{r} given: '{f:Yellow}{$0}{r}'", context.Stop.Line, 214, Value);
						}
						else {
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
					break;
				case "WAITFORALLSPAWNED":
					Node.WaitForAllSpawned = ValueParser.String(Value);
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
				default: // Spawner is inside T_WaveSpawn
					string TemplateName = LookBack[context.Parent.Parent.SourceInterval.a];
					Pop.Population.Templates[TemplateName].Spawner = NewSpawner;
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
					break;
				case "TEMPLATE": {
					if (Node.Template != null) {
						Warning.Write("Cannot use {f:Yellow}multiple Templates{r}.", context.Stop.Line, 215);
						break;
					}

					Node.Template = ValueParser.String(Value);
					try {
						T_Generic Template = Pop.Population.Templates[Node.Template];
						Node.MergeTemplate(Template);
					}
					catch (Exception ex) {
						if (ex.Message == "IncorrectTemplateTypeException") {
							Warning.Write("Invalid {f:Yellow}template type{r} given: '{f:Yellow}{$0}{r}'", context.Stop.Line, 214, Value);
						}
						else {
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
					bool ClassChanged = false;
					foreach (string Class in Classes) {
						if (System.Text.RegularExpressions.Regex.IsMatch(ValueParser.String(Value).ToUpper(), "^" + Class, System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
							Node.Class = Class.First() + Class.Substring(1).ToLower();
							ClassChanged = true;
							break;
						}
					}
					if (!ClassChanged) {
						// TODO Warn Bad Class Value
						Console.WriteLine("WARN | Invalid Class");
					}
					break;
				case "CLASSICON":
					Node.ClassIcon = ValueParser.String(Value);
					break;
				case "HEALTH":
					Node.Health = ValueParser.UnsignedInteger(Value, context);
					int ConfigTFBotHealthMultiple = Program.Config.ReadInt("int_bot_health_multiple");
					if (Node.Health % ConfigTFBotHealthMultiple != 0) { // W0205
						Warning.Write("TFBot {f:Cyan}Health{r} not {f:Yellow}multiple of {$0}{r}: '{f:Yellow}{$1}{r}'", context.Stop.Line, 205, ConfigTFBotHealthMultiple.ToString(), Node.Health.ToString());
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
						// TODO Warn Invalid Skill
						Console.WriteLine("WARN | Invalid Skill");
					}
					break;
				case "WEAPONRESTRICTIONS":
					string[] WeaponRestrictions = { "MELEEONLY", "PRIMARYONLY", "SECONDARYONLY" };
					Node.WeaponRestrictions = ValueParser.String(Value);
					if (!WeaponRestrictions.Contains(ValueParser.String(Value).ToUpper())) {
						// TODO Warn Invalid Restriction Value
						Console.WriteLine("WARN | Invalid Skill");
					}
					break;
				case "BEHAVIORMODIFIERS":
					string[] BehaviorModifiers = { "MOBBER", "PUSH" };
					Node.BehaviorModifiers = ValueParser.String(Value);
					if (!BehaviorModifiers.Contains(ValueParser.String(Value).ToUpper())) {
						// TODO Warn Invalid Modifier
						Console.WriteLine("WARN | Invalid Skill");
					}
					break;
				case "MAXVISIONRANGE":
					Node.MaxVisionRange = ValueParser.Double(Value, context);
					break;
				case "ITEM":
					Node.Items.Add(ValueParser.String(Value));
					// TODO Check item existence
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
					// TODO Check item existence after template import
					break;
				case "CHARACTERATTRIBUTES": // TODO Investigate multiple char attributes
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
						Warning.Write("Tank {f:Cyan}Health{r} not {f:Yellow}multiple of {$0}{r}: '{f:Yellow}{$1}{r}'", context.Stop.Line, 206, ConfigTankHealthMultiple.ToString(), Node.Health.ToString());
					}
					if (Node.Health > ConfigTankHealthMaxLimit) { // W0207
						Warning.Write("Tank {f:Cyan}Health{r} {f:Yellow}exceeds maximum{r} warning [{f:Yellow}>{$0}{r}]: '{f:Yellow}{$1}{r}'", context.Stop.Line, 207, ConfigTankHealthMaxLimit.ToString(), Node.Health.ToString());
					}
					if (Node.Health < ConfigTankHealthMinLimit) { // W0208
						Warning.Write("Tank {f:Cyan}Health{r} is {f:Yellow}below minimum{r} warning [{f:Yellow}<{$0}{r}]: '{f:Yellow}{$1}{r}'", context.Stop.Line, 208, ConfigTankHealthMinLimit.ToString(), Node.Health.ToString());

					}
					break;
				case "SPEED":
					Node.Speed = ValueParser.Double(Value, context);
					break;
				case "NAME":
					Node.Name = ValueParser.String(Value);
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
				Node.Level = ValueParser.UnsignedInteger(Value, context);
			}
			return base.VisitSentrygun_body(context);
		}

		private readonly Dictionary<int, ItemAttributes> ItemAttributesTracker = new Dictionary<int, ItemAttributes>();
		public override object VisitItemattributes_body([NotNull] PopulationParser.Itemattributes_bodyContext context) {
			string Key = context.Start.Text;
			string Value = context.Stop.Text;
			int ParentTokenIndex = context.Parent.SourceInterval.a;
			ItemAttributes Node = ItemAttributesTracker[ParentTokenIndex];
			if (Key.ToUpper() == "ITEMNAME") Node.ItemName = Node.ItemName ?? ValueParser.String(Value); // TODO Test ItemName only accepts first value
			else Node.Attributes[ValueParser.String(Key)] = ValueParser.String(Value);
			return base.VisitItemattributes_body(context);
		}

		private readonly Dictionary<int, CharacterAttributes> CharacterAttributesTracker = new Dictionary<int, CharacterAttributes>();
		public override object VisitCharacterattributes_body([NotNull] PopulationParser.Characterattributes_bodyContext context) {
			CharacterAttributes Node = CharacterAttributesTracker[context.Parent.SourceInterval.a];
			string Key = context.Start.Text;
			string Value = context.Stop.Text;
			Node.Attributes[ValueParser.String(Key)] = ValueParser.String(Value);
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
				T_Generic Template = Pop.Population.Templates[LookBack[context.Parent.Parent.SourceInterval.a]];
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
				Node = Pop.Population.Templates[LookBack[context.Parent.Parent.Parent.SourceInterval.a]].EventChangeAttributes[EventName];
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
						// TODO Warn Invalid Skill
						Console.WriteLine("WARN | Invalid Skill");
					}
					break;
				case "WEAPONRESTRICTIONS":
					string[] WeaponRestrictions = { "MELEEONLY", "PRIMARYONLY", "SECONDARYONLY" };
					Node.WeaponRestrictions = ValueParser.String(Value);
					if (!WeaponRestrictions.Contains(ValueParser.String(Value).ToUpper())) {
						// TODO Warn Invalid Restriction Value
						Console.WriteLine("WARN | Invalid Skill");
					}
					break;
				case "BEHAVIORMODIFIERS":
					string[] BehaviorModifiers = { "MOBBER", "PUSH" };
					Node.BehaviorModifiers = ValueParser.String(Value);
					if (!BehaviorModifiers.Contains(ValueParser.String(Value).ToUpper())) {
						// TODO Warn Invalid Modifier
						Console.WriteLine("WARN | Invalid BehaviorModifiers");
					}
					break;
				case "ITEM":
					Node.Items.Add(ValueParser.String(Value));
					// TODO Check item existence
					break;
				case "TAG":
					Node.Tags.Add(ValueParser.String(Value));
					break;
				case "ITEMATTRIBUTES":
					ItemAttributes ItemAttributes = new ItemAttributes();
					ItemAttributesTracker[context.SourceInterval.a] = ItemAttributes;
					Node.ItemAttributes.Add(ItemAttributes);
					// TODO Check item existence after template import
					break;
				case "CHARACTERATTRIBUTES": // TODO Investigate multiple char attributes
					CharacterAttributes CharacterAttributes = new CharacterAttributes();
					CharacterAttributesTracker[context.SourceInterval.a] = CharacterAttributes;
					Node.CharacterAttributes.Add(CharacterAttributes);
					break;
			}
			return base.VisitEventattributes_body(context);
		}

		public override object VisitErrorNode([NotNull] IErrorNode node) {
			return base.VisitErrorNode(node);
		}

	}
}
