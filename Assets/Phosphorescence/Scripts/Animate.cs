using PhosphorescenceExtensions;
using System;
using System.Collections;
using UnityEngine;
using Rnd = UnityEngine.Random;

internal class Animate
{
    internal Animate(PhosphorescenceScript pho, Init init, Select select, Render render)
    {
        _pho = pho;
        _init = init;
        _select = select;
        _render = render;
    }

    private readonly PhosphorescenceScript _pho;
    private readonly Init _init;
    private readonly Select _select;
    private readonly Render _render;

    private bool isPushingButton;

    internal IEnumerator Run()
    {
        _init.isAnimated = true;
        _init.index = 0; // This makes the display darker, since it always returns 0 in binary.
        Function.PlaySound("start", _pho);

        float solved = _pho.Info.GetSolvedModuleNames().Count,
              solvable = _pho.Info.GetSolvableModuleNames().Count,
              deltaSolved = solved + 1 == solvable ? 1 : solved / solvable;

        int currentTime = Init.streamDelay + 300 + (int)(deltaSolved * 300);

        Render.currentTime = Mathf.Max(Mathf.Min(currentTime, 5999), 10);

        for (int i = 0; i < Render.currentTime; i += (int)Mathf.Ceil((float)Render.currentTime / 100))
        {
            _render.UpdateDisplay(i);
            yield return new WaitForFixedUpdate();
        }

        do _init.index = Rnd.Range(0, 420);
        while (Words.ValidWords[_init.index].Length < 3);

        _init.solution = Words.ValidWords[_init.index].PickRandom();
        Debug.LogFormat("[Phosphorescence #{0}]: The expected answer is {1}, deriving from the starting offset {2}.", _init.moduleId, _init.solution, _init.index);
        Debug.LogFormat("[Phosphorescence #{0}]: All possible answers are: {1}.", _init.moduleId, Function.GetAllAnswers(_init.solution, _init.index).Join(", "));
        _pho.StartCoroutine(_render.Countdown());

        _init.isCountingDown = true;
        _init.isAnimated = false;
    }

    internal IEnumerator PressButton(Transform transform)
    {
        _init.isAnimated = true;
        isPushingButton = true;
        yield return new WaitForSecondsRealtime(0.1f);
        isPushingButton = false;
        _init.isAnimated = false;

        float k = 1;

        while (k > 0 && !isPushingButton)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, -2 * Function.ElasticIn(k), transform.localPosition.z);
            k -= 0.0078125f;
            yield return new WaitForSecondsRealtime(0.01f);
        }

        transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
    }

    internal IEnumerator ResetButtons()
    {
        _init.isAnimated = true;
        isPushingButton = true;
        yield return new WaitForSecondsRealtime(0.1f);
        isPushingButton = false;
        _init.isAnimated = false;

        _init.submission = string.Empty;
        _select.ShuffleButtons();
        Function.PlaySound("shuffleButtons", _pho);
        
        float k = 1;

        while (k > 0 && !isPushingButton)
        {
            for (int i = 0; i < _pho.ButtonRenderers.Length; i++)
            {
                _pho.ButtonRenderers[i].transform.localPosition = new Vector3(_pho.ButtonRenderers[i].transform.localPosition.x, -2 * Function.ElasticIn(k), _pho.ButtonRenderers[i].transform.localPosition.z);
                Function.SetIntertwinedColor(renderer: _pho.ButtonRenderers[i],
                                             colorA: Function.GetColor(_select.buttons[i]),
                                             colorB: Color.white,
                                             f: Math.Max((k - 0.75f) * 4, 0));
            }

            k -= 0.0078125f;
            yield return new WaitForSecondsRealtime(0.01f);
        }

        for (int i = 0; i < _pho.ButtonRenderers.Length; i++)
        {
            _pho.ButtonRenderers[i].transform.localPosition = new Vector3(_pho.ButtonRenderers[i].transform.localPosition.x, 0, _pho.ButtonRenderers[i].transform.localPosition.z);
            _pho.ButtonRenderers[i].material.color = Function.GetColor(_select.buttons[i]);
        }

        yield return FadeButtons();
    }

    internal IEnumerator FadeButtons()
    {
        yield return new WaitForFixedUpdate();
        float k = 0;

        while (k <= 1 && !isPushingButton)
        {
            for (int i = 0; i < _pho.ButtonRenderers.Length; i++)
                Function.SetIntertwinedColor(renderer: _pho.ButtonRenderers[i],
                                             colorA: Function.GetColor(_select.buttons[i]),
                                             colorB: Color.black,
                                             f: k);

            k += 0.001953125f;
            yield return new WaitForSecondsRealtime(0.01f);
        }

        for (int i = 0; i < _pho.ButtonRenderers.Length; i++)
            _pho.ButtonRenderers[i].material.color = Color.black;
    }

    internal IEnumerator IntoSubmit()
    {
        _pho.StartCoroutine(FadeButtons());
        _init.isAnimated = true;

        float k = 1;
        while (k > 0)
        {
            _pho.Screen.transform.localPosition = new Vector3(-0.015f, (0.02f * Function.ElasticOut(k)) - 0.015f, -0.016f);
            k -= 0.015625f;
            yield return new WaitForSecondsRealtime(0.01f);
        }

        k = 0;
        while (k <= 1)
        {
            _pho.Panel.transform.localPosition = new Vector3(0, (0.035f * Function.ElasticOut(k)) - 0.025f, 0);
            k += 0.00390625f;
            yield return new WaitForSecondsRealtime(0.01f);
        }

        _init.isAnimated = false;
    }

    internal IEnumerator Submit()
    {
        _init.isAnimated = true;
        _init.isCountingDown = false;

        Function.PlaySound("endSubmit", _pho);

        float k = 0;
        while (k <= 1)
        {
            _pho.Panel.transform.localPosition = new Vector3(0, (0.035f * Function.ElasticOut(1 - k)) - 0.025f, 0);
            k += 0.00390625f;
            yield return new WaitForSecondsRealtime(0.01f);
        }

        k = 1;
        while (k > 0)
        {
            _pho.Screen.transform.localPosition = new Vector3(-0.015f, (0.02f * Function.ElasticOut(1 - k)) - 0.015f, -0.016f);
            k -= 0.015625f;
            yield return new WaitForSecondsRealtime(0.01f);
        }

        if (_init.submission == _init.solution)
            _pho.StartCoroutine(_init.Solve());
        else
            _pho.StartCoroutine(_init.BufferStrike());

        _init.isAnimated = false;
        _init.isInSubmission = false;
        _init.isSelected = false;
    }

    internal IEnumerator PostSolve(PhosphorescenceScript pho, int[] displayStates)
    {
        while (true)
        {
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 20; j++)
                {
                    for (int k = 0; k < displayStates.Length; k++)
                    {
                        if ((i % 4 == 0 && (k % 7) + (k / 7) == j) ||
                            (i % 4 == 3 && (k % 7) + (7 - (k / 7)) == j) ||
                            (i % 4 == 1 && 7 - (k % 7) + (k / 7) == j) ||
                            (i % 4 == 2 && 7 - (k % 7) + (7 - (k / 7)) == j))
                            displayStates[k] = ++displayStates[k] % 8;
                        pho.Tiles[k].material.color = Function.GetColor((ButtonType)displayStates[k]);
                    }

                    yield return new WaitForSecondsRealtime(0.2f);
                }
        }
    }
}
