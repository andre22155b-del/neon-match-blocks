using System;
using System.Collections.Generic;
using System.Text;

namespace NeonConnectWords.Simulation
{
    public sealed class SimulationDictionary
    {
        private sealed class TrieNode
        {
            public readonly Dictionary<char, TrieNode> Children = new Dictionary<char, TrieNode>();
            public bool IsTerminal;
        }

        private static readonly SimulationCoord[] Directions =
        {
            new SimulationCoord(1, 0),
            new SimulationCoord(0, 1),
            new SimulationCoord(1, 1),
            new SimulationCoord(1, -1)
        };

        private static readonly SimulationWordDirection[] DirectionLabels =
        {
            SimulationWordDirection.Horizontal,
            SimulationWordDirection.Vertical,
            SimulationWordDirection.DiagonalUp,
            SimulationWordDirection.DiagonalDown
        };

        private static readonly char[] Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        private readonly TrieNode root = new TrieNode();
        private readonly HashSet<string> words = new HashSet<string>();
        private readonly HashSet<string> prefixes = new HashSet<string>();
        private readonly int minWordLength;

        public SimulationDictionary(IEnumerable<string> sourceWords, int minWordLength)
        {
            this.minWordLength = Math.Max(2, minWordLength);
            Load(sourceWords);
        }

        public bool IsValidWord(string candidate)
        {
            return words.Contains(Normalize(candidate));
        }

        public List<SimulationWordResult> FindAllWords(SimulationTile[,] board)
        {
            int columns = board.GetLength(0);
            int rows = board.GetLength(1);
            List<SimulationWordResult> results = new List<SimulationWordResult>();
            HashSet<string> emitted = new HashSet<string>();

            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++)
                {
                    if (board[column, row] == null)
                    {
                        continue;
                    }

                    for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
                    {
                        List<SimulationCoord> positions = new List<SimulationCoord>();
                        Explore(
                            board,
                            column,
                            row,
                            Directions[directionIndex],
                            DirectionLabels[directionIndex],
                            string.Empty,
                            positions,
                            results,
                            emitted);
                    }
                }
            }

            return results;
        }

        private void Load(IEnumerable<string> sourceWords)
        {
            if (sourceWords == null)
            {
                return;
            }

            foreach (string raw in sourceWords)
            {
                string cleaned = Normalize(raw);
                if (cleaned.Length < minWordLength || !words.Add(cleaned))
                {
                    continue;
                }

                TrieNode node = root;
                StringBuilder prefixBuilder = new StringBuilder(cleaned.Length);
                for (int i = 0; i < cleaned.Length; i++)
                {
                    char c = cleaned[i];
                    TrieNode next;
                    if (!node.Children.TryGetValue(c, out next))
                    {
                        next = new TrieNode();
                        node.Children.Add(c, next);
                    }

                    node = next;
                    if (i < cleaned.Length - 1)
                    {
                        prefixBuilder.Append(c);
                        prefixes.Add(prefixBuilder.ToString());
                    }
                }

                node.IsTerminal = true;
            }
        }

        private void Explore(
            SimulationTile[,] board,
            int column,
            int row,
            SimulationCoord direction,
            SimulationWordDirection directionLabel,
            string current,
            List<SimulationCoord> positions,
            List<SimulationWordResult> results,
            HashSet<string> emitted)
        {
            int columns = board.GetLength(0);
            int rows = board.GetLength(1);
            if (column < 0 || column >= columns || row < 0 || row >= rows)
            {
                return;
            }

            SimulationTile tile = board[column, row];
            if (tile == null)
            {
                return;
            }

            positions.Add(new SimulationCoord(column, row));

            if (tile.IsWildcard)
            {
                for (int i = 0; i < Alphabet.Length; i++)
                {
                    string candidate = current + Alphabet[i];
                    if (!CanContinue(candidate))
                    {
                        continue;
                    }

                    Emit(candidate, positions, directionLabel, results, emitted);
                    Explore(
                        board,
                        column + direction.Column,
                        row + direction.Row,
                        direction,
                        directionLabel,
                        candidate,
                        positions,
                        results,
                        emitted);
                }

                positions.RemoveAt(positions.Count - 1);
                return;
            }

            string next = current + char.ToUpperInvariant(tile.Letter);
            if (!CanContinue(next))
            {
                positions.RemoveAt(positions.Count - 1);
                return;
            }

            Emit(next, positions, directionLabel, results, emitted);
            Explore(
                board,
                column + direction.Column,
                row + direction.Row,
                direction,
                directionLabel,
                next,
                positions,
                results,
                emitted);
            positions.RemoveAt(positions.Count - 1);
        }

        private void Emit(
            string candidate,
            List<SimulationCoord> positions,
            SimulationWordDirection directionLabel,
            List<SimulationWordResult> results,
            HashSet<string> emitted)
        {
            if (candidate.Length < minWordLength || !words.Contains(candidate))
            {
                return;
            }

            SimulationCoord start = positions[0];
            string key = candidate + ":" + directionLabel + ":" + start.Column + ":" + start.Row + ":" + positions.Count;
            if (!emitted.Add(key))
            {
                return;
            }

            SimulationWordResult result = new SimulationWordResult();
            result.Word = candidate;
            result.Direction = directionLabel;
            result.Points = candidate.Length;
            result.Positions.AddRange(positions);
            results.Add(result);
        }

        private bool CanContinue(string candidate)
        {
            return words.Contains(candidate) || prefixes.Contains(candidate);
        }

        private string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = char.ToUpperInvariant(raw[i]);
                if (c >= 'A' && c <= 'Z')
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
