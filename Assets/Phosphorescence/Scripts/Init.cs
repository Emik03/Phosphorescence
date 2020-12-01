using PhosphorescenceExtensions;
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

    internal static bool vrMode;
    internal bool isSolved, isCountingDown, isInSubmission, isSelected, isAnimated;
    internal static int moduleIdCounter, streamDelay;
    internal int moduleId, index;
    internal string solution, submission;

    internal void Activate()
    {
        ModSettingsJSON.Get(pho, out vrMode, out streamDelay);
        Colorblind();

        moduleId = ++moduleIdCounter;

        pho.Info.OnBombSolved += delegate () { if (moduleId == moduleIdCounter) Function.PlaySound("voice_bombdisarmed", pho); };
        pho.Info.OnBombExploded += delegate () { if (moduleId == moduleIdCounter) Function.PlaySound("voice_gameover", pho); };

        pho.Number.OnInteract += select.NumberPress();
        pho.Color.OnInteract += select.ColorPress();

        Function.OnInteractArray(pho.Buttons, select.ButtonPress);

        if (!vrMode)
        {
            pho.Color.OnCancel += select.ColorCancel();
            Function.OnInteractArray(pho.Markers, select.MarkerPress);
            return;
        }

        pho.Color.OnInteractEnded += select.ColorRelease();

        foreach (var highlight in pho.MarkerHighlightables)
            highlight.transform.localPosition = new Vector3(highlight.transform.localPosition.x, -0.5f, highlight.transform.localPosition.z);
    }


    internal IEnumerator BufferStrike()
    {
        yield return new WaitWhile(() => isAnimated);

        if (isInSubmission)
            yield return select.animate.Submit(); // The method causes another instance of Strike() to run, or solve if the submission is correct.

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

        // Turns off colorblind, since it doesn't matter at this stage.
        foreach (var tile in pho.Tiles)
            tile.material.mainTexture = null;

        int[] displayStates = new int[49];

        foreach (var text in pho.ScreenText)
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

    private void Colorblind()
    {
        render.colorblind = pho.Colorblind.ColorblindModeActive;

        if (!render.colorblind)
            foreach (var tile in pho.Tiles)
                tile.material.mainTexture = null;
    }
}
