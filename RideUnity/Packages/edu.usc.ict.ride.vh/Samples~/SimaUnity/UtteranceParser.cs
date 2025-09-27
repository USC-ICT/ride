using System;
using System.Collections.Generic;

public static class UtteranceParser
{
    /// <summary>
    /// Parses an utterance into a list of (mark, word) pairs.
    /// Each word gets a start mark (T0, T2, T4...) and is followed by an end mark (T1, T3, T5...).
    /// </summary>
    public static List<(string Mark, string Word)> SimpleParser(string sentence)
    {
        var parsed = new List<(string, string)>();
        var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i++)
        {
            string mark = $"T{i * 2}";
            parsed.Add((mark, words[i]));
        }

        return parsed;
    }
}
