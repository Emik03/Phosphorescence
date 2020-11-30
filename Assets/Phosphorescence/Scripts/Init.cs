using PhosphorescenceExtensions;
using System;
using System.Collections;
using UnityEngine;
using Rnd = UnityEngine.Random;

internal class Init
{
    internal Init(PhosphorescenceScript pho)
    {
        this.pho = pho;
        render = new Render(pho, this);
        select = new Select(pho, this, render);
    }

    internal readonly PhosphorescenceScript pho;
    internal readonly Select select;
    internal readonly Render render;

    internal static bool disableMarkers;
    internal bool isSolved, isCountingDown, isInSubmission, isTouched, isAnimated;
    internal static int moduleIdCounter, streamDelay;
    internal int moduleId, index;
    internal string solution, submission;

    internal void Activate()
    {
        ModSettingsJSON.Get(pho, out disableMarkers, out streamDelay);
        render.colorblind = pho.Colorblind.ColorblindModeActive;

        moduleId = ++moduleIdCounter;

        pho.Info.OnBombSolved += delegate () { if (moduleId == moduleIdCounter) Function.PlaySound("voice_bombdisarmed", pho); };
        pho.Info.OnBombExploded += delegate () { if (moduleId == moduleIdCounter) Function.PlaySound("voice_gameover", pho); };

        pho.Number.OnInteract += select.NumberPress();
        pho.Color.OnInteract += select.ColorPress();
        pho.Color.OnCancel += select.ColorCancel();

        Function.OnInteractArray(pho.Buttons, select.ButtonPress);

        if (!disableMarkers)
        {
            Function.OnInteractArray(pho.Markers, select.MarkerPress);
            return;
        }

        pho.Color.OnInteractEnded += select.ColorRelease();
        pho.Color.Children = new KMSelectable[0];

        foreach (var highlight in pho.MarkerHighlightables)
            highlight.transform.localPosition = new Vector3(highlight.transform.localPosition.x, -0.5f, highlight.transform.localPosition.z);
    }


    internal IEnumerator BufferStrike()
    {
        yield return new WaitWhile(() => isAnimated);

        if (isInSubmission)
            yield return select.animate.Submit(); // The method causes another instance of Strike() to run, or Solve() if the submission is correct.

        else
            yield return Strike();
    }

    private IEnumerator Strike()
    {
        isAnimated = true;

        Debug.LogFormat("[Phosphorescence #{0}]: Submission \"{1}\" did not match the expected \"{2}\"!", moduleId, submission, solution);
        Function.PlaySound("strike", pho);

        render.UpdateDisplay(0);
        isCountingDown = false;
        pho.Module.HandleStrike();

        for (int i = 0; i <= 25; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Function.GetColor(Rnd.Range(0, 25) >= i ? ButtonType.Black : ButtonType.Red);

            yield return new WaitForSecondsRealtime(0.02f);
        }

        for (int i = 0; i <= 25; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Function.GetColor(Rnd.Range(0, 25) >= i ? ButtonType.Red : ButtonType.Black);

            yield return new WaitForSecondsRealtime(0.02f);
        }

        isAnimated = false;
    }

    internal IEnumerator Solve()
    {
        isSolved = true;
        Debug.LogFormat("[Phosphorescence #{0}]: The submisssion was correct, that is all.", moduleId);
        
        Function.PlaySound("success", pho);

        int[] displayStates = new int[49];

        foreach (var text in pho.Text)
            text.color = new Color32(98, 196, 98, 255);

        for (int i = 0; i < 8; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Function.GetColor((ButtonType)i);

            yield return new WaitForSeconds(0.5f);
        }

        Function.PlaySound("voice_challengecomplete", pho);
        pho.Module.HandlePass();
        yield return select.animate.PostSolve(pho, displayStates);
    }
}
