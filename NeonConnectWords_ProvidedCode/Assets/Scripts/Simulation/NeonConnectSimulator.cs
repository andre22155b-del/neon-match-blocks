using System;
using System.Collections.Generic;

namespace NeonConnectWords.Simulation
{
    /// <summary>
    /// Deterministic game rules engine. This is the source of truth for board state,
    /// scoring, power-up inventory, turn order, and cascades.
    /// </summary>
    public sealed class NeonConnectSimulator
    {
        private readonly SimulationConfig config;
        private readonly SimulationDictionary dictionary;
        private Random random;

        public NeonConnectSimulator(SimulationConfig config, IEnumerable<string> dictionaryWords, int seed)
        {
            this.config = config ?? throw new ArgumentNullException("config");
            dictionary = new SimulationDictionary(dictionaryWords, config.MinWordLength);
            random = new Random(seed);
            State = new SimulationState(config);
        }

        public SimulationState State { get; private set; }

        public void StartGame(SimulationGameMode mode)
        {
            State = new SimulationState(config);
            State.Mode = mode;
            State.ComboMultiplier = 1f;
            State.TimedRemaining = config.TimedDurationSeconds;

            for (int i = 0; i < config.PlayerCount; i++)
            {
                State.CurrentLetters[i] = DrawLetter();
            }
        }

        public bool AdvanceTime(float deltaSeconds)
        {
            if (State.GameOver || State.Mode != SimulationGameMode.Timed)
            {
                return false;
            }

            State.TimedRemaining = Math.Max(0f, State.TimedRemaining - Math.Max(0f, deltaSeconds));
            if (State.TimedRemaining <= 0f)
            {
                EndGame(BuildWinnerMessage("Time's up"));
                return true;
            }

            return false;
        }

        public bool ArmWildcard()
        {
            if (State.GameOver || State.PowerUps.WildcardCharges <= 0)
            {
                return false;
            }

            State.PowerUps.WildcardCharges--;
            State.CurrentLetters[State.CurrentPlayerIndex] = '*';
            return true;
        }

        public void ResetBoardOnly()
        {
            State.Board = new SimulationTile[config.Columns, config.Rows];
            State.ColumnHeights = new int[config.Columns];
            State.ConsecutiveWordTurns = 0;
            State.ComboMultiplier = 1f;
        }

        public bool SeedTileInColumn(int column, char letter, int ownerIndex, bool wildcard)
        {
            if (State.GameOver || column < 0 || column >= config.Columns || State.ColumnHeights[column] >= config.Rows)
            {
                return false;
            }

            int row = State.ColumnHeights[column];
            State.Board[column, row] = new SimulationTile
            {
                Letter = char.ToUpperInvariant(letter),
                OwnerIndex = ownerIndex,
                IsWildcard = wildcard
            };
            State.ColumnHeights[column]++;
            return true;
        }

        public SimulationTurnResult DropCurrentLetter(int column)
        {
            if (!CanDrop(column))
            {
                return Failure(SimulationActionType.Drop, "Column is full or invalid.");
            }

            int actingPlayer = State.CurrentPlayerIndex;
            int row = State.ColumnHeights[column];
            char letter = State.CurrentLetters[actingPlayer];
            bool wildcard = letter == '*';

            SimulationTile tile = new SimulationTile
            {
                Letter = wildcard ? 'A' : letter,
                OwnerIndex = actingPlayer,
                IsWildcard = wildcard
            };

            State.Board[column, row] = tile;
            State.ColumnHeights[column]++;

            SimulationTilePlacement placement = new SimulationTilePlacement
            {
                Column = column,
                Row = row,
                Letter = letter,
                PlayerIndex = actingPlayer,
                IsWildcard = wildcard
            };

            return ResolveAction(SimulationActionType.Drop, actingPlayer, placement);
        }

        public SimulationTurnResult BombRow(int row)
        {
            if (State.GameOver || row < 0 || row >= config.Rows)
            {
                return Failure(SimulationActionType.BombRow, "Invalid row.");
            }

            if (State.PowerUps.BombCharges <= 0)
            {
                return Failure(SimulationActionType.BombRow, "No bomb charges remaining.");
            }

            State.PowerUps.BombCharges--;

            for (int column = 0; column < config.Columns; column++)
            {
                State.Board[column, row] = null;
            }

            RecalculateColumnHeights();
            ApplyGravity();
            return ResolveAction(SimulationActionType.BombRow, State.CurrentPlayerIndex, null);
        }

        public SimulationTurnResult BombColumn(int column)
        {
            if (State.GameOver || column < 0 || column >= config.Columns)
            {
                return Failure(SimulationActionType.BombColumn, "Invalid column.");
            }

            if (State.PowerUps.BombCharges <= 0)
            {
                return Failure(SimulationActionType.BombColumn, "No bomb charges remaining.");
            }

            State.PowerUps.BombCharges--;

            for (int row = 0; row < config.Rows; row++)
            {
                State.Board[column, row] = null;
            }

            RecalculateColumnHeights();
            ApplyGravity();
            return ResolveAction(SimulationActionType.BombColumn, State.CurrentPlayerIndex, null);
        }

        public SimulationTurnResult SwapTiles(SimulationCoord first, SimulationCoord second)
        {
            if (State.GameOver || !IsInBounds(first.Column, first.Row) || !IsInBounds(second.Column, second.Row))
            {
                return Failure(SimulationActionType.Swap, "Invalid swap coordinates.");
            }

            if (State.PowerUps.SwapCharges <= 0)
            {
                return Failure(SimulationActionType.Swap, "No swap charges remaining.");
            }

            if (State.Board[first.Column, first.Row] == null || State.Board[second.Column, second.Row] == null)
            {
                return Failure(SimulationActionType.Swap, "Both swap targets must contain tiles.");
            }

            State.PowerUps.SwapCharges--;

            SimulationTile temporary = State.Board[first.Column, first.Row];
            State.Board[first.Column, first.Row] = State.Board[second.Column, second.Row];
            State.Board[second.Column, second.Row] = temporary;

            RecalculateColumnHeights();
            ApplyGravity();
            return ResolveAction(SimulationActionType.Swap, State.CurrentPlayerIndex, null);
        }

        public bool CanDrop(int column)
        {
            return !State.GameOver && column >= 0 && column < config.Columns && State.ColumnHeights[column] < config.Rows;
        }

        private SimulationTurnResult ResolveAction(SimulationActionType actionType, int actingPlayer, SimulationTilePlacement placement)
        {
            SimulationTurnResult result = new SimulationTurnResult();
            result.Success = true;
            result.ActionType = actionType;
            result.ActingPlayerIndex = actingPlayer;
            result.Placement = placement;

            List<SimulationCascadeResult> cascades = ResolveCascades();
            result.Cascades.AddRange(cascades);

            bool scored = cascades.Count > 0;
            UpdateTurnCombo(scored);

            int awardedPoints = ScoreCascades(cascades);
            State.Scores[actingPlayer] += awardedPoints;
            result.PointsAwarded = awardedPoints;
            result.ScoreAfterAction = State.Scores[actingPlayer];
            result.ComboMultiplier = State.ComboMultiplier;

            UpdatePowerUpUnlocks(State.Scores[actingPlayer]);

            State.TurnNumber++;
            if (State.Mode == SimulationGameMode.Classic && State.Scores[actingPlayer] >= config.ClassicTargetScore)
            {
                EndGame("Player " + (actingPlayer + 1) + " wins!");
            }
            else if (IsBoardFull())
            {
                EndGame(BuildWinnerMessage("Board full"));
            }

            if (!State.GameOver)
            {
                State.CurrentLetters[actingPlayer] = DrawLetter();
                State.CurrentPlayerIndex = (State.CurrentPlayerIndex + 1) % config.PlayerCount;
                result.TurnAdvanced = true;
            }

            result.NextPlayerIndex = State.CurrentPlayerIndex;
            result.GameOver = State.GameOver;
            result.GameOverMessage = State.GameOverMessage;
            return result;
        }

        private List<SimulationCascadeResult> ResolveCascades()
        {
            List<SimulationCascadeResult> cascades = new List<SimulationCascadeResult>();
            int depth = 1;

            while (true)
            {
                List<SimulationWordResult> words = dictionary.FindAllWords(State.Board);
                if (words.Count == 0)
                {
                    break;
                }

                HashSet<SimulationCoord> cellsToClear = new HashSet<SimulationCoord>();
                for (int i = 0; i < words.Count; i++)
                {
                    for (int p = 0; p < words[i].Positions.Count; p++)
                    {
                        cellsToClear.Add(words[i].Positions[p]);
                    }
                }

                SimulationCascadeResult cascade = new SimulationCascadeResult();
                cascade.Depth = depth;
                cascade.Words.AddRange(words);
                cascade.ClearedCells.AddRange(cellsToClear);
                cascades.Add(cascade);

                foreach (SimulationCoord coord in cellsToClear)
                {
                    State.Board[coord.Column, coord.Row] = null;
                }

                RecalculateColumnHeights();
                ApplyGravity();
                depth++;
            }

            return cascades;
        }

        private int ScoreCascades(List<SimulationCascadeResult> cascades)
        {
            int total = 0;
            for (int i = 0; i < cascades.Count; i++)
            {
                SimulationCascadeResult cascade = cascades[i];
                int basePoints = 0;
                for (int w = 0; w < cascade.Words.Count; w++)
                {
                    basePoints += cascade.Words[w].Points;
                }

                float multiWordFactor = 1f + Math.Max(0, cascade.Words.Count - 1) * config.MultiWordBonusStep;
                float cascadeFactor = 1f + Math.Max(0, cascade.Depth - 1) * config.CascadeBonusStep;
                cascade.AwardedPoints = (int)Math.Round(basePoints * State.ComboMultiplier * multiWordFactor * cascadeFactor);
                total += cascade.AwardedPoints;
            }

            return total;
        }

        private void UpdateTurnCombo(bool scored)
        {
            if (!scored)
            {
                State.ConsecutiveWordTurns = 0;
                State.ComboMultiplier = 1f;
                return;
            }

            State.ConsecutiveWordTurns++;
            if (State.ConsecutiveWordTurns >= config.StreakThreshold)
            {
                State.ComboMultiplier = Math.Min(config.MaxComboMultiplier, State.ComboMultiplier + config.ComboMultiplierStep);
            }
        }

        private void UpdatePowerUpUnlocks(int totalPoints)
        {
            SimulationPowerUpInventory powerUps = State.PowerUps;

            if (!powerUps.WildcardUnlocked && totalPoints >= config.WildcardUnlockAt)
            {
                powerUps.WildcardUnlocked = true;
                powerUps.WildcardCharges = 1;
                powerUps.PointsAtLastWildcard = totalPoints;
            }

            if (!powerUps.BombUnlocked && totalPoints >= config.BombUnlockAt)
            {
                powerUps.BombUnlocked = true;
                powerUps.BombCharges = 1;
                powerUps.PointsAtLastBomb = totalPoints;
            }

            if (!powerUps.SwapUnlocked && totalPoints >= config.SwapUnlockAt)
            {
                powerUps.SwapUnlocked = true;
                powerUps.SwapCharges = 1;
                powerUps.PointsAtLastSwap = totalPoints;
            }

            if (powerUps.WildcardUnlocked && totalPoints - powerUps.PointsAtLastWildcard >= config.WildcardEvery)
            {
                powerUps.WildcardCharges++;
                powerUps.PointsAtLastWildcard = totalPoints;
            }

            if (powerUps.BombUnlocked && totalPoints - powerUps.PointsAtLastBomb >= config.BombEvery)
            {
                powerUps.BombCharges++;
                powerUps.PointsAtLastBomb = totalPoints;
            }

            if (powerUps.SwapUnlocked && totalPoints - powerUps.PointsAtLastSwap >= config.SwapEvery)
            {
                powerUps.SwapCharges++;
                powerUps.PointsAtLastSwap = totalPoints;
            }
        }

        private void ApplyGravity()
        {
            for (int column = 0; column < config.Columns; column++)
            {
                int writeRow = 0;
                for (int row = 0; row < config.Rows; row++)
                {
                    SimulationTile tile = State.Board[column, row];
                    if (tile == null)
                    {
                        continue;
                    }

                    if (row != writeRow)
                    {
                        State.Board[column, writeRow] = tile;
                        State.Board[column, row] = null;
                    }

                    writeRow++;
                }

                State.ColumnHeights[column] = writeRow;
            }
        }

        private void RecalculateColumnHeights()
        {
            for (int column = 0; column < config.Columns; column++)
            {
                int height = 0;
                for (int row = 0; row < config.Rows; row++)
                {
                    if (State.Board[column, row] != null)
                    {
                        height = row + 1;
                    }
                }

                State.ColumnHeights[column] = height;
            }
        }

        private char DrawLetter()
        {
            if (string.IsNullOrEmpty(config.LetterPool))
            {
                return 'E';
            }

            return char.ToUpperInvariant(config.LetterPool[random.Next(config.LetterPool.Length)]);
        }

        private bool IsBoardFull()
        {
            for (int column = 0; column < config.Columns; column++)
            {
                if (State.ColumnHeights[column] < config.Rows)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsInBounds(int column, int row)
        {
            return column >= 0 && column < config.Columns && row >= 0 && row < config.Rows;
        }

        private string BuildWinnerMessage(string reason)
        {
            int bestPlayer = 0;
            bool tie = false;
            for (int i = 1; i < State.Scores.Length; i++)
            {
                if (State.Scores[i] > State.Scores[bestPlayer])
                {
                    bestPlayer = i;
                    tie = false;
                }
                else if (State.Scores[i] == State.Scores[bestPlayer])
                {
                    tie = true;
                }
            }

            return tie ? reason + " - tie game" : reason + " - Player " + (bestPlayer + 1) + " wins";
        }

        private void EndGame(string message)
        {
            State.GameOver = true;
            State.GameOverMessage = message;
        }

        private SimulationTurnResult Failure(SimulationActionType actionType, string message)
        {
            SimulationTurnResult result = new SimulationTurnResult();
            result.Success = false;
            result.ActionType = actionType;
            result.FailureReason = message;
            result.ActingPlayerIndex = State.CurrentPlayerIndex;
            result.NextPlayerIndex = State.CurrentPlayerIndex;
            result.ComboMultiplier = State.ComboMultiplier;
            result.ScoreAfterAction = State.Scores.Length > 0 ? State.Scores[State.CurrentPlayerIndex] : 0;
            result.GameOver = State.GameOver;
            result.GameOverMessage = State.GameOverMessage;
            return result;
        }
    }
}
