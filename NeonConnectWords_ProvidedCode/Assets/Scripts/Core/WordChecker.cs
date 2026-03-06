using System.Collections.Generic;
using UnityEngine;

// ============================================================
// Data Structures
// ============================================================

/// <summary>Result of a word found on the board.</summary>
public class WordResult
{
    public string word;
    public List<Vector2Int> positions;
    public int startCol;
    public int startRow;
    public WordDirection direction;
    public int points;

    public WordResult(string w, List<Vector2Int> pos, WordDirection dir)
    {
        word = w;
        positions = pos;
        startCol = pos[0].x;
        startRow = pos[0].y;
        direction = dir;
        points = w.Length;  // base points
    }
}

public enum WordDirection { Horizontal, Vertical, DiagonalUp, DiagonalDown }

/// <summary>Trie node for O(n) word validation.</summary>
public class TrieNode
{
    public Dictionary<char, TrieNode> children = new Dictionary<char, TrieNode>();
    public bool isEndOfWord;
    public bool isPrefix;  // pre-computed during insert
}

// ============================================================
// WordChecker
// ============================================================

/// <summary>
/// WordChecker: Builds a trie from a word list, validates words,
/// and scans the board in 4 directions for valid words ≥ minWordLength.
/// </summary>
public class WordChecker : MonoBehaviour
{
    [Header("Settings")]
    public int minWordLength = 3;
    public TextAsset wordListAsset;   // assign a .txt word list in Inspector

    private TrieNode root = new TrieNode();
    private HashSet<string> wordSet = new HashSet<string>();

    // -----------------------------------------------------------------------
    // Initialisation
    // -----------------------------------------------------------------------
    private void Awake()
    {
        LoadDictionary();
    }

    public void LoadDictionary()
    {
        root = new TrieNode();
        wordSet.Clear();

        string[] words;
        if (wordListAsset != null)
        {
            words = wordListAsset.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            // Built-in minimal word set for prototype/testing
            words = GetBuiltInWordList();
        }

        foreach (string raw in words)
        {
            string w = raw.Trim().ToUpperInvariant();
            if (w.Length >= minWordLength)
            {
                InsertWord(w);
                wordSet.Add(w);
            }
        }

        Debug.Log($"[WordChecker] Loaded {wordSet.Count} words into trie.");
    }

    private void InsertWord(string word)
    {
        TrieNode node = root;
        foreach (char c in word)
        {
            if (!node.children.ContainsKey(c))
                node.children[c] = new TrieNode();
            node = node.children[c];
        }
        node.isEndOfWord = true;
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    public bool IsValidWord(string word)
    {
        return wordSet.Contains(word.ToUpperInvariant());
    }

    public bool IsPrefix(string prefix)
    {
        TrieNode node = root;
        foreach (char c in prefix.ToUpperInvariant())
        {
            if (!node.children.ContainsKey(c)) return false;
            node = node.children[c];
        }
        return true;
    }

    /// <summary>
    /// Scan entire board and return all valid words found in any direction.
    /// Uses trie prefix pruning for efficiency.
    /// </summary>
    public List<WordResult> FindAllWords(LetterTile[,] board, int cols, int rows)
    {
        List<WordResult> results = new List<WordResult>();
        HashSet<string> foundWords = new HashSet<string>();   // dedup

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // Horizontal →
            new Vector2Int(0, 1),   // Vertical ↑
            new Vector2Int(1, 1),   // Diagonal ↗
            new Vector2Int(1, -1),  // Diagonal ↘
        };

        WordDirection[] dirEnum = new WordDirection[]
        {
            WordDirection.Horizontal, WordDirection.Vertical,
            WordDirection.DiagonalUp, WordDirection.DiagonalDown
        };

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                if (board[c, r] == null) continue;

                for (int d = 0; d < directions.Length; d++)
                {
                    ScanDirection(board, cols, rows, c, r, directions[d], dirEnum[d], results, foundWords);
                }
            }
        }

        return results;
    }

    private void ScanDirection(LetterTile[,] board, int cols, int rows,
        int startC, int startR, Vector2Int dir, WordDirection dirEnum,
        List<WordResult> results, HashSet<string> foundWords)
    {
        TrieNode node = root;
        string accumulated = "";
        List<Vector2Int> positions = new List<Vector2Int>();

        int c = startC;
        int r = startR;

        while (c >= 0 && c < cols && r >= 0 && r < rows)
        {
            LetterTile tile = board[c, r];
            if (tile == null) break;

            char letter = tile.Letter;

            // Wildcard support
            if (tile.IsWildcard)
            {
                // Wildcard: try all children
                foreach (char wc in node.children.Keys)
                {
                    string tryWord = accumulated + wc;
                    if (IsValidWord(tryWord) && tryWord.Length >= minWordLength)
                    {
                        string key = $"{startC},{startR},{dirEnum},{tryWord}";
                        if (!foundWords.Contains(key))
                        {
                            foundWords.Add(key);
                            var pos2 = new List<Vector2Int>(positions) { new Vector2Int(c, r) };
                            results.Add(new WordResult(tryWord, pos2, dirEnum));
                        }
                    }
                }
                break;  // simplification: stop scanning after wildcard
            }

            if (!node.children.ContainsKey(letter)) break;
            node = node.children[letter];
            accumulated += letter;
            positions.Add(new Vector2Int(c, r));

            if (node.isEndOfWord && accumulated.Length >= minWordLength)
            {
                string key = $"{startC},{startR},{dirEnum},{accumulated}";
                if (!foundWords.Contains(key))
                {
                    foundWords.Add(key);
                    results.Add(new WordResult(accumulated, new List<Vector2Int>(positions), dirEnum));
                }
            }

            c += dir.x;
            r += dir.y;
        }
    }

    // -----------------------------------------------------------------------
    // Built-in Word List (prototype fallback)
    // -----------------------------------------------------------------------
    private string[] GetBuiltInWordList()
    {
        return new string[]
        {
            "ACE","ACT","ADD","AGE","AID","AIM","AIR","ALE","AND","ANT",
            "APE","APP","APT","ARC","ARE","ARM","ART","ASH","ASK","ATE",
            "BAD","BAG","BAN","BAR","BAT","BAY","BED","BIG","BIT","BOW",
            "BOX","BOY","BUD","BUG","BUN","BUS","BUT","BUY","CAB","CAN",
            "CAP","CAR","CAT","COB","COD","COG","COP","COT","COW","CRY",
            "CUB","CUP","CUT","DAB","DAD","DAM","DIG","DIM","DIP","DOC",
            "DOG","DOT","DRY","DUB","DUG","DUO","DYE","EAR","EAT","EEL",
            "EGG","ELM","EMU","END","ERA","EWE","FAD","FAN","FAR","FAT",
            "FAX","FIG","FIN","FIT","FLY","FOB","FOG","FOP","FOR","FOX",
            "FRY","FUN","FUR","GAB","GAP","GAS","GAY","GEL","GEM","GET",
            "GIG","GIN","GNU","GOB","GOD","GOT","GUM","GUN","GUT","GUY",
            "GYM","HAD","HAM","HAS","HAT","HAY","HEM","HEN","HER","HID",
            "HIM","HIP","HIS","HIT","HOB","HOG","HOP","HOT","HOW","HUB",
            "HUG","HUM","HUT","ICE","ICY","ILL","IMP","INK","INN","ION",
            "IRE","IRK","JAB","JAG","JAM","JAR","JAW","JAY","JET","JIG",
            "JOB","JOG","JOT","JOY","JUG","JUT","KEG","KID","KIN","KIT",
            "LAB","LAD","LAP","LAW","LAX","LAY","LEA","LED","LEG","LET",
            "LID","LIP","LIT","LOG","LOT","LOW","MAP","MAR","MAT","MAW",
            "NAB","NAG","NAP","NAY","NET","NEW","NIL","NIP","NOB","NOD",
            "NOR","NOT","NOW","NUB","NUN","NUT","OAK","OAR","OAT","ODD",
            "ODE","OFF","OFT","OIL","OLD","OPT","ORB","ORE","OUR","OWE",
            "OWL","OWN","PAD","PAL","PAN","PAP","PAR","PAT","PAW","PAY",
            "PEA","PEG","PEN","PEP","PET","PIE","PIG","PIN","PIP","PIT",
            "PLY","POD","POP","POT","POW","PRY","PUB","PUG","PUN","PUP",
            "PUS","PUT","RAG","RAM","RAN","RAP","RAT","RAW","RAY","RED",
            "REF","RIB","RID","RIG","RIM","RIP","ROB","ROD","ROT","ROW",
            "RUB","RUG","RUM","RUN","RUT","SAC","SAD","SAG","SAP","SAT",
            "SAW","SAY","SEA","SET","SEW","SHY","SIP","SIR","SIT","SIX",
            "SKY","SLY","SOB","SOD","SON","SOP","SOT","SOW","SOY","SPA",
            "SPY","STY","SUB","SUE","SUM","SUN","SUP","TAB","TAN","TAP",
            "TAR","TAT","TAX","TEA","TEN","THE","TIE","TIN","TIP","TOE",
            "TON","TOO","TOP","TOT","TOW","TOY","TUB","TUG","TUN","TWO",
            "URN","USE","VAT","VIA","VIE","VOW","WAD","WAR","WAS","WAX",
            "WEB","WED","WET","WHO","WHY","WIG","WIN","WIT","WOE","WOK",
            "WON","WOO","WOW","YAK","YAM","YAP","YAW","YEA","YEW","YOU",
            // Common 4-letter words
            "ABLE","ACID","AGED","ALSO","AREA","ARMY","AWAY","BABY","BACK",
            "BALL","BAND","BANK","BASE","BATH","BEAR","BEAT","BEEN","BELL",
            "BEST","BIRD","BLOW","BLUE","BOAT","BODY","BOND","BONE","BOOK",
            "BOOM","BORN","BOSS","BOTH","BULK","BURN","CAGE","CAKE","CALL",
            "CALM","CAME","CARD","CARE","CASE","CASH","CAST","CAVE","CELL",
            "CHAT","CHIP","CHOP","CITY","CLAP","CLAY","CLIP","CLUB","CLUE",
            "COAL","COAT","CODE","COLD","COME","COOK","COOL","COPE","COPY",
            "CORD","CORE","CORN","COST","COZY","CREW","CROP","CURE","CUTE",
            "DARK","DASH","DATA","DATE","DAWN","DAYS","DEAD","DEAL","DEAR",
            "DEBT","DEEP","DENY","DESK","DIAL","DIET","DIRT","DISK","DIVE",
            "DOCK","DOES","DOME","DONE","DOOR","DOSE","DOVE","DOWN","DRAW",
            "DREW","DRIP","DROP","DRUM","DUAL","DULL","DUMB","DUMP","DUSK",
            "DUST","DUTY","EACH","EARL","EARN","EASE","EAST","EASY","EDGE",
            "ELSE","EMIT","EPIC","EVEN","EVER","EVIL","EXAM","FACE","FACT",
            "FADE","FAIL","FAIR","FAKE","FALL","FAME","FAST","FATE","FAWN",
            "FEEL","FEET","FELL","FELT","FILE","FILL","FILM","FIND","FINE",
            "FIRE","FIRM","FISH","FIST","FLAG","FLAT","FLEW","FLIP","FLOW",
            "FOAM","FOLD","FOLK","FOND","FONT","FOOD","FOOL","FOOT","FORD",
            "FORE","FORK","FORM","FORT","FOUL","FREE","FROM","FUEL","FULL",
            "FUND","FUSE","GAIN","GAME","GAVE","GEAR","GENE","GIFT","GIVE",
            "GLAD","GLOW","GLUE","GOAL","GOES","GOLD","GOLF","GONE","GOOD",
            "GOWN","GRAB","GRAY","GREW","GRID","GRIM","GRIP","GROW","GULF",
            "GUST","HACK","HAIL","HALF","HALL","HAND","HANG","HARD","HARM",
            "HASH","HATE","HAVE","HAWK","HEAD","HEAL","HEAP","HEAR","HEAT",
            "HEEL","HELD","HELM","HELP","HERE","HIGH","HILL","HINT","HIRE",
            "HOLD","HOLE","HOLY","HOME","HOOD","HOOK","HOPE","HORN","HOST",
            "HOUR","HUGE","HULL","HUNT","HURT","HYMN","ICED","IDEA","IDLE",
            "INCH","INTO","IRON","ISLE","ITEM","JAIL","JERK","JOIN","JOKE",
            "JUMP","JUST","KEEN","KEEP","KICK","KIND","KING","KISS","KNEW",
            "KNOB","KNOW","LACK","LAKE","LAMP","LAND","LANE","LAST","LATE",
            "LAVA","LAWN","LEAD","LEAF","LEAN","LEAP","LEAN","LEFT","LEND",
            "LESS","LIFE","LIFT","LIKE","LIME","LINE","LINK","LION","LIST",
            "LIVE","LOAD","LOAN","LOCK","LOFT","LONE","LONG","LOOK","LOOP",
            "LOOT","LOSE","LOSS","LOST","LOVE","LUCK","LURE","LUSH","MADE",
            "MAIL","MAIN","MAKE","MALE","MALL","MANE","MANY","MARK","MASS",
            "MAST","MATE","MATH","MAZE","MEAL","MEAN","MEAT","MEET","MELT",
            "MEMO","MERE","MESH","MILD","MILE","MILK","MILL","MIND","MINE",
            "MINT","MISS","MODE","MOLD","MOLE","MOOD","MOON","MORE","MOST",
            "MOVE","MUCH","MUST","MYTH","NAIL","NAME","NAVY","NEAR","NECK",
            "NEED","NEST","NEWS","NEXT","NICE","NINE","NODE","NONE","NOON",
            "NORM","NOSE","NOTE","NULL","NUMB","OATH","ONCE","ONLY","OPEN",
            "OPUS","OVAL","OVEN","OVER","PACE","PACK","PAGE","PAID","PAIN",
            "PAIR","PALE","PALM","PANE","PARK","PART","PASS","PAST","PATH",
            "PAVE","PEAK","PEAR","PEEL","PEER","PICK","PIER","PILE","PILL",
            "PINE","PINK","PIPE","PLAN","PLAY","PLOT","PLOW","PLUG","PLUM",
            "PLUS","POEM","POET","POLE","POLL","POND","POOL","POOR","POPE",
            "PORK","PORT","POSE","POST","POUR","PREY","PROD","PROP","PULL",
            "PUMP","PURE","PUSH","RACK","RAIL","RAIN","RAKE","RAMP","RANG",
            "RANK","RARE","RATE","READ","REAL","REEL","RELY","RENT","REST",
            "RICE","RICH","RIDE","RING","RIOT","RISE","RISK","ROAD","ROAM",
            "ROAR","ROBE","ROCK","ROLE","ROLL","ROOF","ROOM","ROOT","ROPE",
            "ROSE","RUBY","RULE","RUSH","RUST","SAFE","SAGE","SAIL","SAKE",
            "SALE","SALT","SAME","SAND","SANE","SANG","SANK","SAVE","SCAN",
            "SCAR","SEAL","SEAM","SEEK","SEEM","SEEN","SELF","SELL","SEND",
            "SENT","SHED","SHIP","SHOP","SHOT","SHOW","SHUT","SICK","SIDE",
            "SIGN","SILK","SING","SINK","SITE","SIZE","SKIN","SKIP","SLAB",
            "SLAM","SLAP","SLIM","SLIP","SLOT","SLOW","SLUG","SNAP","SNOW",
            "SOAK","SOAP","SOCK","SOFT","SOIL","SOLD","SOLE","SOME","SONG",
            "SOON","SORT","SOUL","SOUP","SOUR","SPAN","SPAR","SPIN","SPIT",
            "SPOT","STAB","STAR","STAY","STEM","STEP","STIR","STOP","STOW",
            "STUB","SUCH","SUIT","SUNG","SUNK","SURF","SWAP","SWIM","TAIL",
            "TALE","TALL","TAME","TANK","TAPE","TASK","TEAR","TELL","TEND",
            "TENT","TERM","THAN","THAT","THEM","THEN","THEY","THIN","THIS",
            "THOU","THUS","TIDE","TILL","TILT","TIME","TINY","TIRE","TOAD",
            "TOLD","TOLL","TOMB","TONE","TOOK","TOOL","TORN","TOSS","TOUR",
            "TOWN","TRAP","TRAY","TREE","TRIM","TRIO","TRIP","TROT","TRUE",
            "TUBE","TUCK","TUFT","TUNE","TURF","TUSK","TWIN","TYPE","UGLY",
            "UNDO","UNIT","UPON","USED","USER","VALE","VANE","VARY","VAST",
            "VEIL","VEIN","VERB","VERY","VEST","VIEW","VINE","VISA","VOID",
            "VOLT","VOTE","WADE","WAGE","WAIL","WAKE","WALK","WALL","WAND",
            "WANT","WARD","WARM","WARP","WARY","WASH","WAVE","WEAK","WEAL",
            "WEAN","WEED","WEEK","WELL","WENT","WERE","WEST","WHAT","WHEN",
            "WHOM","WIDE","WIFE","WILD","WILL","WIND","WINE","WING","WIRE",
            "WISE","WISH","WITH","WOLF","WOOD","WOOL","WORD","WORE","WORK",
            "WORM","WORN","WOVE","WRAP","WREN","WRIT","YAWN","YEAR","YELL",
            "YOUR","ZONE","ZOOM",
        };
    }
}
