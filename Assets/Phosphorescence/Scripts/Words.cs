using PhosphorescenceExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Contains information of all the possible words, and methods to generate code, in the plan of new potential words.
/// </summary>
internal static class Words
{
    /// <summary>
    /// This determines how long the sequences are.
    /// </summary>
	internal const int SequenceLength = 8;

    /// <summary>
    /// This determines the amount of words needed in the array for the array to be considered usable.
    /// </summary>
    internal const int MinAcceptableWordSet = 5;

    /// <summary>
    /// Contains all distinct characters within the button colors enum.
    /// </summary>
    public static string ValidAlphabet { get; private set; }

    /// <summary>
    /// Contains the characters submitted for all indexes.
    /// </summary>
	public static string[] ValidChars { get; private set; }

    /// <summary>
    /// Contains all words. Indexes represent the same indexes in _validChars.
    /// This means that index 1 should contain words like "ad", from _validChars' "erlaeyhl" -> "deuglaia" (index 1-2).
    /// </summary>
    public static string[][] ValidWords { get; private set; }

    /// <summary>
    /// Returns the array of all colors from ButtonType in a Color32 equivalent datatype.
    /// </summary>
    internal static Color32[] Colors { get; private set; }

    /// <summary>
    /// Sets the valid words property to all words that are valid.
    /// </summary>
    internal static void Init(TextAsset words)
    {
        // If the method has run at least once, we do not need to initalize this again.
        if (ValidWords != null)
            return;

        ValidChars = GetAllChars(Enum.GetNames(typeof(ButtonType)));
        ValidWords = GetAllWords(words, ValidChars);
        ValidAlphabet = ValidChars.Join("").Distinct().OrderBy(c => c).Join("");
        Colors = Enum.GetValues(typeof(ButtonType)).Cast<ButtonType>().IterateColors(); 

        // Log all of the results.
        if (Application.isEditor)
            Log();
    }

    /// <summary>
    /// Determines whether another word can be constructed that includes the impostor letter.
    /// </summary>
    /// <param name="impostor">The impostor letter.</param>
    /// <param name="solution">The current solution.</param>
    /// <returns>True if the impostor character cannot create a valid word.</returns>
    internal static bool IsValidImpostor(char impostor, string solution)
    {
        if (solution.Select(c => ValidAlphabet.IndexOf(c)).Any(i => Math.Abs(i - ValidAlphabet.IndexOf(impostor)) <= 1))
            return false;

        foreach (char potentialImpostor in solution.Distinct())
        {
            if (Flatten(ValidWords).Distinct().Contains(solution.Select(c => c != potentialImpostor).Join("")))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Calculates all possible answers of the module.
    /// </summary>
    /// <param name="solution">The word to reach to.</param>
    /// <param name="index">The offset index that is used for the colors.</param>
    /// <returns>A string array where every element is a valid answer.</returns>
    internal static string[] GetAllAnswers(string solution, int index, ButtonType[] buttons)
    {
        // Initalize list.
        List<string>[] answers = new List<string>[solution.Length];
        for (int i = 0; i < answers.Length; i++)
            answers[i] = new List<string>();

        // For each character.
        for (int i = 0; i < solution.Length; i++)
        {
            // For each color's word.
            foreach (string button in buttons.Select(b => b.ToString()))
            {
                // Would pushing the button be valid?
                if (solution[i] == button[(index + i) % button.Length].ToString().ToLowerInvariant()[0])
                {
                    // Since Blue and Black both share the same first letter, K is used instead for black.
                    string nextAnswer = button[0].ToString();

                    // If this is not the first iteration, we need to add answers based on all the previous answers as well.
                    if (i == 0)
                        answers[i].Add(nextAnswer);
                    else
                        foreach (string answer in answers[i - 1])
                            answers[i].Add(answer + nextAnswer);
                }
            }
        }

        return answers[answers.Length - 1].ToArray();
    }

    /// <summary>
    /// Gets all characters that are valid.
    /// </summary>
    /// <returns>String array of all characters that are valid per index.</returns>
    internal static string[] GetAllChars(string[] colors)
    {
        string[] output = new string[LeastCommonDenominator(colors.Select(c => c.Length).ToArray())];

        for (int i = 0; i < output.Length; i++)
            output[i] = colors.Select(s => s[i % s.Length]).Join("").ToLowerInvariant();

        return output;
    }

    /// <summary>
    /// Gets all words that are valid.
    /// </summary>
    /// <returns>Jagged array of all valid words, indexes correlating with the valid characters array.</returns>
	internal static string[][] GetAllWords(TextAsset file, string[] validChars)
    {
        // This is simply the directory of a large word list of strictly nouns.
        string[] words = file.text.Split('\n');
        string[][] output = new string[ValidChars.Length][];

        // Tests for all offsets in _validChars.
        for (int offset = 0; offset < ValidChars.Length; offset++)
            output[offset] = words.Where(w => IsValidWord(w, offset, validChars)).ToArray();

        return Function.TrimAll(output);
    }

    /// <summary>
    /// Logs all information about the variables.
    /// </summary>
    private static void Log()
    {
        for (int i = 0; i < ValidWords.Length; i++)
            Debug.LogFormat("{0}: {1}.", i, ValidWords[i].Join(", "));

        Debug.LogFormat("Valid alphabet: {0}.", ValidAlphabet.Join(", "));
        Debug.LogFormat("Valid character sequence: {0}.", ValidChars.Join(", "));

        Debug.LogFormat("The shortest words are: {0}.", GetShortest().Join(", "));
        Debug.LogFormat("The longest words are: {0}.", GetLongest().Join(", "));

        Debug.LogFormat("The indexes that don't meet the required {0} length are: {1}", MinAcceptableWordSet, ValidWords.Where(a => a.Length < MinAcceptableWordSet).Select(a => Array.IndexOf(ValidWords, a)).Join(", "));

        Debug.LogFormat("The amount of distinct to total words are: {0}/{1}.", GetCount(distinct: true), GetCount(distinct: false));
        Debug.LogFormat("The words that are completely unique are: {0}.", Flatten(ValidWords).GroupBy(x => x).Where(g => g.Count() == 1).Select(y => y.Key).Join(", "));
    }

    /// <summary>
    /// Tests if the word is valid, both in length, and its characters.
    /// </summary>
    /// <param name="line">The string to test.</param>
    /// <param name="offset">The starting index for _validChars</param>
    /// <returns>True if the word is valid for the ValidWords array.</returns>
	private static bool IsValidWord(string line, int offset, string[] validChars)
    {
        // Ensures lack of whitespace and capitalization.
        line = line.Trim();

        // This requires words to be 3 to 6 letters long.
        if (line.Length < 3 || line.Length > SequenceLength - 2)
			return false;

        // This requires words to contain only the letters provided in the current index + amount of characters before it in _validChars.
		for (int i = 0; i < line.Length; i++)
        {
            if (!validChars[(offset + i) % validChars.Length].Contains(line[i].ToString()))
                return false;
        }
        return true;
	}

    /// <summary>
    /// Finds the least common denominator of all integers in the array.
    /// </summary>
    /// <param name="array">The list of numbers to find the least common denominator with.</param>
    /// <returns>Integer of least common denominator in the whole array.</returns>
    private static long LeastCommonDenominator(int[] array)
    {
        int lcm = 1, divisor = 2;

        while (true)
        {
            bool divisible = false;
            int counter = 0;

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == 0)
                    return 0;
                else if (array[i] < 0)
                    array[i] = array[i] * (-1);

                if (array[i] == 1)
                    counter++;

                if (array[i] % divisor == 0)
                {
                    divisible = true;
                    array[i] = array[i] / divisor;
                }
            }

            if (divisible)
                lcm = lcm * divisor;
            else
                divisor++;
            
            if (counter == array.Length)
                return lcm;
        }
    }

    /// <summary>
    /// Returns all of the shortest words in ValidWords.
    /// </summary>
    /// <returns>The shortest string(s) in ValidWords.</returns>
	private static IEnumerable<string> GetShortest()
    {
        List<string> shortestWord = new List<string>() { ValidWords.PickRandom().PickRandom() };

        for (int i = 0; i < ValidWords.Length; i++)
        {
            for (int j = 0; j < ValidWords[i].Length; j++)
            {
                if (shortestWord[0].Length > ValidWords[i][j].Length)
                    shortestWord = new List<string>() { ValidWords[i][j] };

                else if (shortestWord[0].Length == ValidWords[i][j].Length)
                    shortestWord.Add(ValidWords[i][j]);
            }
        }

        return shortestWord.Distinct();
    }

    /// <summary>
    /// Returns all of the longest words in ValidWords.
    /// </summary>
    /// <returns>The longest string(s) in ValidWords.</returns>
	private static IEnumerable<string> GetLongest()
    {
		List<string> longestWord = new List<string>() { string.Empty };

		for (int i = 0; i < ValidWords.Length; i++)
		{
			for (int j = 0; j < ValidWords[i].Length; j++)
			{
				if (longestWord[0].Length < ValidWords[i][j].Length)
					longestWord = new List<string>() { ValidWords[i][j] };

				else if (longestWord[0].Length == ValidWords[i][j].Length)
					longestWord.Add(ValidWords[i][j]);
			}
		}

		return longestWord.Distinct();
    }

    /// <summary>
    /// Returns the amount of unique words, flattening the ValidWords array.
    /// </summary>
    /// <returns>The amount of unique entries in ValidWords.</returns>
    private static int GetCount(bool distinct)
    {
		return distinct ? Flatten(ValidWords).Distinct().Count() : Flatten(ValidWords).Count();
    }

    private static string[] Flatten(string[][] array)
    {
        return array.SelectMany(a => a).ToArray();
    }

    /// <summary>
    /// Tests if the two strings provided are anagrams of each other.
    /// </summary>
    /// <param name="a">The first string to test.</param>
    /// <param name="b">The second string to test.</param>
    /// <returns>True if both strings are anagrams, otherwise false.</returns>
    private static bool IsAnagram(string a, string b)
    {
        return string.Concat(a.OrderBy(c => c)).Equals(string.Concat(b.OrderBy(c => c)));
    }
}
