using PhosphorescenceExtensions;
using System;
using System.Collections;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// Initalizer class for Phosphorescence. Create a new instance of this class to initiate the module.
/// </summary>
internal class Init
{
    internal Init(PhosphorescenceScript pho)
    {
        this.pho = pho;
        render = new Render(pho, this);
        select = new Select(pho, this, render);

        Activate();
    }

    internal readonly PhosphorescenceScript pho;
    internal readonly Select select;
    internal readonly Render render;

    /// <summary>
    /// Souvenir Question: "What sequence of buttons were pressed?"
    /// Since there are (usually) multiple answers, having only the submitted word and offset doesn't guarantee knowing button presses.
    /// </summary>
    internal ButtonType[] buttonPresses;

    /// <summary>
    /// If true, disables incompatible markers and initalizes bottom display's event as OnInteractEnded instead of OnDefocus.
    /// </summary>
    internal static bool vrMode;

    internal bool isSolved, isCountingDown, isInSubmission, isSelected, isAnimated;
    internal static int moduleIdCounter, streamDelay;
    internal int moduleId, index;
    internal string solution, submission;

    /// <summary>
    /// The startup method for Init, which gets the module prepared to be interacted with.
    /// </summary>
    internal void Activate()
    {
        // Sets module ID.
        moduleId = ++moduleIdCounter;

        // Sets accessibility.
        ModSettingsJSON.Get(pho, out render.cruelMode, out vrMode, out streamDelay);
        UpdateCruel();

        // This allows TP to read this class.
        pho.TP.Activate(this);

        // Plays voice lines only if it is the last one initiated. Not checking this causes multiple sounds to stack up.
        pho.Info.OnBombSolved += delegate () { if (moduleId == moduleIdCounter) Function.PlaySound("voice_bombdisarmed", pho); };
        pho.Info.OnBombExploded += delegate () { if (moduleId == moduleIdCounter) Function.PlaySound("voice_gameover", pho); };

        pho.Number.OnInteract += select.NumberPress();
        pho.Color.OnInteract += select.ColorPress();
        Function.OnInteractArray(pho.Buttons, select.ButtonPress);

        // Initalize markers, and use OnDefocus.
        if (!vrMode)
        {
            pho.Color.OnDefocus += select.ColorRelease();
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
    /// Sets cruel mode to the tile textures, based on the cruel mode variable.
    /// </summary>
    private void UpdateCruel()
    {
        foreach (var tile in pho.Tiles)
            tile.material.mainTexture = render.cruelMode ? null : pho.TileTexture;
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
        solution = string.Empty; 

        Function.PlaySound("strike", pho);

        // Disable screen.
        render.UpdateDisplay(0);
        isCountingDown = false;

        pho.Module.HandleStrike();

        // Increase in amount of reds.
        for (int i = 0; i <= 25; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Rnd.Range(0, 25) >= i ? Color.black : Function.GetColor(ButtonType.Red);

            yield return new WaitForSecondsRealtime(0.02f);
        }

        // Decrease in amount of reds.
        for (int i = 0; i <= 25; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Rnd.Range(0, 25) >= i ? Function.GetColor(ButtonType.Red) : Color.black;

            yield return new WaitForSecondsRealtime(0.02f);
        }

        isAnimated = false;
    }

    /// <summary>
    /// Gets called when module is about to solve, plays solve animation.
    /// </summary>
    internal IEnumerator Solve()
    {
        isSolved = true;
        Debug.LogFormat("[Phosphorescence #{0}]: The submisssion was correct, that is all.", moduleId);
        Function.PlaySound("success", pho);

        // Removes the texture, since it doesn't matter at this stage.
        foreach (var tile in pho.Tiles)
            tile.material.mainTexture = null;

        // Keeps track of the current states of the screen.
        // This needs to be a 1-dimensional array for it to easily align with the Renderer array.
        int[] displayStates = new int[49];

        // Sets all text to be a light shade of green.
        foreach (var text in pho.ScreenText)
            text.color = new Color32(98, 196, 98, 255);

        // Flashes all colors.
        for (int i = 0; i < Enum.GetNames(typeof(ButtonType)).Length; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Function.GetColor((ButtonType)i);

            yield return new WaitForSeconds(0.1f);
        }

        // Solves the module.
        Function.PlaySound("voice_challengecomplete", pho);
        pho.Module.HandlePass();
        yield return select.animate.PostSolve(pho, displayStates);
    }
}
