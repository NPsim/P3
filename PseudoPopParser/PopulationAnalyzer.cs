using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {
	class PopulationAnalyzer {
		public PopFile Pop;

		public PopulationAnalyzer (PopFile PopFile) {
			this.Pop = PopFile;
		}


		// Wave credits not a multiple of X	
		// Total Possible Credits exceeds 30000
		private void W0101_W0102() {
			int ConfigMultiple = Program.Config.ReadInt("int_warn_credits_multiple");
			uint PopulationTotalCurrency = Pop.Population.StartingCurrency;

			List<Wave> Waves = Pop.Population.Waves;
			for(int i = 0; i < Waves.Count; i++) { 
				uint WaveTotalCurrency = 0;
				foreach (WaveSpawn WaveSpawn in	Waves[i].WaveSpawns) {
					WaveTotalCurrency += WaveSpawn.TotalCurrency;
				}

				// W0101
				if (WaveTotalCurrency % ConfigMultiple != 0) {
					Warning.Write("{f:Yellow}Wave " + (i+1) + "{r}'s credits is {f:Yellow}not multiple{r} of {f:Yellow}" + ConfigMultiple + "{r}: '{f:Yellow}" + WaveTotalCurrency + "{r}'", -1, 101);
				}
				PopulationTotalCurrency += WaveTotalCurrency;
			}

			// W0102
			if (PopulationTotalCurrency > 30000) {
				Warning.Write("{f:Yellow}Total Possible Credits{r} exceeds maximum possible reading of {f:Yellow}30000{r}: '{f:Yellow}" + PopulationTotalCurrency + "{r}'", -1, 102);
			}
		}

		private bool IsSimpleSpawner(dynamic Spawner) {
			string[] SimpleSpawnerTypes = { "PseudoPopParser.TFBot", "PseudoPopParser.Tank", "PseudoPopParser.SentryGun" };
			string Type = Spawner.GetType().ToString();
			return SimpleSpawnerTypes.Contains(Type);
		}

		private bool IsComplexSpawner(dynamic Spawner) {
			string[] ComplexSpawnerTypes = { "PseudoPopParser.Squad", "PseudoPopParser.Mob", "PseudoPopParser.RandomChoice" };
			string Type = Spawner.GetType().ToString();
			return ComplexSpawnerTypes.Contains(Type);
		}

		// Iteratively put all Spawners in a WaveSpawn or Mission in an easy to analyze List
		private List<dynamic> ListSpawners(dynamic SpawnerContainer) {
			dynamic Spawner = SpawnerContainer.Spawner;
			string[] SimpleSpawnerTypes = { "PseudoPopParser.TFBot", "PseudoPopParser.Tank", "PseudoPopParser.SentryGun" };
			string[] ComplexSpawnerTypes = { "PseudoPopParser.Squad", "PseudoPopParser.Mob", "PseudoPopParser.RandomChoice" };
			List<dynamic> SimpleSpawners = new List<dynamic>();
			List<dynamic> ComplexSpawners = new List<dynamic>();
			string type = Spawner.GetType().ToString();

			// Categorize initial spawner
			if (SimpleSpawnerTypes.Contains(type)) {
				SimpleSpawners.Add(Spawner);
			}
			else if (ComplexSpawnerTypes.Contains(type)) {
				ComplexSpawners.Add(Spawner);
			}

			// Comb through spawners
			while (ComplexSpawners.Count >= 1) {
				dynamic Dissect = ComplexSpawners[0];
				foreach (dynamic InternalSpawner in Dissect.Spawners) {
					type = InternalSpawner.GetType().ToString();
					if (SimpleSpawnerTypes.Contains(type)) {
						SimpleSpawners.Add(InternalSpawner);
					}
					else if (ComplexSpawnerTypes.Contains(type)) {
						ComplexSpawners.Add(InternalSpawner);
					}
				}
				ComplexSpawners.Remove(Dissect);
			}
			return SimpleSpawners;
		}

		// TFBot Health Multiple
		// Tank Health Multiple
		/*private void W0205_W0206_W0207_W0208() { // Deprecated due to lack of line number
			int ConfigTFBotHealthMultiple = Program.Config.ReadInt("int_bot_health_multiple");
			int ConfigTankHealthMultiple = Program.Config.ReadInt("int_tank_health_multiple");
			int ConfigTankHealthMaxLimit = Program.Config.ReadInt("int_tank_warn_maximum");
			int ConfigTankHealthMinLimit = Program.Config.ReadInt("int_tank_warn_minimum");


			WaveSpawn WaveSpawn = PopFile.Population.LastWave.LastWaveSpawn;
			List<dynamic> Spawners = ListSpawners(WaveSpawn);
			foreach (dynamic Spawner in Spawners) {
				if (Spawner.GetType().Name == "TFBot") {

					// W0205
					if (Spawner.Health % ConfigTFBotHealthMultiple != 0) {
						Warning.Write("TFBot not multiple of X");
					}
				}
				else if (Spawner.GetType().Name == "Tank") {

					// W0206
					if (Spawner.Health % ConfigTankHealthMultiple != 0) {
						Warning.Write("Tank not multiple of X");
					}

					// W0207
					if (Spawner.Health > ConfigTankHealthMaxLimit) {
						Warning.Write("Tank health too high");
					}

					// W0208
					if (Spawner.Health < ConfigTankHealthMinLimit) {
						Warning.Write("Tank health too low");
					}
				}
			}
		}*/

		public List<List<uint>> CreditStatistics() {
			var Stats = new List<List<uint>>();
			foreach(Wave w in Pop.Population.Waves) {
				List<uint> StatWave = new List<uint>();
				foreach (WaveSpawn ws in w.WaveSpawns) {
					uint TotalCurrency = ws.TotalCurrency;
					if (TotalCurrency > 0)
						StatWave.Add(ws.TotalCurrency);
				}
				Stats.Add(StatWave);
			}
			return Stats;
		}

		public List<List<string>> WaveSpawnNames() {
			var WaveRoster = new List<List<string>>();
			foreach (Wave w in Pop.Population.Waves) {
				List<string> Names = new List<string>();
				foreach (WaveSpawn ws in w.WaveSpawns) {
					if (ws.Name == null) continue;
					if (ws.Name.Length > 0) {
						Names.Add(ws.Name);
					}
				}
				WaveRoster.Add(Names);
			}
			return WaveRoster;
		}

		// Generates a list of all custom icons in the pop file
		// Default icons come from default classes and default base files
		// See this project's default icons database
		public List<string> CustomIcons() {
			var IconDict = new Dictionary<string, string>(); // Key: Normalized comparable string; Value: Original file value
			
			// Get icons from WaveSpawns
			foreach (Wave w in Pop.Population.Waves) {
				foreach (WaveSpawn ws in w.WaveSpawns) {

					// We don't care about dummy wavespawns
					if (ws.Spawner == null) continue;

					// Get all simple spawners
					List<dynamic> Spawners = ListSpawners(ws);

					// Add icons from each simple spawner to the dict
					foreach (dynamic s in Spawners) {
						if (s is TFBot && ((TFBot)s).ClassIcon != null) {
							string key = s.ClassIcon.ToLower();
							if (!IconDict.ContainsKey(key)) {
								IconDict.Add(key, s.ClassIcon);
							}
						}
					}
				}
			}

			// Get icons from Missions
			// Fixed Issue #1: "Show Custom Icons" list does not display custom icons used in mission support
			foreach (Mission m in Pop.Population.Missions) {

				// We don't care about empty missions
				if (m.Spawner == null) continue;

				// Get all simple spawners
				List<dynamic> Spawners = ListSpawners(m);

				// Add icons from each simple spawner to the dict
				foreach (dynamic s in Spawners) {
					if (s is TFBot && ((TFBot)s).ClassIcon != null) {
						string key = s.ClassIcon.ToLower();
						if (!IconDict.ContainsKey(key)) {
							IconDict.Add(key, s.ClassIcon);
						}
					}
				}
			}

			// Remove default icons
			string DefaultIconsPack = AppDomain.CurrentDomain.BaseDirectory + @"Database\DefaultIcons.txt";
			List<string> DefaultIcons = new List<string>(System.IO.File.ReadAllLines(DefaultIconsPack));
			foreach (string DIcon in DefaultIcons) {
				string key = DIcon.ToLower();
				if (IconDict.ContainsKey(key)) {
					IconDict.Remove(key);
				}
			}

			return IconDict.Values.ToList();
		}

		public Tuple<List<string>, List<string>, List<string>> TemplateTypeNames() {
			if (Pop.Population.Templates == null) return new Tuple<List<string>, List<string>, List<string>>(new List<string>(), new List<string>(), new List<string>());
			List<string> TFBotTemplates = new List<string>();
			List<string> WaveSpawnTemplates = new List<string>();
			List<string> GenericTemplates = new List<string>();
			List<string> AllTemplateNames = Pop.Population.Templates.Keys.ToList();
			foreach(string TemplateName in AllTemplateNames) {
				switch(Pop.Population.Templates[TemplateName.ToUpper()].TemplateType) {
					case "TFBOT":
						TFBotTemplates.Add(TemplateName);
						break;
					case "WAVESPAWN":
						WaveSpawnTemplates.Add(TemplateName);
						break;
					default:
						GenericTemplates.Add(TemplateName);
						break;
				}
			}
			return new Tuple<List<string>, List<string>, List<string>>(TFBotTemplates, WaveSpawnTemplates, GenericTemplates);
		}
	}
}
