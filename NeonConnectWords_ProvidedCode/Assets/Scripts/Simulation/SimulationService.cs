using System;
using UnityEngine;

namespace NeonConnectWords.Simulation
{
    /// <summary>
    /// Unity-facing wrapper around the pure simulator.
    /// Use this as the integration point instead of putting rules directly in scene objects.
    /// </summary>
    public class SimulationService : MonoBehaviour
    {
        [Header("Dictionary")]
        public TextAsset wordListAsset;

        [Header("Random")]
        public int randomSeed = 12345;

        [Header("Board")]
        public int columns = 7;
        public int rows = 6;
        public int playerCount = 1;
        public int minWordLength = 3;
        public int letterChoiceCount = 4;

        [Header("Modes")]
        public int classicTargetScore = 100;
        public float timedDuration = 120f;

        [Header("Letters")]
        public string letterPool = "EEEEEEEEEEEEAAAAAAAAAIIIIIIOOOOOOUUUURRRRRRTTTTTTNNNNNNSSSSSSLLLLCCCCPPPPMMMMDDDDGGGGBBBBFFVVWWYYKKJJXXZZQQ";

        [Header("Combo Rules")]
        public float comboMultiplierStep = 0.5f;
        public float maxComboMultiplier = 5f;
        public int streakThreshold = 3;
        public float multiWordBonusStep = 0.35f;
        public float cascadeBonusStep = 0.5f;

        [Header("Power Up Rules")]
        public int wildcardUnlockAt = 20;
        public int bombUnlockAt = 50;
        public int swapUnlockAt = 80;
        public int wildcardEvery = 30;
        public int bombEvery = 60;
        public int swapEvery = 90;

        public NeonConnectSimulator Simulator { get; private set; }
        public SimulationState State => Simulator != null ? Simulator.State : null;

        private void Awake()
        {
            RebuildSimulator();
        }

        public void RebuildSimulator()
        {
            string[] words = wordListAsset != null
                ? wordListAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                : GetFallbackWords();

            SimulationConfig config = new SimulationConfig();
            config.Columns = columns;
            config.Rows = rows;
            config.PlayerCount = playerCount;
            config.LetterChoiceCount = letterChoiceCount;
            config.ClassicTargetScore = classicTargetScore;
            config.TimedDurationSeconds = timedDuration;
            config.MinWordLength = minWordLength;
            config.LetterPool = letterPool;
            config.ComboMultiplierStep = comboMultiplierStep;
            config.MaxComboMultiplier = maxComboMultiplier;
            config.StreakThreshold = streakThreshold;
            config.MultiWordBonusStep = multiWordBonusStep;
            config.CascadeBonusStep = cascadeBonusStep;
            config.WildcardUnlockAt = wildcardUnlockAt;
            config.BombUnlockAt = bombUnlockAt;
            config.SwapUnlockAt = swapUnlockAt;
            config.WildcardEvery = wildcardEvery;
            config.BombEvery = bombEvery;
            config.SwapEvery = swapEvery;

            Simulator = new NeonConnectSimulator(config, words, randomSeed);
        }

        public void StartSimulation(GameMode mode)
        {
            EnsureSimulator();
            Simulator.StartGame(MapMode(mode));
        }

        public SimulationTurnResult DropAtColumn(int column)
        {
            EnsureSimulator();
            return Simulator.DropCurrentLetter(column);
        }

        public bool ArmWildcard()
        {
            EnsureSimulator();
            return Simulator.ArmWildcard();
        }

        public bool SelectLetterIndex(int index)
        {
            EnsureSimulator();
            return Simulator.SetSelectedLetterIndex(index);
        }

        public SimulationTurnResult BombRow(int row)
        {
            EnsureSimulator();
            return Simulator.BombRow(row);
        }

        public SimulationTurnResult BombColumn(int column)
        {
            EnsureSimulator();
            return Simulator.BombColumn(column);
        }

        public SimulationTurnResult SwapTiles(Vector2Int first, Vector2Int second)
        {
            EnsureSimulator();
            return Simulator.SwapTiles(
                new SimulationCoord(first.x, first.y),
                new SimulationCoord(second.x, second.y));
        }

        public bool TickTimedMode(float deltaSeconds)
        {
            EnsureSimulator();
            return Simulator.AdvanceTime(deltaSeconds);
        }

        public void ResetBoardOnly()
        {
            EnsureSimulator();
            Simulator.ResetBoardOnly();
        }

        public bool SeedTileInColumn(int column, char letter, int ownerIndex, bool wildcard)
        {
            EnsureSimulator();
            return Simulator.SeedTileInColumn(column, letter, ownerIndex, wildcard);
        }

        private void EnsureSimulator()
        {
            if (Simulator == null)
            {
                RebuildSimulator();
            }
        }

        private SimulationGameMode MapMode(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Timed:
                    return SimulationGameMode.Timed;
                case GameMode.Puzzle:
                    return SimulationGameMode.Puzzle;
                case GameMode.Tutorial:
                    return SimulationGameMode.Tutorial;
                default:
                    return SimulationGameMode.Classic;
            }
        }

        private string[] GetFallbackWords()
        {
            return new[]
            {
                "ACE", "ACT", "ADD", "AGE", "AIM", "AIR", "AND", "ANT", "ARC", "ARE",
                "ART", "ASK", "ATE", "BAD", "BAG", "BAR", "BAT", "BED", "BIG", "BIT",
                "BOX", "BOY", "BUS", "BUT", "CAN", "CAP", "CAR", "CAT", "COD", "COT",
                "COW", "CUP", "CUT", "DOG", "DOT", "DRY", "EAR", "EAT", "END", "ERA",
                "FAR", "FAT", "FIG", "FIN", "FIT", "FLY", "FOG", "FOX", "FUN", "FUR",
                "GAP", "GAS", "GET", "GOD", "GOT", "GUM", "GUN", "GUT", "HAT", "HEN",
                "HER", "HIT", "HOP", "HOT", "HUG", "ICE", "INK", "ION", "JAR", "JET",
                "JOB", "JOG", "JOY", "KIT", "LAB", "LAP", "LAW", "LAY", "LEG", "LET",
                "LID", "LIP", "LIT", "LOG", "LOT", "LOW", "MAP", "MAT", "NET", "NEW",
                "NOD", "NOT", "NOW", "NUT", "OAK", "OAR", "OAT", "OLD", "ORB", "ORE",
                "OWL", "PAD", "PAL", "PAN", "PAR", "PAT", "PAW", "PAY", "PEG", "PEN",
                "PET", "PIG", "PIN", "PIT", "POD", "POP", "POT", "PUB", "PUP", "PUT",
                "RAG", "RAM", "RAN", "RAP", "RAT", "RAW", "RAY", "RED", "RIB", "RIG",
                "RIM", "RIP", "ROB", "ROD", "ROT", "ROW", "RUB", "RUG", "RUN", "SAD",
                "SAG", "SAP", "SAT", "SAW", "SEA", "SET", "SIP", "SIR", "SIT", "SKY",
                "SOB", "SON", "SPA", "SPY", "SUB", "SUM", "SUN", "SUP", "TAB", "TAN",
                "TAP", "TAR", "TEA", "TEN", "TIE", "TIN", "TIP", "TOE", "TOP", "TOY"
            };
        }
    }
}
