grammar Population;

/*
 * Parser Rules
 */

key
	: MISSION
	// | DIR_BASE
	// | DIR_INCLUDE
	| TEMPLATES
	| WAVE
	| WAVESPAWN
	| RANDOMPLACEMENT
	| PERIODICSPAWN
	| WHEN
	| TFBOT
	| TANK
	| SENTRYGUN
	| SQUAD
	| MOB
	| RANDOMCHOICE
	| ITEMATTRIBUTES
	| CHARACTERATTRIBUTES
	| EVENTCHANGEATTRIBUTES
	| STARTWAVEOUTPUT
	| INITWAVEOUTPUT
	| FIRSTSPAWNOUTPUT
	| LASTSPAWNOUTPUT
	| DONEOUTPUT
	| ONKILLEDOUTPUT
	| ONBOMBDROPPEDOUTPUT
	| WHERE
	| CLOSESTPOINT
	| STARTINGCURRENCY
	| FIXEDRESPAWNWAVETIME
	| RESPAWNWAVETIME
	| CANBOTSATTACKWHILEINSPAWNROOM
	| ISENDLESS
	| ADVANCED
	| ADDSENTRYBUSTERWHENDAMAGEDEALTEXCEEDS
	| ADDSENTRYBUSTERWHENKILLCOUNTEXCEEDS
	| MININTERVAL
	| MAXINTERVAL
	| EVENTPOPFILE
	| TARGET
	| ACTION
	| OBJECTIVE
	| INITIALCOOLDOWN
	| BEGINATWAVE
	| RUNFORTHISMANYWAVES
	| COOLDOWNTIME
	| DESIREDCOUNT
	| DESCRIPTION
	| SOUND
	| WAITWHENDONE
	| CHECKPOINT
	| TOTALCURRENCY
	| TOTALCOUNT
	| MAXACTIVE
	| SPAWNCOUNT
	| SUPPORT
	| RANDOMSPAWN
	| WAITBEFORESTARTING
	| WAITFORALLSPAWNED
	| WAITFORALLDEAD
	| WAITBETWEENSPAWNS
	| WAITBETWEENSPAWNSAFTERDEATH
	| STARTWAVEWARNINGSOUND
	| FIRSTSPAWNWARNINGSOUND
	| LASTSPAWNWARNINGSOUND
	| DONEWARNINGSOUND
	| MINIMUMSEPARATION
	| NAVAREAFILTER
	| WHEN
	| SHOULDPRESERVESQUAD
	| FORMATIONSIZE
	| COUNT
	| TEMPLATE
	| NAME
	| ITEMNAME
	| ATTRIBUTES
	| CLASS
	| CLASSICON
	| HEALTH
	| SCALE
	| AUTOJUMPMIN
	| AUTOJUMPMAX
	| SKILL
	| WEAPONRESTRICTIONS
	| BEHAVIORMODIFIERS
	| MAXVISIONRANGE
	| ITEM
	| TELEPORTWHERE
	| TAG
	| SPEED
	| SKIN
	| STARTINGPATHTRACKNODE
	| LEVEL
	;

value
	: STRING
	| key
	| NUMBER
	;

popfile
	: directive* population EOF
	;

population
	: value '{' population_body* close_curly
	;

directive
	: DIR_BASE value
	| DIR_INCLUDE value
	;

population_body
	: STARTINGCURRENCY value
	| FIXEDRESPAWNWAVETIME value
	| ADVANCED value
	| EVENTPOPFILE value
	| CANBOTSATTACKWHILEINSPAWNROOM value
	| RESPAWNWAVETIME value
	| ISENDLESS value
	| ADDSENTRYBUSTERWHENDAMAGEDEALTEXCEEDS value
	| ADDSENTRYBUSTERWHENKILLCOUNTEXCEEDS value
	| TEMPLATES '{' templates_body* '}' // value is name of the template
	| MISSION '{' mission_body* '}'
	| WAVE '{' wave_body* close_curly
	| RANDOMPLACEMENT '{' randomplacement_body* '}'
	| PERIODICSPAWN '{' periodicspawn_body* '}'
	;

templates_body
	: value '{' template_body* close_curly
	;

template_body
	// Generic
	: TEMPLATE value
	| NAME value
	// TFBot
	| ATTRIBUTES value
	| CLASS value
	| CLASSICON value
	| HEALTH value
	| SCALE value
	| AUTOJUMPMIN value
	| AUTOJUMPMAX value
	| SKILL value
	| WEAPONRESTRICTIONS value
	| BEHAVIORMODIFIERS value
	| MAXVISIONRANGE value
	| ITEM value
	| TELEPORTWHERE value
	| TAG value
	| ITEMATTRIBUTES '{' itemattributes_body* close_curly
	| CHARACTERATTRIBUTES '{' characterattributes_body* '}'
	| EVENTCHANGEATTRIBUTES '{' eventchangeattributes_body*? '}'
	// WaveSpawn
	| TOTALCURRENCY value
	| TOTALCOUNT value
	| WHERE value
	| MAXACTIVE value
	| SPAWNCOUNT value
	| SUPPORT value
	| WAITFORALLDEAD value
	| WAITFORALLSPAWNED value
	| WAITBEFORESTARTING value
	| WAITBETWEENSPAWNS value
	| WAITBETWEENSPAWNSAFTERDEATH value
	| RANDOMSPAWN value
	| STARTWAVEWARNINGSOUND value
	| STARTWAVEOUTPUT '{' output_body* '}'
	| FIRSTSPAWNWARNINGSOUND value
	| FIRSTSPAWNOUTPUT '{' output_body* '}'
	| LASTSPAWNWARNINGSOUND value
	| LASTSPAWNOUTPUT '{' output_body* '}'
	| DONEWARNINGSOUND value
	| DONEOUTPUT '{' output_body* '}'
	| spawners
	;

wave_body
	: DESCRIPTION value
	| SOUND value
	| WAITWHENDONE value
	| CHECKPOINT value
	| STARTWAVEOUTPUT '{' output_body* '}'
	| INITWAVEOUTPUT '{' output_body* '}'
	| DONEOUTPUT '{' output_body* '}'
	| WAVESPAWN '{' wavespawn_body* '}'
	;

wavespawn_body
	: TEMPLATE value
	| NAME value
	| TOTALCURRENCY value
	| TOTALCOUNT value
	| WHERE value
	| MAXACTIVE value
	| SPAWNCOUNT value
	| SUPPORT value
	| WAITFORALLDEAD value
	| WAITFORALLSPAWNED value
	| WAITBEFORESTARTING value
	| WAITBETWEENSPAWNS value
	| WAITBETWEENSPAWNSAFTERDEATH value
	| RANDOMSPAWN value
	| STARTWAVEWARNINGSOUND value
	| STARTWAVEOUTPUT '{' output_body* '}'
	| FIRSTSPAWNWARNINGSOUND value
	| FIRSTSPAWNOUTPUT '{' output_body* '}'
	| LASTSPAWNWARNINGSOUND value
	| LASTSPAWNOUTPUT '{' output_body* '}'
	| DONEWARNINGSOUND value
	| DONEOUTPUT '{' output_body* '}'
	| spawners
	;

randomplacement_body
	: COUNT value
	| MINIMUMSEPARATION value
	| NAVAREAFILTER value
	| spawners
	;

periodicspawn_body
	: WHERE value
	| WHEN ( value | '{' when_body* '}' )
	| spawners
	;

when_body
	: MININTERVAL value
	| MAXINTERVAL value
	;

mission_body
	: OBJECTIVE value
	| INITIALCOOLDOWN value
	| WHERE value
	| BEGINATWAVE value
	| RUNFORTHISMANYWAVES value
	| COOLDOWNTIME value
	| DESIREDCOUNT value
	| spawners
	;

spawners
	: TFBOT '{' tfbot_body*? close_curly
	| TANK '{' tank_body*? '}'
	| SENTRYGUN '{' sentrygun_body*? '}'
	| SQUAD '{' squad_body*? '}'
	| MOB '{' mob_body*? '}'
	| RANDOMCHOICE '{' randomchoice_body*? '}'
	;

squad_body
	: SHOULDPRESERVESQUAD value
	| FORMATIONSIZE value
	| spawners
	;

mob_body
	: COUNT value
	| spawners
	;

randomchoice_body
	: spawners
	;

tfbot_body
	: TEMPLATE value
	| NAME value
	| ATTRIBUTES value
	| CLASS value
	| CLASSICON value
	| HEALTH value
	| SCALE value
	| AUTOJUMPMIN value
	| AUTOJUMPMAX value
	| SKILL value
	| WEAPONRESTRICTIONS value
	| BEHAVIORMODIFIERS value
	| MAXVISIONRANGE value
	| ITEM value
	| TELEPORTWHERE value
	| TAG value
	| ITEMATTRIBUTES '{' itemattributes_body* close_curly
	| CHARACTERATTRIBUTES '{' characterattributes_body* '}'
	| EVENTCHANGEATTRIBUTES '{' eventchangeattributes_body*? '}'
	;

tank_body
	: HEALTH value
	| SPEED value
	| NAME value
	| SKIN value
	| STARTINGPATHTRACKNODE value
	| ONKILLEDOUTPUT '{' output_body* '}'
	| ONBOMBDROPPEDOUTPUT '{' output_body* '}'
	;

sentrygun_body
	: LEVEL value
	;

itemattributes_body
	: ITEMNAME value
	| value value
	;

characterattributes_body
	: value value
	;
	
eventchangeattributes_body
	: value '{' eventattributes_body* '}'
	;

eventattributes_body
	: ATTRIBUTES value
	| SKILL value
	| WEAPONRESTRICTIONS value
	| BEHAVIORMODIFIERS value
	| MAXVISIONRANGE value
	| ITEM value
	| TAG value
	| ITEMATTRIBUTES '{' itemattributes_body* '}'
	| CHARACTERATTRIBUTES '{' characterattributes_body* '}'
	;

output_body
	: TARGET value
	| ACTION value
	;

close_curly // Parser anchor method
	: '}'
	;

/*
 * Lexer Rules
 */

COMMENT
	: '//' ~[\r\n]* -> skip
	;
WS
	: [ \r\t\n]+ -> skip
	;

fragment A	: [Aa] ;
fragment B	: [Bb] ;
fragment C	: [Cc] ;
fragment D	: [Dd] ;
fragment E	: [Ee] ;
fragment F	: [Ff] ;
fragment G	: [Gg] ;
fragment H	: [Hh] ;
fragment I	: [Ii] ;
fragment J	: [Jj] ;
fragment K	: [Kk] ;
fragment L	: [Ll] ;
fragment M	: [Mm] ;
fragment N	: [Nn] ;
fragment O	: [Oo] ;
fragment P	: [Pp] ;
fragment Q	: [Qq] ;
fragment R	: [Rr] ;
fragment S	: [Ss] ;
fragment T	: [Tt] ;
fragment U	: [Uu] ;
fragment V	: [Vv] ;
fragment W	: [Ww] ;
fragment X	: [Xx] ;
fragment Y	: [Yy] ;
fragment Z	: [Zz] ;

// Collections - General
MISSION					: M I S S I O N | '"' MISSION '"' ;
TEMPLATES				: T E M P L A T E S | '"' TEMPLATES '"' ;
WAVE					: W A V E | '"' WAVE '"' ;
WAVESPAWN				: W A V E S P A W N | '"' WAVESPAWN '"' ;
RANDOMPLACEMENT			: R A N D O M P L A C E M E N T | '"' RANDOMPLACEMENT '"' ;
PERIODICSPAWN 			: P E R I O D I C S P A W N | '"' PERIODICSPAWN '"' ;
WHEN					: W H E N | '"' WHEN '"' ;

// Collections - Spawners
TFBOT					: T F B O T | '"' TFBOT '"' ;
TANK					: T A N K | '"' TANK '"' ;
SENTRYGUN				: S E N T R Y G U N | '"' SENTRYGUN '"' ;
SQUAD					: S Q U A D | '"' SQUAD '"' ;
MOB						: M O B | '"' MOB '"' ;
RANDOMCHOICE			: R A N D O M C H O I C E | '"' RANDOMCHOICE '"' ;

// Collections - Attributes
ITEMATTRIBUTES			: I T E M A T T R I B U T E S | '"' ITEMATTRIBUTES '"' ;
CHARACTERATTRIBUTES		: C H A R A C T E R A T T R I B U T E S | '"' CHARACTERATTRIBUTES '"' ;
EVENTCHANGEATTRIBUTES	: E V E N T C H A N G E A T T R I B U T E S | '"' EVENTCHANGEATTRIBUTES '"' ;
// REVERTGATEBOTSBEHAVIOR	: R E V E R T G A T E B O T S B E H A V I O R | '"' REVERTGATEBOTSBEHAVIOR '"' ;
// DEFAULT					: D E F A U L T | '"' DEFAULT '"' ;

// Collections - Outputs
STARTWAVEOUTPUT			: S T A R T W A V E O U T P U T | '"' STARTWAVEOUTPUT '"' ;
INITWAVEOUTPUT			: I N I T W A V E O U T P U T | '"' INITWAVEOUTPUT '"' ;
FIRSTSPAWNOUTPUT		: F I R S T S P A W N O U T P U T | '"' FIRSTSPAWNOUTPUT '"' ;
LASTSPAWNOUTPUT			: L A S T S P A W N O U T P U T | '"' LASTSPAWNOUTPUT '"' ;
DONEOUTPUT				: D O N E O U T P U T | '"' DONEOUTPUT '"' ;
ONKILLEDOUTPUT			: O N K I L L E D O U T P U T | '"' ONKILLEDOUTPUT '"' ;
ONBOMBDROPPEDOUTPUT		: O N B O M B D R O P P E D O U T P U T | '"' ONBOMBDROPPEDOUTPUT '"' ;

// Keys - General
DIR_BASE				: '#' B A S E | '"' DIR_BASE '"' ;
DIR_INCLUDE				: '#' I N C L U D E | '"' DIR_INCLUDE '"' ;
WHERE					: W H E R E | '"' WHERE '"' ;
CLOSESTPOINT			: C L O S E S T P O I N T | '"' CLOSESTPOINT '"' ;
STARTINGCURRENCY		: S T A R T I N G C U R R E N C Y | '"' STARTINGCURRENCY '"' ;
FIXEDRESPAWNWAVETIME	: F I X E D R E S P A W N W A V E T I M E | '"' STARTINGCURRENCY '"' ;
RESPAWNWAVETIME			: R E S P A W N W A V E T I M E | '"' RESPAWNWAVETIME '"' ;
CANBOTSATTACKWHILEINSPAWNROOM
						: C A N B O T S A T T A C K W H I L E I N S P A W N R O O M | '"' CANBOTSATTACKWHILEINSPAWNROOM '"' ;
ISENDLESS				: I S E N D L E S S | '"' ISENDLESS '"' ;
ADVANCED				: A D V A N C E D | '"' ADVANCED '"' ;
ADDSENTRYBUSTERWHENDAMAGEDEALTEXCEEDS
						: A D D S E N T R Y B U S T E R W H E N D A M A G E D E A L T E X C E E D S | '"' ADDSENTRYBUSTERWHENDAMAGEDEALTEXCEEDS '"' ;
ADDSENTRYBUSTERWHENKILLCOUNTEXCEEDS
						: A D D S E N T R Y B U S T E R W H E N K I L L C O U N T E X C E E D S | '"' ADDSENTRYBUSTERWHENKILLCOUNTEXCEEDS '"' ;
MININTERVAL				: M I N I N T E R V A L | '"' MININTERVAL '"' ;
MAXINTERVAL				: M A X I N T E R V A L | '"' MAXINTERVAL '"' ;
EVENTPOPFILE			: E V E N T P O P F I L E | '"' EVENTPOPFILE '"' ;
TARGET					: T A R G E T | '"' TARGET '"' ;
ACTION					: A C T I O N | '"' ACTION '"' ;

// Keys - Mission
OBJECTIVE				: O B J E C T I V E | '"' OBJECTIVE '"' ;
INITIALCOOLDOWN			: I N I T I A L C O O L D O W N | '"' INITIALCOOLDOWN '"' ;
BEGINATWAVE				: B E G I N A T W A V E | '"' BEGINATWAVE '"' ;
RUNFORTHISMANYWAVES		: R U N F O R T H I S M A N Y W A V E S  | '"' RUNFORTHISMANYWAVES '"' ;
COOLDOWNTIME			: C O O L D O W N T I M E | '"' COOLDOWNTIME '"' ;
DESIREDCOUNT			: D E S I R E D C O U N T | '"' DESIREDCOUNT '"' ;

// Keys - Wave
DESCRIPTION				: D E S C R I P T I O N | '"' DESCRIPTION '"' ;
SOUND					: S O U N D | '"' SOUND '"' ;
WAITWHENDONE			: W A I T W H E N D O N E | '"' WAITWHENDONE '"' ;
CHECKPOINT				: C H E C K P O I N T | '"' CHECKPOINT '"' ;

// Keys - WaveSpawn
TOTALCURRENCY			: T O T A L C U R R E N C Y | '"' TOTALCURRENCY '"' ;
TOTALCOUNT				: T O T A L C O U N T | '"' TOTALCOUNT '"' ;
MAXACTIVE				: M A X A C T I V E | '"' MAXACTIVE '"' ;
SPAWNCOUNT				: S P A W N C O U N T | '"' SPAWNCOUNT '"' ;
SUPPORT					: S U P P O R T | '"' SUPPORT '"' ;
RANDOMSPAWN				: R A N D O M S P A W N | '"' RANDOMSPAWN '"' ;
WAITBEFORESTARTING		: W A I T B E F O R E S T A R T I N G | '"' WAITBEFORESTARTING '"' ;
WAITFORALLSPAWNED		: W A I T F O R A L L S P A W N E D | '"' WAITFORALLSPAWNED '"' ;
WAITFORALLDEAD			: W A I T F O R A L L D E A D | '"' WAITFORALLDEAD '"' ;
WAITBETWEENSPAWNS		: W A I T B E T W E E N S P A W N S | '"' WAITBETWEENSPAWNS '"' ;
WAITBETWEENSPAWNSAFTERDEATH
						: W A I T B E T W E E N S P A W N S A F T E R D E A T H | '"' WAITBETWEENSPAWNSAFTERDEATH '"' ;
STARTWAVEWARNINGSOUND	: S T A R T W A V E W A R N I N G S O U N D | '"' STARTWAVEWARNINGSOUND '"' ;
FIRSTSPAWNWARNINGSOUND	: F I R S T S P A W N W A R N I N G S O U N D | '"' FIRSTSPAWNWARNINGSOUND '"' ;
LASTSPAWNWARNINGSOUND	: L A S T S P A W N W A R N I N G S O U N D | '"' LASTSPAWNWARNINGSOUND '"' ;
DONEWARNINGSOUND		: D O N E W A R N I N G S O U N D | '"' DONEWARNINGSOUND '"' ;

// Keys - RandomPlacement
MINIMUMSEPARATION		: M I N I M U M S E P A R A T I O N | '"' MINIMUMSEPARATION '"' ;
NAVAREAFILTER			: N A V A R E A F I L T E R | '"' NAVAREAFILTER '"' ;

// Keys - PeriodicSpawn
// WHEN					: W H E N | '"' WHEN '"' ;

// Keys - Spawners
SHOULDPRESERVESQUAD		: S H O U L D P R E S E R V E S Q U A D | '"' SHOULDPRESERVESQUAD '"' ;
FORMATIONSIZE			: F O R M A T I O N S I Z E | '"' FORMATIONSIZE '"' ;
COUNT					: C O U N T | '"' COUNT '"' ;

// Keys - TFBots
TEMPLATE				: T E M P L A T E | '"' TEMPLATE '"' ;
NAME					: N A M E | '"' NAME '"' ;
ITEMNAME				: I T E M N A M E | '"' ITEMNAME '"' ;
ATTRIBUTES				: A T T R I B U T E S | '"' ATTRIBUTES '"' ;
CLASS					: C L A S S | '"' CLASS '"' ;
CLASSICON				: C L A S S I C O N | '"' CLASSICON '"' ;
HEALTH					: H E A L T H | '"' HEALTH '"' ;
SCALE					: S C A L E | '"' SCALE '"' ;
AUTOJUMPMIN				: A U T O J U M P M I N | '"' AUTOJUMPMIN '"' ;
AUTOJUMPMAX				: A U T O J U M P M A X | '"' AUTOJUMPMAX '"' ;
SKILL					: S K I L L | '"' SKILL '"' ;
WEAPONRESTRICTIONS		: W E A P O N R E S T R I C T I O N S | '"' WEAPONRESTRICTIONS '"' ;
BEHAVIORMODIFIERS		: B E H A V I O R M O D I F I E R S | '"' BEHAVIORMODIFIERS '"' ;
MAXVISIONRANGE			: M A X V I S I O N R A N G E | '"' MAXVISIONRANGE '"' ;
ITEM					: I T E M | '"' ITEM '"' ;
TELEPORTWHERE			: T E L E P O R T W H E R E | '"' TELEPORTWHERE '"' ;
TAG						: T A G | '"' TAG '"' ;

// Keys - Tank
SPEED					: S P E E D | '"' SPEED '"' ;
SKIN					: S K I N | '"' SKIN '"' ;
STARTINGPATHTRACKNODE	: S T A R T I N G P A T H T R A C K N O D E | '"' STARTINGPATHTRACKNODE '"' ;

// Keys - Sentry Gun
LEVEL					: L E V E L | '"' LEVEL '"' ;

// Miscellaneous
NUMBER
	: DIGIT+
	| DIGIT+ '.' DIGIT+
	| '.' DIGIT+
	| '-' NUMBER
	| '"' NUMBER '"'
	;

fragment DIGIT
	: [0-9]
	;

STRING
	: ~[" \r\n\t{}]+
	| '"' (ESC | ~'"')* '"'
	;

fragment ESC
	: '\\' (["\\/nrt])
	;