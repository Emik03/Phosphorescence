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
        animate = new Animate(pho, init, this, render);
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

            if (_init.isSolved || !_init.isCountingDown || !_init.isInSubmission)
            {
                Function.PlaySound("invalidButton", _pho);
                return false;
            }

            else if (_init.submission.Length < 6)
            {
                string currentSubmit = buttons[btn].ToString();
                int currentIndex = _init.index + _init.submission.Length;

                _init.submission += currentSubmit[currentIndex % currentSubmit.Length].ToString().ToLowerInvariant();
                _pho.StartCoroutine(animate.PressButton(_pho.ButtonRenderers[btn].transform));

                Function.PlaySound("submit" + _init.submission.Length, _pho);
                return false;
            }

            _pho.StartCoroutine(animate.ResetButtons());
            return false;
        };
    }

    internal KMSelectable.OnInteractHandler MarkerPress(int btn)
    {
        return delegate ()
        {
            PressFeedback(_pho.Number.transform, 0.1f);

            if (_init.isSolved || _init.isAnimated || !_init.isCountingDown || _init.isInSubmission)
            {
                Function.PlaySound("invalidButton", _pho);
                return false;
            }

            _pho.MarkerRenderers[btn].transform.localPosition = new Vector3(_pho.MarkerRenderers[btn].transform.localPosition.x, _pho.MarkerRenderers[btn].transform.localPosition.y * -1, _pho.MarkerRenderers[btn].transform.localPosition.z);
            Function.PlaySound(_pho.MarkerRenderers[btn].transform.localPosition.y > 0 ? "markerOn" : "markerOff", _pho);
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
                return !Init.vrMode;
            }

            Function.PlaySound("screenPress", _pho);
            ResetMarkers();
            _pho.StartCoroutine(_render.UpdateCubes());
            return !Init.vrMode;
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
        _init.isSelected = false;

        for (int i = 0; i < _pho.Tiles.Length; i++)
            _pho.Tiles[i].material.color = Color.black;
    }

    internal void ShuffleButtons()
    {
        buttons = Enum.GetValues(typeof(ButtonType)).Cast<ButtonType>().ToArray().Shuffle();

        if (_render.colorblind)
            for (int i = 0; i < _pho.ButtonText.Length; i++)
                _pho.ButtonText[i].text = buttons[i] == ButtonType.Black ? "K" : buttons[i].ToString()[0].ToString();

        for (int i = 0; i < _pho.ButtonRenderers.Length; i++)
            _pho.ButtonRenderers[i].material.color = Function.GetColor(buttons[i]);
    }

    private void IntoSubmit()
    {
        _init.submission = string.Empty;
        _init.isInSubmission = true;

        Function.PlaySound("startSubmit", _pho);
        ShuffleButtons();

        _pho.StartCoroutine(animate.IntoSubmit());
    }

    private void PressFeedback(Transform transform, float i = 1)
    {
        _pho.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        _pho.Number.AddInteractionPunch(i);
    }
}
