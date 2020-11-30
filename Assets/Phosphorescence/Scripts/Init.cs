using PhosphorescenceExtensions;
using System.Collections;
using UnityEngine;

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

    internal bool isSolved, isCountingDown, isInSubmission, isTouched, isAnimated;
    internal static int moduleIdCounter;
    internal int moduleId, index;
    internal string solution, submission;

    internal void Activate()
    {
        moduleId = ++moduleIdCounter;

        pho.Info.OnBombSolved += delegate () { if (moduleId == moduleIdCounter) Function.PlaySound("voice_bombdisarmed", pho); };
        pho.Info.OnBombExploded += delegate () { if (moduleId == moduleIdCounter) Function.PlaySound("voice_gameover", pho); };

        pho.Color.OnInteract += select.ColorPanelPress();
        pho.Color.OnInteractEnded += select.ColorPanelRelease();
        pho.Number.OnInteract += select.NumberPress();

        for (int i = 0; i < pho.Buttons.Length; i++)
        {
            int j = i;
            pho.Buttons[i].OnInteract += select.ButtonPress(j);
        }
    }

    internal IEnumerator Strike()
    {
        isAnimated = true;

        Debug.LogFormat("[Phosphorescence #{0}]: Submission \"{1}\" did not match the expected \"{2}\"", moduleId, submission, solution);
        Function.PlaySound("strike", pho);
        
        render.UpdateDisplay(0);
        isCountingDown = false;
        pho.Module.HandleStrike();

        for (int i = 0; i <= 25; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Function.GetColor(Random.Range(0, 25) >= i ? ButtonType.Black : ButtonType.Red);

            yield return new WaitForSecondsRealtime(0.02f);
        }

        for (int i = 0; i <= 25; i++)
        {
            foreach (var tile in pho.Tiles)
                tile.material.color = Function.GetColor(Random.Range(0, 25) >= i ? ButtonType.Red : ButtonType.Black);

            yield return new WaitForSecondsRealtime(0.02f);
        }

        isAnimated = false;
    }

    internal IEnumerator Solve()
    {
        isSolved = true;
        Debug.LogFormat("[Phosphorescence #{0}]: The submisssion was correct, that is all.", moduleId);

        if (pho.Info.GetSolvableModuleNames().Count - 1 != pho.Info.GetSolvedModuleNames().Count)
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

        pho.Module.HandlePass();
        Function.PlaySound("voice_challengecomplete", pho);

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

                    yield return new WaitForSecondsRealtime(0.1f);
                }
        }
    }
}
