using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PseudoPopParser {

	#region Core Classes
	public class PopFile {

		public List<string> Bases { get; } = new List<string>();
		public List<string> Includes { get; } = new List<string>();
		public Population Population { get; set; }

		public PopFile() { }

	}

	public class Population {

		public uint StartingCurrency;
		public uint AddSentryBusterWhenDamageDealtExceeds = 3000;
		public uint AddsentryBusterWhenKillCountExceeds = 15;
		public uint RespawnWaveTime = 10;
		public bool FixedRespawnWaveTime;
		public bool Advanced;
		public bool IsEndless;
		public bool CanBotsAttackWhileInSpawnRoom;
		public string PopulationName;
		public string EventPopFile;

		// Template
		public Dictionary<string, dynamic> Templates;

		// Mission
		public List<Mission> Missions { get; } = new List<Mission>();
		public Mission LastMission => Missions.Last();
		public void NewMission() => Missions.Add(new Mission());

		// Wave
		public List<Wave> Waves { get; } = new List<Wave>();
		public Wave LastWave => Waves.Last();
		public void NewWave() => Waves.Add(new Wave());

		// RandomPlacement
		public List<RandomPlacement> RandomPlacements { get; } = new List<RandomPlacement>();
		public RandomPlacement LastRandomPlacement => RandomPlacements.Last();
		public void NewRandomPlacement() => RandomPlacements.Add(new RandomPlacement());


		// PeriodicSpawn
		public List<PeriodicSpawn> PeriodicSpawns { get; } = new List<PeriodicSpawn>();
		public PeriodicSpawn LastPeriodicSpawn => PeriodicSpawns.Last();
		public void NewPeriodicSpawn() => PeriodicSpawns.Add(new PeriodicSpawn());


		public Population() {
		}

	}
	#endregion

	#region Generic Classes
	public class Mission {

		public string Objective;
		public double InitialCooldown;
		public double CooldownTime;
		public int RunForThisManyWaves = 9999; // Investigate
		public uint BeginAtWave;
		public uint DesiredCount;
		public List<string> Where { get; set; } = new List<string>();
		public dynamic Spawner;

		public Mission() { }

	}

	public class Wave {

		public string Description;
		public string Sound;
		public int WaitWhenDone; // Does nothing
		public bool Checkpoint; // Does nothing
		
		// Outputs
		public LogicOutput StartWaveOutput;
		public LogicOutput InitWaveOutput;
		public LogicOutput DoneOutput;

		// WaveSpawn
		public List<WaveSpawn> WaveSpawns { get; } = new List<WaveSpawn>();
		public WaveSpawn LastWaveSpawn => WaveSpawns.Last();
		public void NewWaveSpawn() => WaveSpawns.Add(new WaveSpawn());

		public Wave() { }

	}

	public class WaveSpawn {

		public string Name;
		public string Template;
		public string WaitForAllDead; // Conflicts with WaitForAllSpawned
		public string WaitForAllSpawned; // Conflicts with WaitForAllDead
		public uint TotalCurrency;
		public uint TotalCount;
		public uint MaxActive;
		public uint SpawnCount;
		public double WaitBeforeStarting;
		public double WaitBetweenSpawns; // Conflicts with WaitBetweenSpawnsAfterDeath
		public double WaitBetweenSpawnsAfterDeath; // Conflicts with WaitBetweenSpawns
		public bool Support;
		public bool SupportLimited;
		public bool RandomSpawn;
		public List<string> Where = new List<string>();
		public dynamic Spawner;

		// Sounds
		public string StartWaveWarningSound;
		public string FirstSpawnWarningSound;
		public string LastSpawnWarningSound;
		public string DoneWarningSound;

		// Outputs
		public LogicOutput StartWaveOutput;
		public LogicOutput FirstSpawnOutput;
		public LogicOutput LastSpawnOutput;
		public LogicOutput DoneOutput;

		public WaveSpawn() { }
		public void MergeTemplate(T_Generic Template) {
			if (Template.TemplateType == "TFBOT") {
				throw new Exception("IncorrectTemplateTypeException");
			}

			// Template value merges iff:
			// 1. Template value != Node value
			// 2. Template value != default value
			// 3. Node value == default value

			if (this.Name != Template.Name && Template.Name != null && this.Name == null)
				this.Name = Template.Name;

			if (this.TotalCurrency != Template.TotalCurrency && Template.TotalCurrency > 0 && this.TotalCurrency == 0)
				this.TotalCurrency = Template.TotalCurrency;

			if (this.TotalCount != Template.TotalCount && Template.TotalCount > 0 && this.TotalCount == 0)
				this.TotalCount = Template.TotalCount;

			if (this.Where != Template.Where && Template.Where.Count > 0 && this.Where.Count == 0)
				this.Where = Template.Where;

			if (this.MaxActive != Template.MaxActive && Template.MaxActive > 0 && this.MaxActive == 0)
				this.MaxActive = Template.MaxActive;

			if (this.SpawnCount != Template.SpawnCount && Template.SpawnCount > 0 && this.SpawnCount == 0)
				this.SpawnCount = Template.SpawnCount;

			if (this.Support != Template.Support && Template.Support && !this.Support) {
				this.Support = Template.Support;

				if (this.SupportLimited != Template.SupportLimited && Template.SupportLimited && !this.SupportLimited)
					this.SupportLimited = Template.SupportLimited;
			}

			if (this.WaitForAllDead != Template.WaitForAllDead && Template.WaitForAllDead != null && this.WaitForAllDead == null)
				this.WaitForAllDead = Template.WaitForAllDead;

			if (this.WaitForAllSpawned != Template.WaitForAllSpawned && Template.WaitForAllSpawned != null && this.WaitForAllSpawned == null)
				this.WaitForAllSpawned = Template.WaitForAllSpawned;

			if (this.WaitBeforeStarting != Template.WaitBeforeStarting && Template.WaitBeforeStarting > 0 && this.WaitBeforeStarting == 0)
				this.WaitBeforeStarting = Template.WaitBeforeStarting;

			if (this.WaitBetweenSpawns != Template.WaitBetweenSpawns && Template.WaitBetweenSpawns > 0 && this.WaitBetweenSpawns == 0)
				this.WaitBetweenSpawns = Template.WaitBetweenSpawns;

			if (this.WaitBetweenSpawnsAfterDeath != Template.WaitBetweenSpawnsAfterDeath && Template.WaitBetweenSpawnsAfterDeath > 0 && this.WaitBetweenSpawnsAfterDeath == 0)
				this.WaitBetweenSpawnsAfterDeath = Template.WaitBetweenSpawnsAfterDeath;

			if (this.RandomSpawn != Template.RandomSpawn && Template.RandomSpawn && !this.RandomSpawn)
				this.RandomSpawn = Template.RandomSpawn;

			if (this.StartWaveWarningSound != Template.StartWaveWarningSound && Template.StartWaveWarningSound != null && this.StartWaveWarningSound == null)
				this.StartWaveWarningSound = Template.StartWaveWarningSound;

			if (this.StartWaveOutput != Template.StartWaveOutput && Template.StartWaveOutput != null && this.StartWaveOutput == null)
				this.StartWaveOutput = Template.StartWaveOutput;

			if (this.FirstSpawnWarningSound != Template.FirstSpawnWarningSound && Template.FirstSpawnWarningSound != null && this.FirstSpawnWarningSound == null)
				this.FirstSpawnWarningSound = Template.FirstSpawnWarningSound;

			if (this.FirstSpawnOutput != Template.FirstSpawnOutput && Template.FirstSpawnOutput != null && this.FirstSpawnOutput == null)
				this.FirstSpawnOutput = Template.FirstSpawnOutput;

			if (this.LastSpawnWarningSound != Template.LastSpawnWarningSound && Template.LastSpawnWarningSound != null && this.LastSpawnWarningSound == null)
				this.LastSpawnWarningSound = Template.LastSpawnWarningSound;

			if (this.LastSpawnOutput != Template.LastSpawnOutput && Template.LastSpawnOutput != null && this.LastSpawnOutput == null)
				this.LastSpawnOutput = Template.LastSpawnOutput;

			if (this.DoneWarningSound != Template.DoneWarningSound && Template.DoneWarningSound != null && this.DoneWarningSound == null)
				this.DoneWarningSound = Template.DoneWarningSound;

			if (this.DoneOutput != Template.DoneOutput && Template.DoneOutput != null && this.DoneOutput == null)
				this.DoneOutput = Template.DoneOutput;

			if (this.Spawner != Template.Spawner && Template.Spawner != null && this.Spawner == null)
				this.Spawner = Template.Spawner;
		}

	}

	public class ItemAttributes {
		public string ItemName;
		public Dictionary<string, string> Attributes = new Dictionary<string, string>();
		public ItemAttributes() { }
	}

	public class CharacterAttributes {
		public Dictionary<string, string> Attributes = new Dictionary<string, string>();
		public CharacterAttributes() { }
	}

	public class RandomPlacement {
		public uint Count;
		public uint MinimumSeparation;
		public string NavFilterArea; // Investigate
		public dynamic Spawner;

		public RandomPlacement() { }

	}

	public class PeriodicSpawn {
		public List<string> Where = new List<string>();
		public uint When;
		public uint MinInterval;
		public uint MaxInterval;
		public bool UseInterval;
		public dynamic Spawner;

		public PeriodicSpawn() { }

	}

	public class LogicOutput {

		public string OutputName;
		public string Target;
		public string Action;

		public LogicOutput(string Name) {
			this.OutputName = Name;
		}

	}

	public class BotInventory {
		public string[] slot = new string[10];
	}
	#endregion

	#region Templates Classes
	public class T_Generic {

		// Generic
		public string Name;
		public string Template;
		public string TemplateType;

		// TFBot
		public uint Health;
		public double AutoJumpMin;
		public double AutoJumpMax;
		public double Scale = 1.0f;
		public double MaxVisionRange = 6000.0f;
		public string Skill = "Easy";
		public string Class;
		public string ClassIcon;
		public string WeaponRestrictions;
		public string BehaviorModifiers;
		public List<string> Items = new List<string>();
		public List<string> Attributes = new List<string>();
		public List<string> TeleportWheres = new List<string>();
		public List<string> Tags = new List<string>();
		public List<ItemAttributes> ItemAttributes = new List<ItemAttributes>();
		public List<CharacterAttributes> CharacterAttributes = new List<CharacterAttributes>();
		public Dictionary<string, EventChangeAttributes> EventChangeAttributes;

		// WaveSpawn
		public string WaitForAllDead; // Conflicts with WaitForAllSpawned
		public string WaitForAllSpawned; // Conflicts with WaitForAllDead
		public uint TotalCurrency;
		public uint TotalCount;
		public uint MaxActive;
		public uint SpawnCount;
		public double WaitBeforeStarting;
		public double WaitBetweenSpawns; // Conflicts with WaitBetweenSpawnsAfterDeath
		public double WaitBetweenSpawnsAfterDeath; // Conflicts with WaitBetweenSpawns
		public bool Support;
		public bool SupportLimited;
		public bool RandomSpawn;
		public List<string> Where = new List<string>();
		public dynamic Spawner;
		public string StartWaveWarningSound;
		public string FirstSpawnWarningSound;
		public string LastSpawnWarningSound;
		public string DoneWarningSound;
		public LogicOutput StartWaveOutput;
		public LogicOutput FirstSpawnOutput;
		public LogicOutput LastSpawnOutput;
		public LogicOutput DoneOutput;

		public T_Generic() { }
	}

	public class T_TFBot : TFBot {
		public T_TFBot() { }
		public T_TFBot(T_WaveSpawn T_WaveSpawn) {
			this.Name = T_WaveSpawn.Name;
			this.Template = T_WaveSpawn.Template;
		}
	}

	public class T_WaveSpawn : WaveSpawn {
		public T_WaveSpawn() { }
		public T_WaveSpawn(T_TFBot T_TFBot) {
			this.Name = T_TFBot.Name;
			this.Template = T_TFBot.Template;
		}
	}

	public class EventChangeAttributes {
		public double MaxVisionRange;
		public string Skill;
		public string WeaponRestrictions;
		public string BehaviorModifiers;
		public List<string> Items = new List<string>();
		public List<string> Attributes = new List<string>();
		public List<string> Tags = new List<string>();
		public List<ItemAttributes> ItemAttributes = new List<ItemAttributes>();
		public List<CharacterAttributes> CharacterAttributes = new List<CharacterAttributes>();
		public EventChangeAttributes() { }
	}
	#endregion

	#region Spawner Classes
	public class Spawner {
		public Spawner() { }
	}

	public class TFBot : Spawner {
		public uint Health;
		public double AutoJumpMin;
		public double AutoJumpMax;
		public double Scale = 1.0f;
		public double MaxVisionRange = 6000.0f;
		public string Name;
		public string Template;
		public string Skill = "Easy";
		public string Class;
		public string ClassIcon;
		public string WeaponRestrictions;
		public string BehaviorModifiers;
		public List<string> Items = new List<string>();
		public List<string> Attributes = new List<string>();
		public List<string> TeleportWheres = new List<string>();
		public List<string> Tags = new List<string>();
		public List<ItemAttributes> ItemAttributes = new List<ItemAttributes>();
		public List<CharacterAttributes> CharacterAttributes = new List<CharacterAttributes>();
		public Dictionary<string, EventChangeAttributes> EventChangeAttributes;
		public TFBot() { }
		public bool TemplateUsed = false;
		public void MergeTemplate(T_Generic Template) {

			if (Template.TemplateType == "WAVESPAWN") {
				throw new Exception("IncorrectTemplateTypeException");
			}

			// Template value merges iff:
			// 1. Template value != Node value
			// 2. Template value != default value
			// 3. Node value == default value

			if (this.Name != Template.Name && Template.Name != null && this.Name == null)
				this.Name = Template.Name;

			if (this.Attributes != Template.Attributes && Template.Attributes.Count > 0 && this.Attributes.Count == 0)
				this.Attributes = Template.Attributes;

			if (this.Class != Template.Class && Template.Class != null && this.Class == null)
				this.Class = Template.Class;

			if (this.ClassIcon != Template.ClassIcon && Template.ClassIcon != null && this.ClassIcon == null)
				this.ClassIcon = Template.ClassIcon;

			if (this.Health != Template.Health && Template.Health != 0 && this.Health == 0 && this.Health == 0)
				this.Health = Template.Health;

			if (this.Scale != Template.Scale && Template.Scale != 1.0f && this.Scale == 1.0f)
				this.Scale = Template.Scale;

			if (this.AutoJumpMin != Template.AutoJumpMin && Template.AutoJumpMin > 0 && this.AutoJumpMin == 0)
				this.AutoJumpMin = Template.AutoJumpMin;

			if (this.AutoJumpMax != Template.AutoJumpMax && Template.AutoJumpMax > 0 && this.AutoJumpMax == 0)
				this.AutoJumpMax = Template.AutoJumpMax;

			if (this.Skill != Template.Skill && Template.Skill != "Easy" && this.Skill == "Easy")
				this.Skill = Template.Skill;

			if (this.WeaponRestrictions != Template.WeaponRestrictions && Template.WeaponRestrictions != null && this.WeaponRestrictions == null)
				this.WeaponRestrictions = Template.WeaponRestrictions;

			if (this.BehaviorModifiers != Template.BehaviorModifiers && Template.BehaviorModifiers != null && this.BehaviorModifiers == null)
				this.BehaviorModifiers = Template.BehaviorModifiers;

			if (this.MaxVisionRange != Template.MaxVisionRange && Template.MaxVisionRange != 6000.0f && this.MaxVisionRange == 6000.0f)
				this.MaxVisionRange = Template.MaxVisionRange;

			if (this.Items != Template.Items && Template.Items.Count > 0 && this.Items.Count == 0)
				this.Items = Template.Items;

			if (this.TeleportWheres != Template.TeleportWheres && Template.TeleportWheres.Count > 0 && this.TeleportWheres.Count == 0)
				this.TeleportWheres = Template.TeleportWheres;

			if (this.Tags != Template.Tags && Template.Tags.Count > 0 && this.Tags.Count == 0)
				this.Tags = Template.Tags;

			if (this.ItemAttributes != Template.ItemAttributes && Template.ItemAttributes.Count > 0 && this.ItemAttributes.Count == 0)
				this.ItemAttributes = Template.ItemAttributes;

			if (this.CharacterAttributes != Template.CharacterAttributes && Template.CharacterAttributes.Count > 0 && this.CharacterAttributes.Count == 0)
				this.CharacterAttributes = Template.CharacterAttributes;
		}

	}

	public class Tank : Spawner {
		public uint Health = 50000;
		public int Skin;
		public double Speed = 75.0;
		public string Name = "Tank";
		public string StartingPathTrackNode;

		// Outputs
		public LogicOutput OnKilledOutput;
		public LogicOutput OnBombDroppedOutput;
	
		public Tank() { }

	}

	public class SentryGun : Spawner {
		public int Level;
		public SentryGun() { }
	}

	public class Squad : Spawner {
		public bool ShouldPreserveSquad;
		public double FormationSize = -1.0;
		public List<dynamic> Spawners = new List<dynamic>();
		public Squad() { }
		public dynamic LastSpawner() => Spawners.Last();
	}

	public class Mob : Spawner {
		public uint Count;
		public List<dynamic> Spawners = new List<dynamic>();
		public Mob() { }
		public dynamic LastSpawner() => Spawners.Last();
	}

	public class RandomChoice : Spawner {
		public List<dynamic> Spawners = new List<dynamic>();
		public RandomChoice() { }
		public dynamic LastSpawner() => Spawners.Last();
	}
	#endregion

}
