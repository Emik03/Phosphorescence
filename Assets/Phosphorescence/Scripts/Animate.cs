using System.Collections;
using UnityEngine;
using Rnd = UnityEngine.Random;

internal class Animate
{
    internal Animate(PhosphorescenceScript pho, Init init, Render render)
    {
        _pho = pho;
        _init = init;
        _render = render;
    }

    private readonly PhosphorescenceScript _pho;
    private readonly Init _init;
    private readonly Render _render;

    internal IEnumerator Run()
    {
        _init.isAnimated = true;
        _init.index = 0; // This makes the display darker, since it always returns 0 in binary.
        Function.PlaySound("start", _pho);

        float solved = _pho.Info.GetSolvedModuleNames().Count,
              solvable = _pho.Info.GetSolvableModuleNames().Count;

        float additionalTime = solved + 1 == solvable ? 1 : solved / solvable;

        Render.amountOfTime = 200 + (int)(200 * additionalTime);

        for (int i = 0; i < Render.amountOfTime; i += (int)Mathf.Ceil((float)Render.amountOfTime / 100))
        {
            _render.UpdateDisplay(i);
            yield return new WaitForFixedUpdate();
        }

        do _init.index = Rnd.Range(0, 420);
        while (Words.ValidWords[_init.index].Length < 3);

        _init.solution = Words.ValidWords[_init.index].PickRandom();
        Debug.LogFormat("[Phosphorescence #{0}]: The expected answer is {1}, deriving from the starting offset {2}.", _init.moduleId, _init.solution, _init.index);
        _pho.StartCoroutine(_render.Countdown());

        _init.isCountingDown = true;
        _init.isAnimated = false;
    }

    internal IEnumerator PressButton(Transform transform)
    {
        float k = 1;

        while (k > 0)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, -2 * Function.ElasticIn(k), transform.localPosition.z);
            k -= 0.0078125f;
            yield return new WaitForSecondsRealtime(0.01f);
        }
    }

    internal IEnumerator IntoSubmit()
    {
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
            _pho.StartCoroutine(_init.Strike());

        _init.isAnimated = false;
        _init.isInSubmission = false;
        _init.isTouched = false;
    }
}
