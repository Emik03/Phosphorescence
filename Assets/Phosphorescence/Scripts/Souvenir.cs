using PhosphorescenceExtensions;
using UnityEngine;

/// <summary>
/// Phosphorescence-side implementation for the modded module 'Souvenir' by Timwi.
/// </summary>
internal class Souvenir
{
    /// <summary>
    /// This class is used to be compatible with Souvenir. 
    /// The only thing needed to pass in here is the moduleId for logging reasons.
    /// </summary>
    /// <param name="moduleId">The module's Id.</param>
    internal Souvenir(int moduleId)
    {
        _moduleId = moduleId;
    }

    /// <summary>
    /// When this is set true, Souvenir will start reading this class.
    /// </summary>
    internal bool Solved { get; private set; }

    /// <summary>
    /// Souvenir Question: "What was the offset?" 
    /// </summary>
    internal int Offset { get; private set; }

    /// <summary>
    /// Souvenir Question: "What sequence of buttons were pressed?"
    /// Since there are (usually) multiple answers, having only the submitted word and offset doesn't guarantee knowing button presses.
    /// </summary>
    internal ButtonType[] ButtonPresses { get; private set; }

    /// <summary>
    /// The module's Id, meant for logging.
    /// </summary>
    private readonly int _moduleId;

    /// <summary>
    /// Sets the Souvenir variables to the parameters provided. When called, this class assumes that the module is solved.
    /// </summary>
    /// <param name="offset">The offset </param>
    /// <param name="buttonPresses">The sequence of button presses used to solve the module.</param>
    internal void Set(int offset, ButtonType[] buttonPresses)
    {
        Solved = true;
        Offset = offset;
        ButtonPresses = buttonPresses;

        Debug.LogFormat("[Phosphorescence #{0}]: Souvenir detected, the buttons that were pushed to solve the module were {1}.", _moduleId, buttonPresses.Join(", "));
    }
}
