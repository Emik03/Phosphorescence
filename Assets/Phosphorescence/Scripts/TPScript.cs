using System.Collections;
using UnityEngine;

public class TPScript : MonoBehaviour 
{
	public PhosphorescenceScript Pho;

	private Init _init;
	private Select _select;

#pragma warning disable 414
	private const string TwitchHelpMessage = @"!{0}";
#pragma warning restore 414

	private void Start()
    {
		_init = Pho.init;
		_select = _init.select;
    }

	private IEnumerator ProcessTwitchCommand(string command)
    {
		string[] split = command.Split();

		yield return null;
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
