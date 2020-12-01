using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

internal class Render
{
    internal Render(PhosphorescenceScript pho, Init init)
    {
        _pho = pho;
        _init = init;
    }

    internal bool colorblind;
    internal static int currentTime;
    internal int currentIndex;
    internal float burn;
    internal string letters = string.Empty;

    private readonly PhosphorescenceScript _pho;
    private readonly Init _init;

    private Color32[] _colors = new Color32[49];
    private static readonly Color32[] _allColors = new Color32[] { Color.black, Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow, Color.white };

    private int _time;
    private const float _burnSpeed = 0.00000381469f;
    private const string _alphabet = "abcdeghiklmnortuwy";

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

    internal IEnumerator UpdateCubes()
    {
        if (!_init.isCountingDown)
            yield break;

        _init.isSelected = true;

        if (++currentIndex >= letters.Length)
            NewSequence();

        _colors = SetCubeColors();
        burn = 0;

        while (_init.isSelected)
        {
            DisplayCubes();
            yield return new WaitForSecondsRealtime(0.02f);
        }
    }

    internal void UpdateDisplay(int t)
    {
        if (t <= 0)
        {
            foreach (var text in _pho.ScreenText)
                text.text = string.Empty;
            return;
        }

        _pho.ScreenText[0].text = (t / 600).ToString();
        _pho.ScreenText[1].text = (t / 60 % 10).ToString();
        _pho.ScreenText[2].text = (t % 60 / 10).ToString();
        _pho.ScreenText[3].text = (t % 10).ToString();
        _pho.ScreenText[4].text = _pho.ScreenText[4].text = ".";

        byte strain = (byte)((float)t / currentTime * 98);
        int currentPow = (int)Math.Pow(2, int.Parse(_pho.ScreenText[3].text));

        foreach (var text in _pho.ScreenText)
            text.color = _init.index / currentPow % 2 == 0 ? new Color32(98, strain, strain, 255) : new Color32(196, (byte)(strain * 2), (byte)(strain * 2), 255);
    }

    private void NewSequence()
    {
        Function.PlaySound("reshuffle", _pho);

        currentIndex = -1;
        letters = _init.solution;
        string impostor = _alphabet.Where(c => !_init.solution.Contains(c)).PickRandom().ToString();

        while (letters.Length < Words.MaxLength)
            letters = letters.Insert(Rnd.Range(0, letters.Length), impostor);
        Debug.LogFormat("[Phosphorescence #{0}]: Reshuffled! The sequence shown is {1}.", _init.moduleId, letters);
    }

    private void DisplayCubes()
    {
        for (int i = 0; i < _pho.Tiles.Length; i++)
            Function.SetIntertwinedColor(renderer: _pho.Tiles[i],
                                         colorA: _colors[i],
                                         colorB: Color.gray,
                                         f: burn = Mathf.Min(burn + _burnSpeed + ((1 - (_time / currentTime)) * _burnSpeed), 1));
    }

    private Color32[] SetCubeColors()
    {
        if (currentIndex == -1)
            return Enumerable.Repeat(new Color32(128, 128, 128, 255), 49).ToArray();

        bool[] booleans;
        do booleans = Function.RandomBools(49); // Forces L count to match up with the alphabet.
        while (Function.GetLCount(booleans) != 10 + _alphabet.IndexOf(letters[currentIndex]));

        Debug.Log(Function.GetLCount(booleans));
        _colors = AssignCubeColors(booleans);
        return _colors;
    }

    private Color32[] AssignCubeColors(bool[] booleans)
    {
        Color32 colorA = Color.black, colorB;
        do colorB = _allColors.PickRandom();
        while (Array.IndexOf(_allColors, colorA) == Array.IndexOf(_allColors, colorB) || Array.IndexOf(_allColors, colorB) == _allColors.Length - 1);

        Color32[] colors = new Color32[49];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = booleans[i] ? colorA : colorB;

        return colors;
    }
}
