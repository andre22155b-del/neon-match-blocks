using System;
using System.Collections.Generic;

namespace NeonConnectWords.Simulation
{
    public enum SimulationGameMode
    {
        Classic,
        Timed,
        Puzzle,
        Tutorial
    }

    public enum SimulationPowerUpType
    {
        Wildcard,
        Bomb,
        Swap
    }

    public enum SimulationActionType
    {
        None,
        Drop,
        BombRow,
        BombColumn,
        Swap
    }

    public enum SimulationWordDirection
    {
        Horizontal,
        Vertical,
        DiagonalUp,
        DiagonalDown
    }

    public struct SimulationCoord : IEquatable<SimulationCoord>
    {
        public int Column;
        public int Row;

        public SimulationCoord(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public bool Equals(SimulationCoord other)
        {
            return Column == other.Column && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is SimulationCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Column * 397) ^ Row;
            }
        }
    }

    public sealed class SimulationConfig
    {
        public int Columns = 7;
        public int Rows = 6;
        public int PlayerCount = 2;
        public int ClassicTargetScore = 100;
        public float TimedDurationSeconds = 120f;
        public int MinWordLength = 3;
        public string LetterPool = "EEEEEEEEEEEEAAAAAAAAAIIIIIIOOOOOOUUUURRRRRRTTTTTTNNNNNNSSSSSSLLLLCCCCPPPPMMMMDDDDGGGGBBBBFFVVWWYYKKJJXXZZQQ";

        public float ComboMultiplierStep = 0.5f;
        public float MaxComboMultiplier = 5f;
        public int StreakThreshold = 3;
        public float MultiWordBonusStep = 0.35f;
        public float CascadeBonusStep = 0.5f;

        public int WildcardUnlockAt = 20;
        public int BombUnlockAt = 50;
        public int SwapUnlockAt = 80;
        public int WildcardEvery = 30;
        public int BombEvery = 60;
        public int SwapEvery = 90;
    }

    public sealed class SimulationTile
    {
        public char Letter;
        public int OwnerIndex;
        public bool IsWildcard;

        public SimulationTile Clone()
        {
            return new SimulationTile
            {
                Letter = Letter,
                OwnerIndex = OwnerIndex,
                IsWildcard = IsWildcard
            };
        }
    }

    public sealed class SimulationWordResult
    {
        public string Word;
        public List<SimulationCoord> Positions = new List<SimulationCoord>();
        public SimulationWordDirection Direction;
        public int Points;
    }

    public sealed class SimulationTilePlacement
    {
        public int Column;
        public int Row;
        public char Letter;
        public int PlayerIndex;
        public bool IsWildcard;
    }

    public sealed class SimulationCascadeResult
    {
        public int Depth;
        public List<SimulationWordResult> Words = new List<SimulationWordResult>();
        public List<SimulationCoord> ClearedCells = new List<SimulationCoord>();
        public int AwardedPoints;
    }

    public sealed class SimulationTurnResult
    {
        public bool Success;
        public string FailureReason;
        public SimulationActionType ActionType;
        public SimulationTilePlacement Placement;
        public List<SimulationCascadeResult> Cascades = new List<SimulationCascadeResult>();
        public int ActingPlayerIndex;
        public int NextPlayerIndex;
        public int PointsAwarded;
        public int ScoreAfterAction;
        public float ComboMultiplier;
        public bool TurnAdvanced;
        public bool GameOver;
        public string GameOverMessage;
    }

    public sealed class SimulationPowerUpInventory
    {
        public int WildcardCharges;
        public int BombCharges;
        public int SwapCharges;

        public bool WildcardUnlocked;
        public bool BombUnlocked;
        public bool SwapUnlocked;

        public int PointsAtLastWildcard;
        public int PointsAtLastBomb;
        public int PointsAtLastSwap;
    }

    public sealed class SimulationState
    {
        public SimulationState(SimulationConfig config)
        {
            Board = new SimulationTile[config.Columns, config.Rows];
            ColumnHeights = new int[config.Columns];
            Scores = new int[config.PlayerCount];
            CurrentLetters = new char[config.PlayerCount];
            PowerUps = new SimulationPowerUpInventory();
        }

        public SimulationGameMode Mode;
        public SimulationTile[,] Board;
        public int[] ColumnHeights;
        public int[] Scores;
        public char[] CurrentLetters;
        public int CurrentPlayerIndex;
        public int ConsecutiveWordTurns;
        public float ComboMultiplier = 1f;
        public bool GameOver;
        public string GameOverMessage;
        public float TimedRemaining;
        public int TurnNumber;
        public SimulationPowerUpInventory PowerUps;
    }
}
