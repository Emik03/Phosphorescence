using PhosphorescenceExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class TPScript : MonoBehaviour 
{
	public PhosphorescenceScript Pho;

	private Init _init;
	private Render _render;
	private Select _select;

	private static readonly Dictionary<char, ButtonType> _charToButton = new Dictionary<char, ButtonType>()
	{
		{ 'K', ButtonType.Black },
		{ 'R', ButtonType.Red },
		{ 'G', ButtonType.Green },
		{ 'B', ButtonType.Blue },
		{ 'C', ButtonType.Cyan },
		{ 'M', ButtonType.Magenta },
		{ 'Y', ButtonType.Yellow },
		{ 'W', ButtonType.White },
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

	private IEnumerator ProcessTwitchCommand(string command)
    {
		string[] split = command.Split();

		while (_init.isAnimated)
			yield return true;

		if (Regex.IsMatch(split[0], @"^\s*display\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			yield return DisplayCommand();
		}

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
		yield return Pho.Color.OnCancel();
		yield return new WaitForSecondsRealtime(0.2f);
		yield return Pho.Color.OnInteract();
	}

	private IEnumerator NextSequenceCommand()
	{
		string temp = _render.letters;

		while (temp == _render.letters)
		{
			yield return Pho.Color.OnCancel();
			yield return new WaitForSecondsRealtime(0.05f);
			yield return Pho.Color.OnInteract();
			yield return new WaitForSecondsRealtime(0.05f);
		}
	}

	private IEnumerator DisplayCommand()
    {
		while (_init.isAnimated)
			yield return true;
		yield return Pho.Number.OnInteract();
	}

	private IEnumerator SubmitCommand(string submit)
    {
		if (!_init.isInSubmission)
        {
			yield return Pho.Number.OnInteract();
			while (_init.isAnimated)
				yield return true;
		}

        foreach (char s in submit)
        {
			ButtonType button;
			_charToButton.TryGetValue(s, out button);

			int buttonIndex = Array.IndexOf(_select.buttons, button);
			if (buttonIndex == -1)
				throw new IndexOutOfRangeException("SubmitCommand() caused an unexpected error, dumping variables: " + new object[] { submit, s, button, buttonIndex }.Join(", "));

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

		if (!_init.isCountingDown)
		{
			yield return Pho.Number.OnInteract();
			while (_init.isAnimated)
				yield return true;
		}

		if (!_init.isInSubmission)
		{
			yield return Pho.Number.OnInteract();
			while (_init.isAnimated)
				yield return true;
		}

		_init.submission = string.Empty;

		foreach (char c in _init.solution)
		{
			yield return new WaitForSecondsRealtime(0.2f);

			for (int i = 0; i < _select.buttons.Length; i++)
            {
				string currentSubmit = _select.buttons[i].ToString();
				int currentIndex = _init.index + _init.submission.Length;

				if (c == currentSubmit[currentIndex % currentSubmit.Length].ToString().ToLowerInvariant().ToCharArray()[0])
                {
					Pho.Buttons[i].OnInteract();
					break;
                }
			}
        }

		Pho.Number.OnInteract();
	}
}
