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
        animate = new Animate(pho, init, render);
    }

    internal readonly Animate animate;

    internal ButtonType[] buttons;

    private readonly Init _init;
    private readonly PhosphorescenceScript _pho;
    private readonly Render _render;

    internal KMSelectable.OnInteractHandler NumberPress()
    {
        return delegate ()
        {
            PressFeedback(_pho.Number.transform);

            if (_init.isSolved || _init.isAnimated)
            {
                Function.PlaySound("invalidButton", _pho);
                return false;
            }

            if (!_init.isCountingDown)
                _pho.StartCoroutine(animate.Run());
            else if (!_init.isAnimated)
                if (_init.isInSubmission)
                    _pho.StartCoroutine(animate.Submit());
                else
                    IntoSubmit();
            return false;
        };
    }

    internal KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            PressFeedback(_pho.Number.transform);

            if (_init.isSolved || !_init.isCountingDown || !_init.isInSubmission || _init.submission.Length >= 6)
            {
                Function.PlaySound("invalidButton", _pho);
                return false;
            }

            string currentSubmit = buttons[btn].ToString();
            int currentIndex = _init.index + _init.submission.Length;
            _init.submission += currentSubmit[currentIndex % currentSubmit.Length].ToString().ToLowerInvariant();
            Function.PlaySound("submit" + _init.submission.Length, _pho);
            _pho.StartCoroutine(animate.PressButton(_pho.ButtonRenderers[btn].transform));

            return false;
        };
    }

    internal KMSelectable.OnInteractHandler MarkerPress(int btn)
    {
        return delegate ()
        {
            PressFeedback(_pho.Number.transform);

            if (_init.isSolved || _init.isAnimated || !_init.isCountingDown || _init.isInSubmission)
            {
                Function.PlaySound("invalidButton", _pho);
                return false;
            }

            _pho.MarkerRenderers[btn].transform.localPosition = new Vector3(_pho.MarkerRenderers[btn].transform.localPosition.x, _pho.MarkerRenderers[btn].transform.localPosition.y * -1, _pho.MarkerRenderers[btn].transform.localPosition.z);
            return false;
        };
    }

    internal KMSelectable.OnInteractHandler ColorPress()
    {
        return delegate ()
        {
            PressFeedback(_pho.Color.transform, 2);

            if (_init.isSolved || _init.isAnimated || _init.isInSubmission)
            {
                Function.PlaySound("invalidButton", _pho);
                return true;
            }

            Function.PlaySound("screenPress", _pho);
            ResetMarkers();
            _pho.StartCoroutine(_render.UpdateCubes());
            return true;
        };
    }

    private void ResetMarkers()
    {
        foreach (var renderer in _pho.MarkerRenderers)
            renderer.transform.localPosition = new Vector3(renderer.transform.localPosition.x, -0.5f, renderer.transform.localPosition.z);
    }

    internal Action ColorRelease()
    {
        return delegate () { StopSequence(); };
    }

    internal KMSelectable.OnCancelHandler ColorCancel()
    {
        return delegate () { StopSequence(); return true; };
    }

    internal void StopSequence()
    {
        PressFeedback(_pho.Color.transform, 0.5f);

        if (_init.isSolved || _init.isAnimated || _init.isInSubmission)
        {
            Function.PlaySound("invalidButton", _pho);
            return;
        }

        Function.PlaySound("screenRelease", _pho);
        _init.isTouched = false;

        for (int i = 0; i < _pho.Tiles.Length; i++)
            _pho.Tiles[i].material.color = Color.black;
    }

    private void IntoSubmit()
    {
        _init.submission = string.Empty;
        _init.isInSubmission = true;

        Function.PlaySound("startSubmit", _pho);

        buttons = Enum.GetValues(typeof(ButtonType)).Cast<ButtonType>().ToArray().Shuffle();

        for (int i = 0; i < _pho.ButtonRenderers.Length; i++)
            _pho.ButtonRenderers[i].material.color = Function.GetColor(buttons[i]);

        _pho.StartCoroutine(animate.IntoSubmit());
    }

    private void PressFeedback(Transform transform, float i = 1)
    {
        _pho.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        _pho.Number.AddInteractionPunch(i);
    }
}
