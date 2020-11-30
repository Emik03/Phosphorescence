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

    internal static int amountOfTime;
    internal int currentIndex;
    internal float burn;

    private readonly PhosphorescenceScript _pho;
    private readonly Init _init;

    private Color32[] _colors = new Color32[49];
    private static readonly Color32[] _allColors = new Color32[] { Color.black, Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow, Color.white };

    private int _time;
    private const float _burnSpeed = 0.00001525878f;
    private string _letters = string.Empty;
    private const string _alphabet = "abcdeghiklmnortuwy";

    internal IEnumerator Countdown()
    {
        _init.isCountingDown = true;
        Function.PlaySound("voice_go", _pho);

        for (_time = amountOfTime; _time >= 1; _time--)
        {
            if (!_init.isCountingDown)
                yield break;

            Function.CountSound(_time, _pho, _time == amountOfTime);
            UpdateDisplay(_time);

            yield return new WaitForSecondsRealtime(1);
        }

        _init.Strike();
    }

    internal IEnumerator UpdateCubes()
    {
        if (!_init.isCountingDown)
            yield break;

        _init.isTouched = true;

        if (++currentIndex >= _letters.Length)
            NewSequence();

        _colors = SetCubeColors();
        burn = 0;

        while (_init.isTouched)
        {
            DisplayCubes();
            yield return new WaitForFixedUpdate();
        }
    }

    internal void UpdateDisplay(int t)
    {
        if (t <= 0)
        {
            _pho.Text[0].text = _pho.Text[1].text = _pho.Text[2].text = _pho.Text[3].text = _pho.Text[4].text = string.Empty;
            return;
        }

        _pho.Text[0].text = (t / 60).ToString();
        _pho.Text[1].text = (t % 60 / 10).ToString();
        _pho.Text[2].text = (t % 10).ToString();
        _pho.Text[3].text = _pho.Text[4].text = ".";

        byte strain = (byte)((float)t / amountOfTime * 98);
        int max = (int)Math.Pow(2, int.Parse(_pho.Text[2].text));

        foreach (var text in _pho.Text)
            text.color = _init.index / max % 2 == 0 ? new Color32(98, strain, strain, 255) : new Color32(196, (byte)(strain * 2), (byte)(strain * 2), 255);
    }

    private void NewSequence()
    {
        Function.PlaySound("reshuffle", _pho);

        currentIndex = -1;
        _letters = _init.solution;
        string impostor = _alphabet.Where(c => !_init.solution.Contains(c)).PickRandom().ToString();

        while (_letters.Length < Words.MaxLength)
            _letters = _letters.Insert(Rnd.Range(0, _letters.Length), impostor);
        Debug.LogFormat("[Phosphorescence #{0}]: Reshuffled! The sequence shown is {1}.", _init.moduleId, _letters);
    }

    private void DisplayCubes()
    {
        for (int i = 0; i < _pho.Tiles.Length; i++)
        {
            burn = Mathf.Min(burn + (_burnSpeed * (1 - (_time / 400))), 1);
            _pho.Tiles[i].material.color = new Color32((byte)((_colors[i].r * (1 - burn)) + (128 * burn)), (byte)((_colors[i].g * (1 - burn)) + (128 * burn)), (byte)((_colors[i].b * (1 - burn)) + (128 * burn)), 255);
        }
    }

    private Color32[] SetCubeColors()
    {
    restart:
        if (currentIndex == -1)
            return Enumerable.Repeat(new Color32(128, 128, 128, 255), 49).ToArray();

        bool[] booleans = Function.RandomBools(49);
        int lCount = Function.GetLCount(booleans);

        // Forces lCount to match up with the alphabet.
        if (lCount != 10 + _alphabet.IndexOf(_letters[currentIndex]))
            goto restart;
        _colors = AssignCubeColors(booleans);

        //Debug.LogFormat("l: {0}", lCount);

        return _colors;
    }

    private Color32[] AssignCubeColors(bool[] booleans)
    {
        Color32 colorA = Color.black, colorB;
        do colorB = _allColors.PickRandom();
        while (Array.IndexOf(_allColors, colorA) == Array.IndexOf(_allColors, colorB));

        Color32[] colors = new Color32[49];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = booleans[i] ? colorA : colorB;

        return colors;
    }
}
