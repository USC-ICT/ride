using System;
using System.Linq;


/// <summary>
/// Locates the timing marker index (T#) for a target token/phrase inside an utterance,
/// given the broader phrase in which the target appears.
/// </summary>
/// <remarks>
/// Workflow:
/// 1) Tokenize <paramref name="sentence"/>, <paramref name="phrase"/>, and <paramref name="target"/> by spaces.
/// 2) Find the start index of <paramref name="phrase"/> within <paramref name="sentence"/> (word-based match).
/// 3) Find the start index of <paramref name="target"/> within <paramref name="phrase"/> (word-based match).
/// 4) Return <c>(phraseStart + targetStart) * 2</c>, which maps to your T-mark scheme (T0 before word0, T1 after word0, etc.).
///
/// Returns <c>null</c> if the phrase or target can’t be found.
/// Matching is case-sensitive and punctuation-sensitive as written. If you need
/// case-insensitive or punctuation-normalized matching, consider pre-normalizing inputs
/// or adding a StringComparison-based overload.
/// </remarks>
public static class TimingMarkerLocator
{
    public static int? Locate(string sentence, string phrase, string target)
    {
        var sentenceWords = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var phraseWords = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var targetWords = target.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Step 1: Find start index of phrase in sentence
        int phraseStartIndex = -1;
        for (int i = 0; i <= sentenceWords.Length - phraseWords.Length; i++)
        {
            if (sentenceWords.Skip(i).Take(phraseWords.Length).SequenceEqual(phraseWords))
            {
                phraseStartIndex = i;
                break;
            }
        }

        if (phraseStartIndex == -1)
        {
            Console.Error.WriteLine($"[TimingMarkerLocator] Phrase '{phrase}' not found in sentence.");
            return null;
        }

        // Step 2: Find start index of target in phrase
        for (int j = 0; j <= phraseWords.Length - targetWords.Length; j++)
        {
            if (phraseWords.Skip(j).Take(targetWords.Length).SequenceEqual(targetWords))
            {
                return (phraseStartIndex + j) * 2;
            }
        }

        Console.Error.WriteLine($"[TimingMarkerLocator] Target '{target}' not found in phrase.");
        return null;
    }
}
