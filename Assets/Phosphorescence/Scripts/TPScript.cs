using PhosphorescenceExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// TwitchPlays support for Phosphorescence. Contains an autosolver.
/// </summary>
public class TPScript : MonoBehaviour 
{
	public PhosphorescenceScript Pho;

	private Init _init;
	private Render _render;
	private Select _select;

    /// <summary>
    /// Used to take the individual characters from a user's submission and get the equivalent ButtonType.
    /// </summary>
	private static readonly Dictionary<char, ButtonType> _charToButton = new Dictionary<char, ButtonType>()
	{
		{ 'k', ButtonType.Black },
		{ 'r', ButtonType.Red },
		{ 'r', ButtonType.Green },
		{ 'b', ButtonType.Blue },
		{ 'c', ButtonType.Cyan },
		{ 'm', ButtonType.Magenta },
		{ 'y', ButtonType.Yellow },
		{ 'w', ButtonType.White },
	};

#pragma warning disable 414
	private const string TwitchHelpMessage = @"!{0} display | !{0} next | !{0} nextsequence | !{0} submit <KRGBCMYW>";
#pragma warning restore 414

	private void Start()
    {
		_init = Pho.init;
		_render = _init.render;
		_select = _init.select;
    }

    /// <summary>
    /// Parses the user command and interacts with the module accordingly.
    /// </summary>
    /// <param name="command">The user's command, trimming off the initial "!{0}" part.</param>
	private IEnumerator ProcessTwitchCommand(string command)
    {
		string[] split = command.Split();

        // Wait until the animations aren't playing.
		while (_init.isAnimated)
			yield return true;

        // Display command: no parameters, a command so simple it doesn't need its own method.
		if (Regex.IsMatch(split[0], @"^\s*display\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
            yield return Pho.Number.OnInteract();
        }

        // Next command: no parameters, needs to be in an active non-submission state.
		else if (Regex.IsMatch(split[0], @"^\s*next\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;

			if (!_init.isCountingDown)
				yield return "sendtochaterror The module isn't active right now. This command is unavailable until the display is pressed.";

			else if (_init.isInSubmission)
				yield return "sendtochaterror The module is currently in submission. This command is unavailable until the next activation.";

			else
				yield return NextCommand();
		}

        // Next command: no parameters, needs to be in an active non-submission state. An extension of the next command.
		else if (Regex.IsMatch(split[0], @"^\s*nextsequence\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;

			if (!_init.isCountingDown)
				yield return "sendtochaterror The module isn't active right now. This command is unavailable until the display is pressed.";

			else if (_init.isInSubmission)
				yield return "sendtochaterror The module is currently in submission. This command is unavailable until the next activation.";

			else
				yield return NextSequenceCommand();
		}

        // Submit command: 1 parameter, needs to be in an active submission state. Additional parsing is needed to make sure the command is formatted correctly.
		else if (Regex.IsMatch(split[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;

			if (!_init.isCountingDown)
				yield return "sendtochaterror The module isn't active right now. This command is unavailable until the display is pressed.";

			else if (split.Length < 2 || split[1].Length == 0)
				yield return "sendtochaterror There is no sequence provided. Example submission: \"submit RGCK\" submits Red, Green, Cyan and Black.";

			else if (split.Length > 2)
				yield return "sendtochaterror There are too many parameters. Be sure that the button presses aren't separated by a space. Example submission: \"submit RGCK\" submits Red, Green, Cyan and Black.";

			else if (split[1].Length > 6)
				yield return "sendtochaterror Submissions longer than 6 characters would reset the buttons, which is useless in TwitchPlays.";

			else if (split[1].Any(c => !"KRGBCMYW".Contains(c)))
				yield return "sendtochaterror One of the button presses provided are invalid. The only valid characters are K, R, G, B, C, M, Y, and W.";

			else
				yield return SubmitCommand(split[1]);
		}
	}
    
	private IEnumerator NextCommand()
    {
        // A release is done prior to a press instead of the other way around to keep the screen lit after the command.
		yield return Pho.Color.OnCancel();
		yield return new WaitForSecondsRealtime(0.2f);
		yield return Pho.Color.OnInteract();
	}

    
	private IEnumerator NextSequenceCommand()
	{
		string temp = _render.letters;

        // Mashes the screen until a new sequence is made.
		while (temp == _render.letters)
		{
			yield return Pho.Color.OnCancel();
			yield return new WaitForSecondsRealtime(0.05f);
			yield return Pho.Color.OnInteract();
			yield return new WaitForSecondsRealtime(0.05f);
		}
	}

	private IEnumerator SubmitCommand(string submit)
    {
        // Enters submission if it isn't in already.
		if (!_init.isInSubmission)
        {
			yield return Pho.Number.OnInteract();
			while (_init.isAnimated)
				yield return true;
		}

        // For each character in the user's submission.
        foreach (char s in submit)
        {
			ButtonType button;

            // Converts the first character to lowercase, making it case-insensitive.
			_charToButton.TryGetValue(s.ToString().ToLowerInvariant().ToCharArray()[0], out button);

            // Gets the index of the button to press.
			int buttonIndex = Array.IndexOf(_select.buttons, button);

            // Failsafe, in case it isn't found.
			if (buttonIndex == -1)
				throw new IndexOutOfRangeException("SubmitCommand() caused an unexpected error, dumping variables: " + new object[] { submit, s, button, buttonIndex }.Join(", "));

            // Presses the corresponding button.
            yield return Pho.Buttons[buttonIndex].OnInteract();
            yield return new WaitForSecondsRealtime(0.2f);
        }

		while (_init.isAnimated)
			yield return true;
		yield return Pho.Number.OnInteract();
    }

	private IEnumerator TwitchHandleForcedSolve()
	{
		yield return null;

		while (_init.isAnimated)
			yield return true;

        // If inactive, active it.
		if (!_init.isCountingDown)
		{
			yield return Pho.Number.OnInteract();
			while (_init.isAnimated)
				yield return true;
		}

        // If not in submission, enter submission.
		if (!_init.isInSubmission)
		{
			yield return Pho.Number.OnInteract();
			while (_init.isAnimated)
				yield return true;
		}

        // Reset submission, just in case it had any button presses.
		_init.submission = string.Empty;
        
        // Take each character from the solution.
		foreach (char c in _init.solution)
		{
            // Take each button.
			for (int i = 0; i < _select.buttons.Length; i++)
            {
                // Get the string name, and current index.
				string currentSubmit = _select.buttons[i].ToString();
				int currentIndex = _init.index + _init.submission.Length;

                // Does the button match the character, and therefore is the answer?
				if (c == currentSubmit[currentIndex % currentSubmit.Length].ToString().ToLowerInvariant().ToCharArray()[0])
                {
					Pho.Buttons[i].OnInteract();
					break;
                }
			}

            yield return new WaitForSecondsRealtime(0.2f);
        }

        // Presses the 7-segment display to submit the answer.
		Pho.Number.OnInteract();
	}
}
