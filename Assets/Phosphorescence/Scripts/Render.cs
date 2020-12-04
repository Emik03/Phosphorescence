using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// Handles rendering and displaying of the module.
/// </summary>
internal class Render
{
    internal Render(PhosphorescenceScript pho, Init init)
    {
        _pho = pho;
        _init = init;
    }
    
    internal bool cruelMode;
    internal static int currentTime;
    internal int currentIndex;
    internal float burn;
    internal string letters = string.Empty;

    private readonly PhosphorescenceScript _pho;
    private readonly Init _init;

    private Color32[] _colors = new Color32[49];

    private int _time;
    private const float _burnSpeed = 0.00000381469f;

    /// <summary>
    /// Starts a countdown, and attempts a strike if it reaches the end. Make sure that _time is set before running a coroutine with this method.
    /// </summary>
    internal IEnumerator Countdown()
    {
        _init.isCountingDown = true;
        Function.PlaySound("voice_go", _pho);

        for (_time = currentTime; _time >= 1; _time--)
        {
            if (!_init.isCountingDown)
                yield break;

            Function.CountSound(_time, _pho, _time == currentTime);
            UpdateDisplay(_time);

            yield return new WaitForSecondsRealtime(1);
        }

        UpdateDisplay(0);
        _pho.StartCoroutine(_init.BufferStrike());
    }

    /// <summary>
    /// This should be called whenever the bottom screen is pressed. Handles getting the 7x7 grid to display the correct colors.
    /// </summary>
    internal IEnumerator UpdateCubes()
    {
        if (!_init.isCountingDown)
            yield break;

        _init.isSelected = true;

        // Has the sequence finished? Generate a new one.
        if (++currentIndex >= letters.Length)
            NewSequence();

        _colors = GetCubeColors();
        burn = 0;

        // Render the cubes until the screen is released.
        while (_init.isSelected)
        {
            DisplayCubes();
            yield return new WaitForSecondsRealtime(0.02f);
        }
    }

    /// <summary>
    /// Updates the 7-segment display at the top.
    /// </summary>
    /// <param name="t"></param>
    internal void UpdateDisplay(int t)
    {
        // If 0 or less, it will display as inactive.
        if (t <= 0)
        {
            foreach (var text in _pho.ScreenText)
                text.text = string.Empty;
            return;
        }

        _pho.ScreenText[0].text = (t / 600).ToString(); // X0:00
        _pho.ScreenText[1].text = (t / 60 % 10).ToString(); // 0X:00
        _pho.ScreenText[2].text = (t % 60 / 10).ToString(); // 00:X0
        _pho.ScreenText[3].text = (t % 10).ToString(); // 00:0X
        _pho.ScreenText[4].text = _pho.ScreenText[4].text = "."; // 00X00

        // Redshifts the display as the timer ticks closer to 0.
        byte strain = (byte)((float)t / currentTime * 98);
        int currentPow = (int)Math.Pow(2, int.Parse(_pho.ScreenText[3].text));

        foreach (var text in _pho.ScreenText)
            text.color = _init.index / currentPow % 2 == 0 ? new Color32(98, strain, strain, 255) : new Color32(196, (byte)(strain * 2), (byte)(strain * 2), 255);
    }

    /// <summary>
    /// Generate a new sequence, while still containing the same current answer.
    /// </summary>
    private void NewSequence()
    {
        Function.PlaySound("reshuffle", _pho);

        // Setting this to negative 1 means that the next button press will display 0, or the first element.
        currentIndex = -1;

        // Grab the current solution.
        letters = _init.solution;

        // Find any letter not present in the solution.
        List<char> alphabet = Words.ValidAlphabet.Where(c => !_init.solution.Contains(c)).ToList();
        char impostor;
        do
        {
            impostor = alphabet.PickRandom();
            alphabet.RemoveAt(alphabet.IndexOf(impostor));
        }
        while (!Words.IsValidImpostor(impostor, _init.solution));

        // Randomly append this letter until it reaches the theoretical maximum length.
        while (letters.Length < Words.SequenceLength)
            letters = letters.Insert(Rnd.Range(0, letters.Length), impostor.ToString());

        Debug.LogFormat("[Phosphorescence #{0}]: Reshuffled! The sequence shown is {1}.", _init.moduleId, letters);
    }

    /// <summary>
    /// Updates the 7x7 grid. This method needs to be called constantly if smooth color tweening is required.
    /// </summary>
    private void DisplayCubes()
    {
        for (int i = 0; i < _pho.Tiles.Length; i++)
            Function.SetIntertwinedColor(renderer: _pho.Tiles[i],
                                         colorA: _colors[i],
                                         colorB: Color.gray,
                                         f: burn = Mathf.Min(burn + _burnSpeed + ((1 - (_time / currentTime)) * _burnSpeed), 1));
    }

    /// <summary>
    /// Generates a set of colors that contain the current character represented.
    /// </summary>
    private Color32[] GetCubeColors()
    {
        // If the sequence is shuffling, display all gray.
        if (currentIndex == -1)
            return Enumerable.Repeat(new Color32(128, 128, 128, 255), 49).ToArray();

        // Forces L count to match up with the alphabet.
        bool[] booleans = Function.RandomBools(49);

        int times = 0;
        int goal = 6 + Words.ValidAlphabet.IndexOf(letters[currentIndex]);
        while (Function.GetLCount(booleans) != goal)
        {
            times++;
            bool isLower = Function.GetLCount(booleans) < goal;

            int[] iIndexes = Enumerable.Range(0, 6).ToArray().Shuffle(),
                  jIndexes = Enumerable.Range(0, 6).ToArray().Shuffle();

            for (int i = 0; i < iIndexes.Length; i++)
            {
                for (int j = 0; j < jIndexes.Length; j++)
                {
                    if (isLower ^ Function.IsLPattern(booleans, iIndexes[i], jIndexes[j]))
                    {
                        int iRandom = Rnd.Range(0, 1),
                            jRandom = Rnd.Range(0, 1);
                        Function.InvertBoolean(ref booleans[((iIndexes[i] + iRandom) * 7) + jIndexes[j] + jRandom]);
                        goto check;
                    }
                }
            }

        check:
            goal += 0; // This does nothing. This is just so we can iterate through the loop again.
        }

        // Now that a pattern is formed, convert the boolean array to an array of colors.
        _colors = BoolArrayToColorArray(booleans);
        return _colors;
    }

    /// <summary>
    /// Converts a boolean array to a color array.
    /// </summary>
    /// <param name="booleans">The array to scan with.</param>
    /// <returns>Returns a color array based on the boolean array where false is always black, and true is 1 of the 6 in Red, Green, Blue, Cyan, Magenta, Yellow.</returns>
    private Color32[] BoolArrayToColorArray(bool[] booleans)
    {
        Color32 colorA = Color.black, colorB;

        // Picks any color for colorB as long as it isn't black or white.
        do colorB = Words.Colors.PickRandom();
        while (Array.IndexOf(Words.Colors, colorA) == Array.IndexOf(Words.Colors, colorB) || Array.IndexOf(Words.Colors, colorB) == Words.Colors.Length - 1);

        // Constructs the array.
        Color32[] colors = new Color32[49];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = booleans[i] ? colorA : colorB;

        return colors;
    }
}
