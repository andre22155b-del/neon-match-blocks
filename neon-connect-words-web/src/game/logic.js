/*
SCAN REPORT - 2026-05-23
- Word scanner directions present before this pass: right, left, down, up, diagonal down-right, diagonal down-left, diagonal up-right, diagonal up-left.
- Tile color/variant was not used to credit words; scoring was already letter-only. No color scoring condition exists.
- Letters are generated/stored uppercase, seeded puzzle letters are uppercase, and comparisons are now forced through uppercase word lookup.
- Score and Streak initialize as plain numbers set to 0.
- Word scan re-runs after gravity settles post-clear through the clearing-to-runScan path; chain reactions are preserved.
*/

export const ROWS = 7;
export const VS_ROWS = 9;
export const COLS = 7;
export const CLEAR_MS = 280;
export const COMBO_RESET_MS = 5000;
export const TIMED_SECONDS = 90;

const LETTER_BAG = [
  ...'EEEEEEEEEEEE',
  ...'AAAAAAAAA',
  ...'IIIIIIIII',
  ...'OOOOOOOO',
  ...'NNNNNN',
  ...'RRRRRR',
  ...'TTTTTT',
  ...'LLLL',
  ...'SSSS',
  ...'UUUU',
  ...'DDDD',
  ...'GGG',
  ...'BB',
  ...'CC',
  ...'MM',
  ...'PP',
  ...'F',
  ...'H',
  ...'V',
  ...'W',
  ...'Y',
  ...'K',
  ...'J',
  ...'X',
  ...'Q',
  ...'Z',
];

export const LETTER_POINTS = {
  A: 1,
  B: 3,
  C: 3,
  D: 2,
  E: 1,
  F: 4,
  G: 2,
  H: 4,
  I: 1,
  J: 8,
  K: 5,
  L: 1,
  M: 3,
  N: 1,
  O: 1,
  P: 3,
  Q: 10,
  R: 1,
  S: 1,
  T: 1,
  U: 1,
  V: 4,
  W: 4,
  X: 8,
  Y: 4,
  Z: 10,
};

const VS_TWO_LETTER_WORDS = new Set([
  'TO',
  'IS',
  'IT',
  'IN',
  'ON',
  'GO',
  'HE',
  'ME',
  'WE',
  'BE',
  'DO',
  'NO',
  'SO',
  'UP',
  'AN',
  'AT',
  'BY',
  'MY',
  'OR',
  'IF',
  'AS',
  'AM',
  'US',
  'HI',
  'OK',
  'OH',
  'AH',
  'OW',
  'AX',
  'OX',
]);

const WORDS = new Set([
  'AA',
  'AB',
  'AD',
  'AE',
  'AG',
  'AH',
  'AI',
  'AL',
  'AM',
  'AN',
  'AR',
  'AS',
  'AT',
  'AW',
  'AX',
  'AY',
  'BA',
  'BE',
  'BI',
  'BO',
  'BY',
  'DA',
  'DE',
  'DO',
  'ED',
  'EF',
  'EH',
  'EL',
  'EM',
  'EN',
  'ER',
  'ES',
  'ET',
  'EX',
  'FA',
  'FE',
  'GO',
  'HA',
  'HE',
  'HI',
  'HM',
  'HO',
  'ID',
  'IF',
  'IN',
  'IS',
  'IT',
  'JO',
  'KA',
  'LA',
  'LI',
  'LO',
  'MA',
  'ME',
  'MI',
  'MO',
  'MU',
  'MY',
  'NA',
  'NE',
  'NO',
  'NU',
  'OD',
  'OE',
  'OF',
  'OH',
  'OI',
  'OK',
  'OM',
  'ON',
  'OP',
  'OR',
  'OS',
  'OW',
  'OX',
  'OY',
  'PA',
  'PE',
  'PI',
  'QI',
  'RE',
  'SH',
  'SI',
  'SO',
  'TA',
  'TI',
  'TO',
  'UH',
  'UM',
  'UN',
  'UP',
  'US',
  'UT',
  'WE',
  'WO',
  'XI',
  'XU',
  'YA',
  'YE',
  'YO',
  'ZA',
  'ACE',
  'ACT',
  'AGE',
  'AIR',
  'ARC',
  'ARE',
  'ART',
  'ASH',
  'AURA',
  'BOLT',
  'BYTE',
  'CAT',
  'CODE',
  'CORE',
  'DATA',
  'DASH',
  'ECHO',
  'EDGE',
  'FIRE',
  'FLOW',
  'FLUX',
  'GLOW',
  'GRID',
  'ION',
  'LASER',
  'LINE',
  'LINK',
  'LOGIC',
  'LOOP',
  'LUX',
  'MATRIX',
  'MODE',
  'NEON',
  'NODE',
  'NOVA',
  'PIXEL',
  'PLAY',
  'POWER',
  'PULSE',
  'QUEST',
  'RAY',
  'RIFT',
  'RISE',
  'ROAR',
  'SCORE',
  'SHIFT',
  'SPARK',
  'STAR',
  'STREAK',
  'SWAP',
  'TILE',
  'TIME',
  'VIBE',
  'VOLT',
  'WORD',
  'WORDS',
  'ZONE',
]);

[
  'ABLE',
  'ABOUT',
  'ABOVE',
  'ACID',
  'ACORN',
  'ACRE',
  'ADD',
  'AFTER',
  'AGAIN',
  'AGO',
  'AID',
  'AIM',
  'ALARM',
  'ALBUM',
  'ALERT',
  'ALIKE',
  'ALIVE',
  'ALL',
  'ALLEY',
  'ALLOW',
  'ALMOST',
  'ALONE',
  'ALONG',
  'ALSO',
  'ALTER',
  'AMAZE',
  'ANGER',
  'ANGLE',
  'ANGRY',
  'ANT',
  'ANY',
  'APPLE',
  'APPLY',
  'APRON',
  'AREA',
  'ARM',
  'ARMY',
  'ARROW',
  'AUNT',
  'AUTO',
  'AVOID',
  'AWAKE',
  'AWARD',
  'AWAY',
  'BABY',
  'BACK',
  'BAD',
  'BAG',
  'BAKE',
  'BALL',
  'BAND',
  'BANK',
  'BARK',
  'BASE',
  'BASIC',
  'BASKET',
  'BATH',
  'BEACH',
  'BEAM',
  'BEAN',
  'BEAR',
  'BEAT',
  'BEAUTY',
  'BED',
  'BELL',
  'BEND',
  'BEST',
  'BET',
  'BIRD',
  'BIRTH',
  'BIT',
  'BITE',
  'BLACK',
  'BLADE',
  'BLAST',
  'BLEND',
  'BLESS',
  'BLIND',
  'BLOCK',
  'BLOOM',
  'BLUE',
  'BLUSH',
  'BOARD',
  'BOAT',
  'BODY',
  'BONE',
  'BOOK',
  'BOOST',
  'BOOT',
  'BORN',
  'BOSS',
  'BOWL',
  'BOX',
  'BOY',
  'BRAIN',
  'BRAND',
  'BRAVE',
  'BREAD',
  'BREAK',
  'BRICK',
  'BRIDE',
  'BRIEF',
  'BRING',
  'BROAD',
  'BROKE',
  'BROWN',
  'BRUSH',
  'BUILD',
  'BURN',
  'BURST',
  'BUS',
  'BUSY',
  'BUT',
  'BUY',
  'CAKE',
  'CALL',
  'CALM',
  'CAMP',
  'CAN',
  'CANDY',
  'CAP',
  'CAR',
  'CARD',
  'CARE',
  'CARRY',
  'CASE',
  'CAST',
  'CAVE',
  'CELL',
  'CHAIR',
  'CHALK',
  'CHAMP',
  'CHARM',
  'CHASE',
  'CHEAP',
  'CHECK',
  'CHEER',
  'CHEST',
  'CHILD',
  'CHILL',
  'CHIME',
  'CHOIR',
  'CHOOSE',
  'CITY',
  'CLAP',
  'CLASS',
  'CLEAN',
  'CLEAR',
  'CLERK',
  'CLICK',
  'CLIFF',
  'CLIMB',
  'CLOCK',
  'CLOSE',
  'CLOUD',
  'CLUB',
  'COACH',
  'COAST',
  'COIN',
  'COLD',
  'COLOR',
  'COME',
  'COOK',
  'COOL',
  'COPY',
  'CORN',
  'COST',
  'COUNT',
  'COURT',
  'COVER',
  'CRAFT',
  'CRANE',
  'CRASH',
  'CRAWL',
  'CREAM',
  'CREEK',
  'CREST',
  'CROWN',
  'CUP',
  'CURE',
  'DANCE',
  'DARK',
  'DAY',
  'DEAL',
  'DEAR',
  'DEEP',
  'DEER',
  'DESK',
  'DICE',
  'DIG',
  'DINNER',
  'DIRT',
  'DISH',
  'DOG',
  'DOLL',
  'DOOR',
  'DREAM',
  'DRESS',
  'DRIFT',
  'DRINK',
  'DRIVE',
  'DROP',
  'DRUM',
  'DRY',
  'DUCK',
  'DUST',
  'EACH',
  'EAR',
  'EARLY',
  'EARN',
  'EAST',
  'EASY',
  'EAT',
  'EGG',
  'ELBOW',
  'ELSE',
  'EMPTY',
  'END',
  'ENJOY',
  'ENTER',
  'EQUAL',
  'EVER',
  'EVERY',
  'EVIL',
  'EXACT',
  'EXIT',
  'FACE',
  'FACT',
  'FAIR',
  'FALL',
  'FAMILY',
  'FANCY',
  'FARM',
  'FAST',
  'FATHER',
  'FEAR',
  'FEED',
  'FEEL',
  'FENCE',
  'FEST',
  'FIELD',
  'FIGHT',
  'FILE',
  'FILM',
  'FINAL',
  'FIND',
  'FINE',
  'FISH',
  'FIT',
  'FLAG',
  'FLAME',
  'FLASH',
  'FLAT',
  'FLOAT',
  'FLOOR',
  'FLOUR',
  'FOAM',
  'FOCUS',
  'FOG',
  'FOOD',
  'FOOT',
  'FOR',
  'FORCE',
  'FOREST',
  'FORK',
  'FORM',
  'FORT',
  'FOUND',
  'FOX',
  'FRAME',
  'FREE',
  'FRESH',
  'FRIEND',
  'FROG',
  'FRONT',
  'FRUIT',
  'FUN',
  'GAME',
  'GARDEN',
  'GAS',
  'GATE',
  'GHOST',
  'GIANT',
  'GIFT',
  'GIRL',
  'GIVE',
  'GLASS',
  'GLOVE',
  'GOLD',
  'GOOD',
  'GRACE',
  'GRADE',
  'GRAIN',
  'GRAND',
  'GRAPE',
  'GRASS',
  'GREAT',
  'GREEN',
  'GRIN',
  'GROUND',
  'GROUP',
  'GROW',
  'GUARD',
  'GUESS',
  'GUEST',
  'GUIDE',
  'HAIR',
  'HALF',
  'HALL',
  'HAND',
  'HAPPY',
  'HARD',
  'HARM',
  'HAT',
  'HAVE',
  'HAWK',
  'HEAD',
  'HEAL',
  'HEAR',
  'HEART',
  'HEAT',
  'HEAVY',
  'HELP',
  'HERO',
  'HILL',
  'HINT',
  'HOLD',
  'HOME',
  'HONEY',
  'HOPE',
  'HORSE',
  'HOUSE',
  'HUMAN',
  'HUNT',
  'HURRY',
  'ICE',
  'IDEA',
  'IMAGE',
  'IRON',
  'ITEM',
  'JAM',
  'JAR',
  'JAZZ',
  'JOB',
  'JOIN',
  'JOKE',
  'JUMP',
  'JUNE',
  'JUST',
  'KEEP',
  'KEY',
  'KICK',
  'KID',
  'KIND',
  'KING',
  'KISS',
  'KITE',
  'KNEE',
  'KNIFE',
  'KNOW',
  'LADY',
  'LAKE',
  'LAMP',
  'LAND',
  'LARGE',
  'LAST',
  'LATE',
  'LAUGH',
  'LEAF',
  'LEARN',
  'LEFT',
  'LEMON',
  'LESS',
  'LEVEL',
  'LIFE',
  'LIGHT',
  'LIKE',
  'LIMIT',
  'LION',
  'LIST',
  'LIVE',
  'LOAD',
  'LOCK',
  'LONG',
  'LOOK',
  'LOOSE',
  'LOST',
  'LOUD',
  'LOVE',
  'LOW',
  'LUCK',
  'LUNCH',
  'MAGIC',
  'MAIL',
  'MAIN',
  'MAKE',
  'MAP',
  'MARCH',
  'MARK',
  'MATCH',
  'MAY',
  'MEAL',
  'MEAN',
  'MEAT',
  'MELT',
  'MENU',
  'MERRY',
  'MILE',
  'MILK',
  'MIND',
  'MINE',
  'MINT',
  'MISS',
  'MONEY',
  'MONTH',
  'MOON',
  'MORNING',
  'MOUSE',
  'MOVE',
  'MUSIC',
  'NAME',
  'NEAR',
  'NEED',
  'NERVE',
  'NEST',
  'NEW',
  'NEWS',
  'NIGHT',
  'NORTH',
  'NOSE',
  'NOTE',
  'NOW',
  'OCEAN',
  'ODD',
  'OFF',
  'OIL',
  'OLD',
  'ONCE',
  'ONLY',
  'OPEN',
  'ORDER',
  'OTHER',
  'OUT',
  'OVER',
  'OWN',
  'PACK',
  'PAGE',
  'PAINT',
  'PAIR',
  'PAL',
  'PAPER',
  'PARK',
  'PART',
  'PARTY',
  'PASS',
  'PAST',
  'PATH',
  'PEACE',
  'PEARL',
  'PEN',
  'PET',
  'PHONE',
  'PIANO',
  'PICK',
  'PIECE',
  'PINE',
  'PINK',
  'PIPE',
  'PITCH',
  'PLACE',
  'PLAIN',
  'PLANE',
  'PLANT',
  'PLATE',
  'POINT',
  'POOL',
  'PORCH',
  'POST',
  'PRESS',
  'PRICE',
  'PRIDE',
  'PRINT',
  'PRIZE',
  'PROOF',
  'PULL',
  'PUSH',
  'QUEEN',
  'QUICK',
  'QUIET',
  'RACE',
  'RADIO',
  'RAIN',
  'RAISE',
  'RANGE',
  'READ',
  'READY',
  'REAL',
  'RED',
  'REST',
  'RICE',
  'RIDE',
  'RING',
  'RIVER',
  'ROAD',
  'ROCK',
  'ROLL',
  'ROOM',
  'ROOT',
  'ROPE',
  'ROUND',
  'ROUTE',
  'RULE',
  'RUN',
  'SAD',
  'SAFE',
  'SAIL',
  'SALT',
  'SAME',
  'SAND',
  'SAVE',
  'SAY',
  'SCALE',
  'SCENE',
  'SCHOOL',
  'SEA',
  'SEAT',
  'SEED',
  'SEEM',
  'SELL',
  'SEND',
  'SENSE',
  'SEVEN',
  'SHADE',
  'SHAKE',
  'SHAPE',
  'SHARE',
  'SHARP',
  'SHEET',
  'SHELL',
  'SHINE',
  'SHIP',
  'SHIRT',
  'SHOE',
  'SHOP',
  'SHORT',
  'SHOT',
  'SHOW',
  'SIDE',
  'SIGHT',
  'SIGN',
  'SILK',
  'SILLY',
  'SING',
  'SINK',
  'SISTER',
  'SIX',
  'SKILL',
  'SKIN',
  'SKY',
  'SLEEP',
  'SLIDE',
  'SLIM',
  'SLOW',
  'SMALL',
  'SMILE',
  'SMOKE',
  'SNOW',
  'SOAP',
  'SOFT',
  'SON',
  'SONG',
  'SORT',
  'SOUND',
  'SOUTH',
  'SPACE',
  'SPEED',
  'SPELL',
  'SPEND',
  'SPICE',
  'SPORT',
  'SPOT',
  'SPRING',
  'STACK',
  'STAGE',
  'STAIR',
  'STAMP',
  'STAND',
  'START',
  'STATE',
  'STAY',
  'STEAM',
  'STEEL',
  'STEP',
  'STICK',
  'STONE',
  'STOP',
  'STORE',
  'STORM',
  'STORY',
  'STREET',
  'STRONG',
  'STUDY',
  'SUGAR',
  'SUIT',
  'SUM',
  'SUN',
  'SWEET',
  'SWIM',
  'TABLE',
  'TAIL',
  'TAKE',
  'TALK',
  'TALL',
  'TEACH',
  'TEAM',
  'TEAR',
  'TELL',
  'TENT',
  'TERM',
  'TEST',
  'TEXT',
  'THAN',
  'THANK',
  'THAT',
  'THE',
  'THEIR',
  'THEM',
  'THEN',
  'THERE',
  'THESE',
  'THICK',
  'THING',
  'THINK',
  'THIS',
  'THOSE',
  'THREE',
  'THROW',
  'TIDE',
  'TIGER',
  'TINY',
  'TODAY',
  'TOGET',
  'TONE',
  'TOOL',
  'TOOTH',
  'TOP',
  'TOTAL',
  'TOUCH',
  'TOWER',
  'TOWN',
  'TRACK',
  'TRAIN',
  'TRAP',
  'TREE',
  'TRICK',
  'TRIP',
  'TRUCK',
  'TRUE',
  'TRUST',
  'TRY',
  'TURN',
  'UNDER',
  'UNIT',
  'UNTIL',
  'UPPER',
  'USE',
  'USER',
  'VALUE',
  'VOICE',
  'WAIT',
  'WALK',
  'WALL',
  'WANT',
  'WARM',
  'WASH',
  'WATCH',
  'WATER',
  'WAVE',
  'WAY',
  'WEAR',
  'WEEK',
  'WELL',
  'WEST',
  'WHEEL',
  'WHEN',
  'WHERE',
  'WHICH',
  'WHITE',
  'WHO',
  'WHY',
  'WIDE',
  'WIFE',
  'WILD',
  'WIND',
  'WING',
  'WINTER',
  'WIRE',
  'WISE',
  'WISH',
  'WITH',
  'WOMAN',
  'WOOD',
  'WORK',
  'WORLD',
  'WORRY',
  'WRITE',
  'YARD',
  'YEAR',
  'YELLOW',
  'YES',
  'YOUNG',
  'YOUR',
  'ZERO',
].forEach((word) => {
  if (word.length >= 2 && word.length <= 6) WORDS.add(word);
});

export const modes = {
  classic: {
    title: 'Classic',
    label: 'Fill, clear, chain',
  },
  timed: {
    title: 'Timed',
    label: '90-second neon sprint',
  },
  puzzle: {
    title: 'Puzzle',
    label: 'Hit targets in few drops',
  },
  tutorial: {
    title: 'Tutorial',
    label: 'Five guided lessons',
  },
  daily: {
    title: 'Daily Arena',
    label: 'Same board. One shot. Daily.',
  },
};

const tutorialSteps = [
  'Tap any column to drop a letter.',
  'Tiles fall to the lowest open slot.',
  'Words clear in every direction.',
  'Fast clears build combo power.',
  'Special tiles can rescue tight boards.',
];

export function createEmptyBoard(rows = ROWS) {
  return Array.from({ length: rows }, () => Array(COLS).fill(null));
}

export function makeTile(overrides = {}) {
  const { random = Math.random, ...tileOverrides } = overrides;
  const variantRoll = random();
  const variant = variantRoll < 0.72 ? 'purple' : variantRoll < 0.9 ? 'cyan' : 'magenta';
  const letter = LETTER_BAG[Math.floor(random() * LETTER_BAG.length)];

  return {
    id: crypto.randomUUID(),
    letter,
    value: LETTER_POINTS[letter],
    variant,
    owner: 'player',
    special: null,
    status: 'normal',
    justDropped: false,
    ...tileOverrides,
  };
}

function makeSpecialTile(special) {
  const symbol = special === 'wildcard' ? '★' : special === 'bomb' ? '✦' : '⇄';
  const variant = special === 'bomb' ? 'magenta' : special === 'swap' ? 'cyan' : 'purple';
  return makeTile({ letter: symbol, value: special === 'bomb' ? 8 : 5, variant, special });
}

export function initialState(mode = 'classic') {
  const day = todayString();
  const dailySeed = hashSeed(day);
  const board = mode === 'puzzle' ? seedPuzzleBoard() : mode === 'daily' ? seedDailyBoard(dailySeed) : createEmptyBoard();
  const hotZoneRandom = mode === 'daily' ? seededRandom(dailySeed + 700) : Math.random;
  return {
    screen: 'home',
    mode,
    vs: false,
    vsSetup: {
      step: 1,
      playerName: '',
      teamName: '',
      playerColor: '#9d4edd',
      aiColor: '#ff3333',
    },
    aiDifficulty: null,
    currentTurn: 'player',
    aiThinking: false,
    board,
    score: 0,
    aiScore: 0,
    playerRounds: 0,
    aiRounds: 0,
    roundHistory: [],
    wordsFound: [],
    aiWordsFound: [],
    bestCombo: 1,
    combo: 1,
    feverActive: false,
    feverStartedAt: null,
    feverAvailable: mode !== 'daily',
    feverUsedThisCombo: false,
    streak: 0,
    cascadeDepth: 0,
    drops: 0,
    dropsLeft: mode === 'puzzle' ? 18 : Infinity,
    targetWords: mode === 'puzzle' ? ['NEON', 'GLOW', 'WORD'] : [],
    timeLeft: mode === 'timed' ? TIMED_SECONDS : null,
    gameOver: false,
    phase: 'idle',
    message: mode === 'tutorial' ? tutorialSteps[0] : 'Ready',
    tutorialStep: 0,
    pendingSpecial: null,
    nextTiles: createNextTiles(mode, mode === 'daily' ? dailySeed : null, 0),
    swapMode: null,
    feedback: null,
    clearingImpact: null,
    cinematicDrop: null,
    flash: null,
    achievements: [],
    achievementQueue: [],
    lastWordAt: null,
    hotZones: generateHotZones(hotZoneRandom),
    hotZoneUses: 0,
    dailyDate: mode === 'daily' ? day : null,
    randomSeed: mode === 'daily' ? dailySeed : null,
    shareReady: false,
    objective: null,
    objectiveProgress: 0,
    objectiveComplete: false,
    powerDropUsed: { player: false, ai: false },
    powerDropArmed: false,
    taunt: null,
    tauntCooldownUntil: 0,
    lastActionAt: Date.now(),
  };
}

export function reducer(state, action) {
  switch (action.type) {
    case 'SHOW_HOME':
      return { ...state, screen: 'home' };
    case 'START_MODE':
      return { ...initialState(action.mode), screen: 'game' };
    case 'START_VS':
      return { ...initialState(action.mode), screen: 'preMatch', vs: true };
    case 'REMATCH_VS': {
      const next = initialState(state.mode);
      return {
        ...next,
        screen: 'game',
        vs: true,
        vsSetup: state.vsSetup,
        aiDifficulty: state.aiDifficulty,
        board: createEmptyBoard(VS_ROWS),
        objective: pickObjective(),
        message: 'Your move',
      };
    }
    case 'SET_VS_IDENTITY':
      return { ...state, vsSetup: { ...state.vsSetup, ...action.payload, step: 2 } };
    case 'SET_VS_COLOR':
      return { ...state, vsSetup: { ...state.vsSetup, playerColor: action.playerColor, aiColor: action.aiColor, step: 3 } };
    case 'START_VS_MATCH':
      return { ...state, screen: 'difficulty' };
    case 'CHOOSE_DIFFICULTY':
      return { ...state, screen: 'game', board: state.vs ? createEmptyBoard(VS_ROWS) : state.board, aiDifficulty: action.difficulty, objective: pickObjective(), message: 'Your move' };
    case 'COMPLETE_VICTORY':
      return { ...state, screen: 'results' };
    case 'ARM_POWER_DROP':
      if (state.powerDropUsed.player || state.currentTurn !== 'player') return state;
      return { ...state, powerDropArmed: true, flash: makeFlash('power'), achievementQueue: [...state.achievementQueue, { id: 'power-drop', label: 'POWER DROP ⚡' }] };
    case 'SEND_TAUNT':
      if (Date.now() < state.tauntCooldownUntil) return state;
      return { ...state, taunt: action.text, tauntCooldownUntil: Date.now() + 8000 };
    case 'CLEAR_TAUNT':
      return { ...state, taunt: null };
    case 'DROP_TILE':
      return dropTile(state, action.col, action.actor ?? 'player');
    case 'DETONATE_BOMB':
      return detonateBomb(state, action.row, action.col);
    case 'MARK_CLEARING':
      return markClearing(state, action.matches);
    case 'CLEAR_AND_GRAVITY':
      return clearAndGravity(state, action.matches);
    case 'CLEAR_FEEDBACK':
      return { ...state, feedback: null };
    case 'CLEAR_FLASH':
      return state.flash?.id === action.id ? { ...state, flash: null } : state;
    case 'CLEAR_ACHIEVEMENT':
      return { ...state, achievementQueue: state.achievementQueue.slice(1) };
    case 'FEVER_TICK':
      if (!state.feverActive) return state;
      if (Date.now() - state.feverStartedAt < 10000) return state;
      return { ...state, feverActive: false, feverStartedAt: null, flash: makeFlash('white'), message: 'Fever cooled' };
    case 'SETTLED':
      return settleState(state, action.message);
    case 'RESET_COMBO':
      if (Date.now() - state.lastActionAt < COMBO_RESET_MS || state.combo === 1) return state;
      return { ...state, combo: 1, streak: 0, feverUsedThisCombo: false, message: 'Combo cooled' };
    case 'TICK':
      if (state.screen !== 'game' || state.mode !== 'timed' || state.gameOver || state.phase !== 'idle') return state;
      if (state.timeLeft <= 1) return finishGame(state, 'Time up');
      return { ...state, timeLeft: state.timeLeft - 1 };
    case 'USE_SWAP_TILE':
      return { ...state, swapMode: { first: null }, message: 'Choose two tiles' };
    case 'PICK_SWAP':
      return pickSwap(state, action.row, action.col);
    case 'RESULTS':
      return finishGame(state, action.message ?? 'Game over');
    default:
      return state;
  }
}

function seedPuzzleBoard() {
  const board = createEmptyBoard();
  const seeds = [
    [6, 0, 'N'],
    [6, 1, 'E'],
    [6, 2, 'O'],
    [5, 2, 'N'],
    [6, 4, 'G'],
    [6, 5, 'L'],
    [6, 6, 'O'],
    [5, 6, 'W'],
    [4, 3, 'W'],
    [5, 3, 'O'],
    [6, 3, 'R'],
  ];
  seeds.forEach(([row, col, letter]) => {
    board[row][col] = makeTile({ letter, value: LETTER_POINTS[letter], variant: col % 2 ? 'cyan' : 'purple' });
  });
  return board;
}

function dropTile(state, col, actor = 'player') {
  if (state.screen !== 'game' || state.phase !== 'idle' || state.gameOver || state.swapMode) return state;
  if (state.vs && actor !== state.currentTurn) return state;
  if (state.mode === 'puzzle' && state.dropsLeft <= 0) return finishGame(state, 'No drops left');

  const row = findDropRow(state.board, col);
  if (row === -1) return finishGame(state, 'Column filled');

  const board = cloneBoard(state.board);
  const isNearFull = state.board.filter((line) => line[col]).length >= state.board.length - 2;
  const random = state.randomSeed ? seededRandom(state.randomSeed + state.drops * 97 + (actor === 'ai' ? 31 : 0)) : Math.random;
  const special = state.mode === 'daily' || state.vs ? null : state.pendingSpecial || (isNearFull && random() < 0.5 ? 'bomb' : null);
  const tileOverrides = { owner: actor };
  if (actor === 'ai') tileOverrides.variant = 'ai';
  else if (state.vs) tileOverrides.variant = 'purple';
  if (state.vs && actor === 'player') tileOverrides.color = state.vsSetup.playerColor;
  if (state.vs && actor === 'ai') tileOverrides.color = state.vsSetup.aiColor;
  const queuedTile = state.nextTiles?.[0];
  const tile = special ? makeSpecialTile(special) : queuedTile ? { ...queuedTile, id: crypto.randomUUID(), status: 'normal', justDropped: false, ...tileOverrides } : makeTile({ random, ...tileOverrides });
  board[row][col] = { ...tile, justDropped: true };
  const cinematicDrop = actor === 'player' ? getCinematicDropCue({ ...state, board, lastActor: actor, lastDrop: { row, col } }) : null;

  const tutorialStep = state.mode === 'tutorial' ? Math.min(tutorialSteps.length - 1, state.tutorialStep + 1) : state.tutorialStep;

  return {
    ...state,
    board,
    phase: 'dropping',
    lastActor: actor,
    lastDrop: { row, col },
    cinematicDrop,
    cascadeDepth: 0,
    aiThinking: false,
    powerDropUsed: actor === 'player' && state.powerDropArmed ? { ...state.powerDropUsed, player: true } : state.powerDropUsed,
    powerDropArmed: actor === 'player' ? false : state.powerDropArmed,
    drops: state.drops + 1,
    dropsLeft: state.mode === 'puzzle' ? state.dropsLeft - 1 : state.dropsLeft,
    pendingSpecial: null,
    nextTiles: shiftNextTiles(state, actor),
    tutorialStep,
    lastActionAt: Date.now(),
    message: special === 'bomb' ? 'Bomb armed' : state.mode === 'tutorial' ? tutorialSteps[tutorialStep] : actor === 'ai' ? 'AI scanning' : 'Scanning',
  };
}

function getCinematicDropCue(state) {
  const matches = findMatches(state.board, { minLength: state.vs ? 2 : 3, vs: state.vs });
  const scoringMatches = state.vs ? getScoringMatchesForState(state, matches) : matches;
  const dropKey = `${state.lastDrop.row},${state.lastDrop.col}`;
  const dropMatches = scoringMatches.filter((match) => match.cells.some((cell) => `${cell.row},${cell.col}` === dropKey));
  if (!dropMatches.length) return null;
  return {
    length: Math.max(...dropMatches.map((match) => match.word.length)),
    dropKey,
    words: dropMatches.map((match) => match.word),
    cells: dropMatches.flatMap((match) => match.cells),
  };
}

function createNextTiles(mode, randomSeed, startDrops) {
  return Array.from({ length: 3 }, (_, index) => {
    const random = randomSeed ? seededRandom(randomSeed + (startDrops + index) * 97) : Math.random;
    return makeTile({ random });
  });
}

function shiftNextTiles(state, actor) {
  const start = state.drops + 1;
  const random = state.randomSeed ? seededRandom(state.randomSeed + (start + 2) * 97 + (actor === 'ai' ? 31 : 0)) : Math.random;
  return [...(state.nextTiles ?? []).slice(1), makeTile({ random })].slice(0, 3);
}

function detonateBomb(state, row, col) {
  if (state.board[row]?.[col]?.special !== 'bomb') return state;
  return {
    ...state,
    board: clearBombArea(state.board, row, col),
    phase: 'clearing',
    message: 'Bomb burst',
    lastActionAt: Date.now(),
  };
}

function markClearing(state, matches) {
  const ids = new Set(matches.flatMap((match) => match.cells.map((cell) => state.board[cell.row][cell.col]?.id).filter(Boolean)));
  const maxLength = Math.max(0, ...matches.map((match) => match.word.length));
  const clearedCells = matches.flatMap((match) => match.cells);
  const hotZoneSet = new Set(state.vs || state.mode === 'daily' ? [] : state.hotZones.map((zone) => `${zone.row},${zone.col}`));
  return {
    ...state,
    phase: 'clearing',
    clearingImpact: {
      length: maxLength,
      cells: matches.flatMap((match) => match.cells),
      intersection: matches.length > 1 && hasSharedCell(matches),
    },
    feedback: matches.length ? {
      words: [...new Set(matches.map((match) => match.word))],
      points: matches.reduce((total, match) => total + match.word.length, 0),
      anchor: averageCells(clearedCells),
      hotCells: clearedCells.filter((cell) => hotZoneSet.has(`${cell.row},${cell.col}`)),
      intersection: matches.length > 1 && hasSharedCell(matches),
    } : null,
    flash: matches.length ? makeFlash('word') : state.flash,
    board: state.board.map((row) => row.map((tile) => (tile && ids.has(tile.id) ? { ...tile, status: 'clearing' } : tile))),
  };
}

function clearAndGravity(state, matches) {
  const scoringMatches = state.vs ? getScoringMatchesForState(state, matches) : matches;
  const ids = new Set(scoringMatches.flatMap((match) => match.cells.map((cell) => state.board[cell.row][cell.col]?.id).filter(Boolean)));
  const words = [...new Set(scoringMatches.map((match) => match.word))];
  const hotZoneSet = new Set(state.vs || state.mode === 'daily' ? [] : state.hotZones.map((zone) => `${zone.row},${zone.col}`));
  const hotCells = scoringMatches.flatMap((match) => match.cells.filter((cell) => hotZoneSet.has(`${cell.row},${cell.col}`)));
  const awardedPoints = scoringMatches.reduce((total, match) => total + match.word.length, 0);
  const combo = Math.min(9, state.combo + (scoringMatches.length > 0 ? 1 : 0));
  const streak = state.streak + scoringMatches.length;
  const nextSpecial = state.mode === 'daily' || state.vs ? null : streak >= 5 && combo >= 5 ? 'swap' : streak >= 3 ? 'wildcard' : state.pendingSpecial;
  const withoutMatches = state.board.map((row) => row.map((tile) => (tile && ids.has(tile.id) ? null : tile)));
  const board = applyGravity(withoutMatches);
  const targetWords = state.targetWords.filter((word) => !words.includes(word));
  const timeLeft = state.mode === 'timed' ? Math.min(TIMED_SECONDS, state.timeLeft + words.length * 3) : state.timeLeft;
  const clearedCells = scoringMatches.flatMap((match) => match.cells);
  const actor = state.lastActor ?? 'player';
  const scoreKey = actor === 'ai' ? 'aiScore' : 'score';
  const wordKey = actor === 'ai' ? 'aiWordsFound' : 'wordsFound';
  const feverStarts = !state.vs && state.mode !== 'daily' && combo >= 5 && !state.feverActive && !state.feverUsedThisCombo;
  const hotZoneUsed = hotCells.length > 0;
  const nextHotZones = hotZoneUsed ? generateHotZones(state.randomSeed ? seededRandom(state.randomSeed + state.hotZoneUses * 211 + 17) : Math.random) : state.hotZones;
  const now = Date.now();
  const achievements = collectAchievements(state, scoringMatches, hotZoneUsed, feverStarts, now);
  const intersection = scoringMatches.length > 1 && hasSharedCell(scoringMatches);
  const objectiveProgress = state.vs && state.objective ? updateObjectiveProgress(state, scoringMatches, actor) : state.objectiveProgress;
  const objectiveComplete = state.objective ? objectiveProgress >= state.objective.goal : state.objectiveComplete;
  if (intersection && !achievements.seen.includes('crossword')) {
    achievements.seen.push('crossword');
    achievements.newOnes.push({ id: 'crossword', label: 'CROSSWORD 💫' });
  }

  return {
    ...state,
    board,
    cascadeDepth: state.vs ? state.cascadeDepth + 1 : state.cascadeDepth,
    clearingImpact: null,
    cinematicDrop: null,
    [wordKey]: [...state[wordKey], ...scoringMatches.map((match) => ({ word: match.word, points: match.word.length }))],
    combo,
    bestCombo: Math.max(state.bestCombo, combo),
    feverActive: feverStarts ? true : state.feverActive,
    feverStartedAt: feverStarts ? now : state.feverStartedAt,
    feverUsedThisCombo: feverStarts ? true : state.feverUsedThisCombo,
    streak,
    targetWords,
    timeLeft,
    pendingSpecial: nextSpecial,
    hotZones: nextHotZones,
    hotZoneUses: state.hotZoneUses + (hotZoneUsed ? 1 : 0),
    achievements: achievements.seen,
    achievementQueue: [...state.achievementQueue, ...achievements.newOnes],
    objectiveProgress,
    objectiveComplete,
    score: scoreKey === 'score' ? state.score + awardedPoints : state.score,
    aiScore: scoreKey === 'aiScore' ? state.aiScore + awardedPoints : state.aiScore,
    lastActionAt: Date.now(),
    lastWordAt: now,
    message: intersection ? 'CROSSWORD COMBO — bussin fr' : scoringMatches.length ? `${[...new Set(scoringMatches.map((match) => match.word))].join(' + ')} x${combo}` : 'No same-color word',
  };
}

function pickSwap(state, row, col) {
  if (!state.swapMode || !state.board[row][col]) return state;
  if (!state.swapMode.first) {
    return { ...state, swapMode: { first: { row, col } }, message: 'Pick destination' };
  }
  const first = state.swapMode.first;
  const board = cloneBoard(state.board);
  const held = board[first.row][first.col];
  board[first.row][first.col] = board[row][col];
  board[row][col] = held;
  return { ...state, board, swapMode: null, phase: 'dropping', message: 'Swap locked', lastActionAt: Date.now() };
}

function finishGame(state, message) {
  if (state.vs) {
    const playerWon = state.score >= state.aiScore;
    return {
      ...state,
      screen: 'victory',
      gameOver: true,
      phase: 'idle',
      playerRounds: playerWon ? 2 : 0,
      aiRounds: playerWon ? 0 : 2,
      roundHistory: [{ player: state.score, ai: state.aiScore, winner: playerWon ? 'player' : 'ai' }],
      flash: makeFlash('gameOver'),
      message,
    };
  }
  return {
    ...state,
    screen: 'results',
    gameOver: true,
    phase: 'idle',
    flash: makeFlash('gameOver'),
    message,
  };
}

export function findMatches(board, options = {}) {
  const minLength = options.minLength ?? 2;
  const directions = [
    [0, 1],
    [0, -1],
    [1, 0],
    [-1, 0],
    [1, 1],
    [1, -1],
    [-1, 1],
    [-1, -1],
  ];
  const matches = [];
  const seen = new Set();
  const rows = board.length;

  for (let row = 0; row < rows; row += 1) {
    for (let col = 0; col < COLS; col += 1) {
      for (const [dr, dc] of directions) {
        const cells = [];
        for (let len = 1; len <= rows; len += 1) {
          const nr = row + dr * (len - 1);
          const nc = col + dc * (len - 1);
          if (!inBounds(board, nr, nc) || !board[nr][nc]) break;
          cells.push({ row: nr, col: nc });
          if (cells.length >= minLength) {
            const word = resolveWord(cells.map((cell) => board[cell.row][cell.col]), options);
            if (word) {
              const key = `${word}:${cells.map((cell) => `${cell.row},${cell.col}`).join('|')}`;
              if (!seen.has(key)) {
                seen.add(key);
                matches.push({ word, cells: [...cells], direction: { dr, dc } });
              }
            }
          }
        }
      }
    }
  }

  return preferLongMatches(matches);
}

export function getScoringMatchesForState(state, matches) {
  if (!state.vs) return matches;
  const actor = state.lastActor ?? 'player';
  const lastDrop = state.lastDrop;
  return matches.filter((match) => {
    const includesDrop = lastDrop ? match.cells.some((cell) => cell.row === lastDrop.row && cell.col === lastDrop.col) : true;
    const droppedTile = lastDrop ? state.board[lastDrop.row]?.[lastDrop.col] : null;
    return includesDrop && droppedTile?.owner === actor;
  });
}

function preferLongMatches(matches) {
  return matches
    .sort((a, b) => b.cells.length - a.cells.length)
    .filter((match, index, all) => {
      const exactCells = new Set(match.cells.map((cell) => `${cell.row},${cell.col}`));
      return !all.slice(0, index).some((other) => match.cells.every((cell) => other.cells.some((o) => o.row === cell.row && o.col === cell.col)) && other.cells.length > exactCells.size);
    });
}

function resolveWord(tiles, options = {}) {
  const pattern = tiles.map((tile) => (tile.special === 'wildcard' ? '.' : String(tile.letter).toUpperCase())).join('');
  if (pattern.length === 2) {
    if (!options.vs) return null;
    if (!pattern.includes('.')) return VS_TWO_LETTER_WORDS.has(pattern) ? pattern : null;
    const twoLetterRe = new RegExp(`^${pattern}$`);
    return [...VS_TWO_LETTER_WORDS].find((word) => twoLetterRe.test(word)) ?? null;
  }
  if (!pattern.includes('.')) return WORDS.has(pattern) ? pattern : null;
  const re = new RegExp(`^${pattern}$`);
  return [...WORDS].find((word) => word.length === tiles.length && re.test(word.toUpperCase())) ?? null;
}

function averageCells(cells) {
  if (!cells.length) return { row: ROWS / 2, col: COLS / 2 };
  const total = cells.reduce(
    (sum, cell) => ({
      row: sum.row + cell.row,
      col: sum.col + cell.col,
    }),
    { row: 0, col: 0 },
  );
  return {
    row: total.row / cells.length,
    col: total.col / cells.length,
  };
}

function settleState(state, message) {
  const nextTurn = state.vs ? (state.lastActor === 'player' ? 'ai' : 'player') : state.currentTurn;
  return {
    ...state,
    phase: 'idle',
    currentTurn: nextTurn,
    aiThinking: state.vs && nextTurn === 'ai',
    cinematicDrop: null,
    board: clearDropFlags(state.board),
    message: message ?? (state.vs && nextTurn === 'ai' ? 'AI thinking...' : state.message),
  };
}

function makeFlash(type) {
  return { id: crypto.randomUUID(), type };
}

function collectAchievements(state, matches, hotZoneUsed, feverStarts, now) {
  const seen = [...state.achievements];
  const newOnes = [];
  const add = (id, label) => {
    if (seen.includes(id)) return;
    seen.push(id);
    newOnes.push({ id, label });
  };
  if (matches.length) add('first-word', 'FIRST WORD 🔥');
  if (matches.some((match) => Math.abs(match.direction.dr) === 1 && Math.abs(match.direction.dc) === 1)) add('diagonal', 'DIAGONAL HIT ⚡');
  if (feverStarts) add('fever', 'FEVER 💜');
  if (hotZoneUsed) add('hot-zone', 'HOT ZONE 🎯');
  if (state.lastWordAt && now - state.lastWordAt <= 3000) add('back-to-back', 'BACK TO BACK 🔥');
  if (matches.some((match) => match.word.length >= 5)) add('five-letters', '5 LETTERS 💎');
  if (state.combo >= 6) add('beast-mode', 'BEAST MODE 👑');
  return { seen, newOnes };
}

function hasSharedCell(matches) {
  const counts = new Map();
  matches.forEach((match) => {
    match.cells.forEach((cell) => {
      const key = `${cell.row},${cell.col}`;
      counts.set(key, (counts.get(key) ?? 0) + 1);
    });
  });
  return [...counts.values()].some((count) => count > 1);
}

function updateObjectiveProgress(state, matches, actor) {
  if (actor !== 'player' || !matches.length || state.objectiveComplete) return state.objectiveProgress;
  switch (state.objective.id) {
    case 'word-hunter':
    case 'speed-speller':
      return state.objectiveProgress + matches.length;
    case 'diagonal-ace':
      return state.objectiveProgress + matches.filter((match) => Math.abs(match.direction.dr) === 1 && Math.abs(match.direction.dc) === 1).length;
    case 'big-word':
      return state.objectiveProgress + (matches.some((match) => match.word.length >= 5) ? 1 : 0);
    case 'color-lock':
      return state.objectiveProgress + 1;
    case 'shutout':
      return state.aiScore === 0 ? 1 : state.objectiveProgress;
    default:
      return state.objectiveProgress;
  }
}

function findDropRow(board, col) {
  for (let row = board.length - 1; row >= 0; row -= 1) {
    if (!board[row][col]) return row;
  }
  return -1;
}

function cloneBoard(board) {
  return board.map((row) => row.map((tile) => (tile ? { ...tile } : null)));
}

function applyGravity(board) {
  const rows = board.length;
  const next = createEmptyBoard(rows);
  for (let col = 0; col < COLS; col += 1) {
    const tiles = [];
    for (let row = rows - 1; row >= 0; row -= 1) {
      if (board[row][col]) tiles.push({ ...board[row][col], justDropped: true, status: 'normal' });
    }
    tiles.forEach((tile, index) => {
      next[rows - 1 - index][col] = tile;
    });
  }
  return next;
}

function clearBombArea(board, row, col) {
  const next = cloneBoard(board);
  for (let r = row - 1; r <= row + 1; r += 1) {
    for (let c = col - 1; c <= col + 1; c += 1) {
      if (inBounds(board, r, c)) next[r][c] = null;
    }
  }
  return applyGravity(next);
}

function clearDropFlags(board) {
  return board.map((row) => row.map((tile) => (tile ? { ...tile, justDropped: false, status: 'normal' } : null)));
}

function inBounds(board, row, col) {
  return row >= 0 && row < board.length && col >= 0 && col < COLS;
}

function generateHotZones(random = Math.random) {
  const cols = [...Array(COLS).keys()];
  const zones = [];
  for (let i = 0; i < 3; i += 1) {
    const colIndex = Math.floor(random() * cols.length);
    const col = cols.splice(colIndex, 1)[0];
    zones.push({ row: 3 + Math.floor(random() * 4), col });
  }
  return zones;
}

function pickObjective() {
  const objectives = [
    { id: 'word-hunter', name: 'WORD HUNTER', requirement: 'spell 5 words this match', goal: 5, reward: 10, label: 'bussin word collector' },
    { id: 'color-lock', name: 'COLOR LOCK', requirement: 'connect your color 3 times in a row', goal: 3, reward: 8, label: 'lowkey on sight' },
    { id: 'diagonal-ace', name: 'DIAGONAL ACE', requirement: 'score 3 diagonal words', goal: 3, reward: 12, label: 'the crossword era fr' },
    { id: 'shutout', name: 'SHUTOUT ROUND', requirement: 'stop AI from scoring in one round', goal: 1, reward: 15, label: 'main character defense' },
    { id: 'speed-speller', name: 'SPEED SPELLER', requirement: 'find 3 words in under 60 seconds', goal: 3, reward: 10, label: 'no cap moving different' },
    { id: 'big-word', name: 'BIG WORD', requirement: 'spell one word of 5 or more letters', goal: 1, reward: 8, label: 'vocabulary bussin' },
  ];
  return objectives[Math.floor(Math.random() * objectives.length)];
}

function seedDailyBoard(seed) {
  const random = seededRandom(seed);
  const board = createEmptyBoard();
  for (let col = 0; col < COLS; col += 1) {
    const height = col % 2 === 0 ? 1 : 0;
    for (let row = ROWS - 1; row >= ROWS - height; row -= 1) {
      board[row][col] = makeTile({ random, variant: 'purple' });
    }
  }
  return board;
}

function todayString() {
  return new Date().toISOString().slice(0, 10);
}

function hashSeed(text) {
  let hash = 2166136261;
  for (let i = 0; i < text.length; i += 1) {
    hash ^= text.charCodeAt(i);
    hash = Math.imul(hash, 16777619);
  }
  return hash >>> 0;
}

function seededRandom(seed) {
  let value = seed >>> 0;
  return () => {
    value = Math.imul(value + 0x6d2b79f5, 1);
    let t = value;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
