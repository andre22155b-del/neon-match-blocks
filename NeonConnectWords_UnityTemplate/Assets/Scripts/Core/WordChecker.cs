using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum WordDirection
{
    Horizontal,
    Vertical,
    DiagonalUp,
    DiagonalDown
}

/// <summary>
/// Data returned for each found word. Positions are in board coordinates.
/// </summary>
public class WordResult
{
    public string word;
    public List<Vector2Int> positions;
    public WordDirection direction;
    public int points;

    public WordResult(string word, List<Vector2Int> positions, WordDirection direction)
    {
        this.word = word;
        this.positions = positions;
        this.direction = direction;
        points = word.Length;
    }
}

/// <summary>
/// Dictionary-backed board scanner with prefix pruning and wildcard support.
/// </summary>
public class WordChecker : MonoBehaviour
{
    [Header("Dictionary")]
    public TextAsset dictionaryAsset;
    public int minWordLength = 3;
    public bool logDictionaryStats;

    private static readonly Vector2Int[] ScanDirections =
    {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1)
    };

    private static readonly WordDirection[] DirectionLabels =
    {
        WordDirection.Horizontal,
        WordDirection.Vertical,
        WordDirection.DiagonalUp,
        WordDirection.DiagonalDown
    };

    private static readonly char[] Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    private readonly HashSet<string> dictionaryWords = new HashSet<string>();
    private readonly HashSet<string> validPrefixes = new HashSet<string>();

    private void Awake()
    {
        LoadDictionary();
    }

    public void LoadDictionary()
    {
        dictionaryWords.Clear();
        validPrefixes.Clear();

        string[] words = dictionaryAsset != null
            ? dictionaryAsset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
            : GetFallbackWords();

        for (int i = 0; i < words.Length; i++)
        {
            string cleaned = NormalizeWord(words[i]);
            if (cleaned.Length < minWordLength)
            {
                continue;
            }

            if (!dictionaryWords.Add(cleaned))
            {
                continue;
            }

            StringBuilder prefixBuilder = new StringBuilder(cleaned.Length);
            for (int c = 0; c < cleaned.Length - 1; c++)
            {
                prefixBuilder.Append(cleaned[c]);
                validPrefixes.Add(prefixBuilder.ToString());
            }
        }

        if (logDictionaryStats)
        {
            Debug.Log($"WordChecker loaded {dictionaryWords.Count} words and {validPrefixes.Count} prefixes.");
        }
    }

    public bool IsValidWord(string word)
    {
        return dictionaryWords.Contains(NormalizeWord(word));
    }

    public List<WordResult> FindAllWords(LetterTile[,] board, int columns, int rows)
    {
        List<WordResult> results = new List<WordResult>();
        HashSet<string> emittedKeys = new HashSet<string>();

        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (board[column, row] == null)
                {
                    continue;
                }

                for (int directionIndex = 0; directionIndex < ScanDirections.Length; directionIndex++)
                {
                    List<Vector2Int> positions = new List<Vector2Int>();
                    ExploreFrom(board, columns, rows, column, row, ScanDirections[directionIndex], DirectionLabels[directionIndex], string.Empty, positions, results, emittedKeys);
                }
            }
        }

        return results;
    }

    private void ExploreFrom(
        LetterTile[,] board,
        int columns,
        int rows,
        int column,
        int row,
        Vector2Int direction,
        WordDirection label,
        string current,
        List<Vector2Int> positions,
        List<WordResult> results,
        HashSet<string> emittedKeys)
    {
        if (column < 0 || column >= columns || row < 0 || row >= rows)
        {
            return;
        }

        LetterTile tile = board[column, row];
        if (tile == null)
        {
            return;
        }

        Vector2Int position = new Vector2Int(column, row);
        positions.Add(position);

        if (tile.IsWildcard)
        {
            for (int i = 0; i < Alphabet.Length; i++)
            {
                string candidate = current + Alphabet[i];
                if (!CanContinue(candidate))
                {
                    continue;
                }

                EmitWordIfValid(candidate, positions, label, results, emittedKeys);
                ExploreFrom(board, columns, rows, column + direction.x, row + direction.y, direction, label, candidate, positions, results, emittedKeys);
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

        EmitWordIfValid(next, positions, label, results, emittedKeys);
        ExploreFrom(board, columns, rows, column + direction.x, row + direction.y, direction, label, next, positions, results, emittedKeys);
        positions.RemoveAt(positions.Count - 1);
    }

    private void EmitWordIfValid(string candidate, List<Vector2Int> positions, WordDirection label, List<WordResult> results, HashSet<string> emittedKeys)
    {
        if (candidate.Length < minWordLength || !dictionaryWords.Contains(candidate))
        {
            return;
        }

        Vector2Int start = positions[0];
        string key = $"{candidate}:{label}:{start.x}:{start.y}:{positions.Count}";
        if (!emittedKeys.Add(key))
        {
            return;
        }

        results.Add(new WordResult(candidate, new List<Vector2Int>(positions), label));
    }

    private bool CanContinue(string candidate)
    {
        return dictionaryWords.Contains(candidate) || validPrefixes.Contains(candidate);
    }

    private string NormalizeWord(string raw)
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

    private string[] GetFallbackWords()
    {
        return new[]
        {
            "CAT", "DOG", "SUN", "SKY", "STAR", "GLOW", "NEON", "WORD", "GRID", "DROP",
            "GAME", "PLAY", "BLOCK", "BOMB", "SWAP", "CODE", "FALL", "LINE", "LAMP", "WAVE",
            "CORE", "MOVE", "TIME", "TURN", "COIN", "SCORE", "CHAIN", "LASER", "LIGHT", "SPARK"
        };
    }
}
