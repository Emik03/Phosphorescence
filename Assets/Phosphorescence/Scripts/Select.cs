using PhosphorescenceExtensions;
using System;
using System.Linq;
using UnityEngine;

internal class Select
{
    internal Select(PhosphorescenceScript pho, Init init, Render render)
    {
        _pho = pho;
        _init = init;
        _render = render;
        _animate = new Animate(pho, init, render);
    }

    internal ButtonType[] buttons;

    private readonly Animate _animate;
    private readonly Init _init;
    private readonly PhosphorescenceScript _pho;
    private readonly Render _render;

    internal KMSelectable.OnInteractHandler NumberPress()
    {
        return delegate ()
        {
            PressProductionValue(_pho.Number.transform, 2);

            if (_init.isSolved || _init.isAnimated)
            {
                Function.PlaySound("invalidButton", _pho);
                return false;
            }

            if (!_init.isCountingDown)
                _pho.StartCoroutine(_animate.Run());
            else if (!_init.isAnimated)
                if (_init.isInSubmission)
                    _pho.StartCoroutine(_animate.Submit());
                else
                    IntoSubmit();
            return false;
        };
    }

    internal KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            PressProductionValue(_pho.Number.transform, 2);

            if (_init.isSolved || !_init.isCountingDown || !_init.isInSubmission || _init.submission.Length >= 6)
            {
                Function.PlaySound("invalidButton", _pho);
                return false;
            }

            string currentSubmit = buttons[btn].ToString();
            int currentIndex = _init.index + _init.submission.Length;
            _init.submission += currentSubmit[currentIndex % currentSubmit.Length].ToString().ToLowerInvariant();
            Function.PlaySound("submit" + _init.submission.Length, _pho);
            _pho.StartCoroutine(_animate.PressButton(_pho.ButtonRenderers[btn].transform));

            return false;
        };
    }

    internal KMSelectable.OnInteractHandler ColorPanelPress()
    {
        return delegate ()
        {
            PressProductionValue(_pho.Color.transform, 3);

            if (_init.isSolved || _init.isAnimated || _init.isInSubmission)
            {
                Function.PlaySound("invalidButton", _pho);
                return false;
            }

            Function.PlaySound("screenPress", _pho);
            _pho.StartCoroutine(_render.UpdateCubes());
            return false;
        };
    }

    internal Action ColorPanelRelease()
    {
        return delegate ()
        {
            PressProductionValue(_pho.Color.transform, 1);

            if (_init.isSolved || _init.isAnimated || _init.isInSubmission)
            {
                Function.PlaySound("invalidButton", _pho);
                return;
            }

            Function.PlaySound("screenRelease", _pho);
            _init.isTouched = false;

            for (int i = 0; i < _pho.Tiles.Length; i++)
                _pho.Tiles[i].material.color = Color.black;
        };
    }

    private void IntoSubmit()
    {
        _init.submission = string.Empty;
        _init.isInSubmission = true;

        Function.PlaySound("startSubmit", _pho);

        buttons = Enum.GetValues(typeof(ButtonType)).Cast<ButtonType>().ToArray().Shuffle();

        for (int i = 0; i < _pho.ButtonRenderers.Length; i++)
            _pho.ButtonRenderers[i].material.color = Function.GetColor(buttons[i]);

        _pho.StartCoroutine(_animate.IntoSubmit());
    }

    private void PressProductionValue(Transform transform, int i)
    {
        _pho.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        _pho.Number.AddInteractionPunch(i);
    }
}
