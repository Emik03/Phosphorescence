using PhosphorescenceExtensions;
using System.Collections;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// Initalizer class for Phosphorescence. Create a new instance to initiate the module.
/// </summary>
internal class Init
{
    internal Init(PhosphorescenceScript pho)
    {
        this.pho = pho;
        render = new Render(pho, this);
        souvenir = new Souvenir(moduleId = ++moduleIdCounter);
        select = new Select(pho, this, render);
    }

    internal readonly PhosphorescenceScript pho;
    internal readonly Select select;
    internal readonly Souvenir souvenir;
    internal readonly Render render;

    /// <summary>
    /// If true, disables incompatible markers and initalizes bottom display's event as OnInteractEnded instead of OnCancel.
    /// </summary>
    internal static bool vrMode;

    /// <summary>
    /// Records critical states of the module.
    /// </summary>
    internal bool isSolved, isCountingDown, isInSubmission, isSelected, isAnimated;

    internal static int moduleIdCounter, streamDelay;
    internal int moduleId, index;
    internal string solution, submission;

    internal ButtonType[] buttonPresses;

    /// <summary>
    /// The startup method for Init, which gets the module prepared to be interacted with.
    /// </summary>
    internal void Activate()
    {
        ModSettingsJSON.Get(pho, out vrMode, out streamDelay);
        Colorblind();

        // Plays voice lines only if it is the last one initiated. Otherwise, multiple sounds could stack up.
        pho.Info.OnBombSolved += delegate () { if (moduleId == moduleIdCounter) Function.PlaySound("voice_bombdisarmed", pho); };
        pho.Info.OnBombExploded += delegate () { if (moduleId == moduleIdCounter) Function.PlaySound("voice_gameover", pho); };

        pho.Number.OnInteract += select.NumberPress();
        pho.Color.OnInteract += select.ColorPress();

        Function.OnInteractArray(pho.Buttons, select.ButtonPress);

        // Initalize markers, and use OnCancel.
        if (!vrMode)
        {
            pho.Color.OnCancel += select.ColorCancel();
            Function.OnInteractArray(pho.Markers, select.MarkerPress);
        }

        // Otherwise, remove markers and use OnInteractEnded.
        else
        {
            pho.Color.OnInteractEnded += select.ColorRelease();

            foreach (var highlight in pho.MarkerHighlightables)
                highlight.transform.localPosition = new Vector3(highlight.transform.localPosition.x, -0.5f, highlight.transform.localPosition.z);
        }
    }

    /// <summary>
    /// Sets colorblind to be whether or not it is enabled.
    /// </summary>
    private void Colorblind()
    {
        render.colorblind = pho.Colorblind.ColorblindModeActive;

        foreach (var tile in pho.Tiles)
            tile.material.mainTexture = render.colorblind ? pho.ColorblindTexture : null;
    }

    /// <summary>
    /// This method is called when the module is about to strike, but needs to wait until the animations are finished first.
    /// The small delay does make the module a bit more forgiving, as it will revalidate the answer before properly striking.
    /// </summary>
    internal IEnumerator BufferStrike()
    {
        yield return new WaitWhile(() => isAnimated);

        // The method causes another instance of Strike() to run, or solve if the submission is correct.
        if (isInSubmission)
            yield return select.animate.ExitSubmit(); 

        else
            yield return Strike();
    }

    /// <summary>
    /// Strikes the module, with a sparratic screen animation.
    /// </summary>
    private IEnumerator Strike()
    {
        isAnimated = true;

        Debug.LogFormat("[Phosphorescence #{0}]: Submission \"{1}\" did not match the expected \"{2}\"!", moduleId, submission, solution);
        Function.PlaySound("strike", pho);

        // Disable screen.
        render.UpdateDisplay(0);
        isCountingDown = false;

        pho.Module.HandleStrike();

        // Increase in amount of reds.
        for (int i = 0; i <= 25; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Function.GetColor(Rnd.Range(0, 25) >= i ? ButtonType.Black : ButtonType.Red);

            yield return new WaitForSecondsRealtime(0.02f);
        }

        // Decrease in amount of reds.
        for (int i = 0; i <= 25; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Function.GetColor(Rnd.Range(0, 25) >= i ? ButtonType.Red : ButtonType.Black);

            yield return new WaitForSecondsRealtime(0.02f);
        }

        isAnimated = false;
    }

    /// <summary>
    /// Gets called when module is about to solve, plays solve animation.
    /// </summary>
    internal IEnumerator Solve()
    {
        if (pho.Info.GetSolvableModuleNames().Contains("Souvenir"))
            souvenir.Set(index, buttonPresses);

        isSolved = true;
        Debug.LogFormat("[Phosphorescence #{0}]: The submisssion was correct, that is all.", moduleId);
        Function.PlaySound("success", pho);

        // Turns off colorblind, since it doesn't matter at this stage.
        foreach (var tile in pho.Tiles)
            tile.material.mainTexture = null;

        // Keeps track of the current states of the screen.
        // This needs to be a 1-dimensional array for it to easily align with the Renderer array.
        int[] displayStates = new int[49];

        // Sets all text to be a light shade of green.
        foreach (var text in pho.ScreenText)
            text.color = new Color32(98, 196, 98, 255);

        // Flashes all colors.
        for (int i = 0; i < 8; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Function.GetColor((ButtonType)i);

            yield return new WaitForSeconds(0.5f);
        }

        // Solves the module.
        Function.PlaySound("voice_challengecomplete", pho);
        pho.Module.HandlePass();
        yield return select.animate.PostSolve(pho, displayStates);
    }
}
