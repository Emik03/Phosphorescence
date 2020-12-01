using PhosphorescenceExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// Contains code that is needed repeatedly throughout.
/// </summary>
internal static class Function
{
    /// <summary>
    /// Plays a sound.
    /// </summary>
    /// <param name="sound">The name of the sound file.</param>
    internal static void PlaySound(string sound, PhosphorescenceScript pho)
    {
        pho.Audio.PlaySoundAtTransform(sound, pho.transform);
    }

    /// <summary>
    /// ElasticIn ease that returns the current step.
    /// </summary>
    /// <param name="k">The current step. Generally in range of 0 to 1 (both inclusive).</param>
    /// <returns>The current step of the number provided.</returns>
    internal static float ElasticIn(float k)
    {
        return k % 1 == 0 ? k : -Mathf.Pow(2f, 10f * (k -= 1f)) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f);
    }

    /// <summary>
    /// ElasticOut ease that returns the current step.
    /// </summary>
    /// <param name="k">The current step. Generally in range of 0 to 1 (both inclusive).</param>
    /// <returns>The current step of the number provided.</returns>
    internal static float ElasticOut(float k)
    {
        return k % 1 == 0 ? k : Mathf.Pow(2f, -10f * k) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f) + 1f;
    }

    /// <summary>
    /// Assigns KMSelectable.OnInteract event handlers. Reminder that your method should have only a single integer parameter, which will be used to pass the index of the button pressed.
    /// </summary>
    /// <param name="selectables">The array to create event handlers for.</param>
    /// <param name="method">The method that will be called whenever an event is triggered.</param>
    internal static void OnInteractArray(KMSelectable[] selectables, Func<int, KMSelectable.OnInteractHandler> method)
    {
        for (int i = 0; i < selectables.Length; i++)
        {
            // This might look redundant, but using 'i' always passes in selectable.Length - 1.
            // This is a workaround, in other words.
            int j = i;
            selectables[i].OnInteract += method(j);
        }
    }

    /// <summary>
    /// Generates and returns a boolean array that is random.
    /// </summary>
    /// <param name="length">The length of the array.</param>
    /// <returns>A boolean array of random values.</returns>
    internal static bool[] RandomBools(int length)
    {
        bool[] array = new bool[length];
        for (int i = 0; i < array.Length; i++)
            array[i] = Rnd.Range(0, 1f) > 0.5;
        return array;
    }

    /// <summary>
    /// Mixes the two colors provided and sets the renderer.material.color to be that color. Weighting can be included.
    /// </summary>
    /// <param name="renderer">The renderer to change color. This does mean that the renderer's material must support color.</param>
    /// <param name="colorA">The first color, as f approaches 0.</param>
    /// <param name="colorB">The second color, as f approaches 1.</param>
    /// <param name="f">The weighting of color mixing, with 0 being 100% colorA and 1 being 100% colorB.</param>
    internal static void SetIntertwinedColor(Renderer renderer, Color32 colorA, Color32 colorB, float f = 0.5f)
    {
        float negF = 1 - f;
        renderer.material.color = new Color32((byte)((colorA.r * negF) + (colorB.r * f)), (byte)((colorA.g * negF) + (colorB.g * f)), (byte)((colorA.b * negF) + (colorB.b * f)), 255);
    }

    /// <summary>
    /// Calculates all possible answers of the module.
    /// </summary>
    /// <param name="solution">The word to reach to.</param>
    /// <param name="index">The offset index that is used for the colors.</param>
    /// <returns>A string array where every element is a valid answer.</returns>
    internal static string[] GetAllAnswers(string solution, int index)
    {
        // Initalize list.
        List<string>[] answers = new List<string>[solution.Length];
        for (int i = 0; i < answers.Length; i++)
            answers[i] = new List<string>();

        // For each character.
        for (int i = 0; i < solution.Length; i++)
        {
            // For each color's word.
            foreach (string button in Enum.GetNames(typeof(ButtonType)))
            {
                // Would pushing the button be valid?
                if (solution[i] == button[(index + i) % button.Length].ToString().ToLowerInvariant().ToCharArray()[0])
                {
                    // Since Blue and Black both share the same first letter, K is used instead for black.
                    string nextAnswer = button == "Black" ? "K" : button[0].ToString();

                    // If this is not the first iteration, we need to add answers based on all the previous answers as well.
                    if (i == 0)
                        answers[i].Add(nextAnswer);
                    else
                        foreach (string answer in answers[i - 1])
                            answers[i].Add(answer + nextAnswer);
                }
            }
        }

        return answers[answers.Length - 1].ToArray();
    }

    /// <summary>
    /// Gets the color equivalent of the supplied ButtonType.
    /// </summary>
    /// <param name="buttonType">The type of ButtonType to use.</param>
    /// <returns>The color equivalent of ButtonType.</returns>
    internal static Color GetColor(ButtonType buttonType)
    {
        switch (buttonType)
        {
            case ButtonType.Black: return Color.black;
            case ButtonType.Red: return Color.red;
            case ButtonType.Green: return Color.green;
            case ButtonType.Blue: return Color.blue;
            case ButtonType.Cyan: return Color.cyan;
            case ButtonType.Magenta: return Color.magenta;
            case ButtonType.Yellow: return Color.yellow;
            case ButtonType.White: return Color.white;
            default: throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Plays voice lines whenever the timer strikes a specific time.
    /// </summary>
    /// <param name="time">The current time remaining in seconds.</param>
    /// <param name="exception">An exception that, if true, will not play voice lines.</param>
    internal static void CountSound(int time, PhosphorescenceScript pho, bool exception)
    {
        if (exception)
            return;

        PlaySound("timerTick", pho);

        switch (time)
        {
            case 1: PlaySound("voice_one", pho); break;
            case 2: PlaySound("voice_two", pho); break;
            case 3: PlaySound("voice_three", pho); break;
            case 4: PlaySound("voice_four", pho); break;
            case 5: PlaySound("voice_five", pho); break;
            case 6: PlaySound("voice_six", pho); break;
            case 7: PlaySound("voice_seven", pho); break;
            case 8: PlaySound("voice_eight", pho); break;
            case 9: PlaySound("voice_nine", pho); break;
            case 30: PlaySound("voice_thirtyseconds", pho); break;
            case 60: PlaySound("voice_oneminute", pho); break;
            case 120: PlaySound("voice_twominutes", pho); break;
            case 180: PlaySound("voice_threeminutes", pho); break;
            case 240: PlaySound("voice_fourminutes", pho); break;
            default: if (time % 60 == 0) PlaySound("notableTimeLeft", pho); break;
        }
    }

    /// <summary>
    /// Counts the number of L-shapes within the boolean array. The boolean array is treated as being 2-dimensional.
    /// </summary>
    /// <param name="colors">The boolean array, which must be of length 49.</param>
    /// <returns>An integer representing the amount of L's found.</returns>
    internal static int GetLCount(bool[] colors)
    {
        if (colors.Length != 49)
            throw new IndexOutOfRangeException("Colors is length " + colors.Length);

        const int s = 7;
        int count = 0;

        for (int i = 0; i < s - 1; i++)
            for (int j = 0; j < s - 1; j++)
            {
                // Subarray containing the 4 current squares.
                bool[] subArray = new[]
                {
                    colors[(i * s) + j],
                    colors[(i * s) + j + 1],
                    colors[((i + 1) * s) + j],
                    colors[((i + 1) * s) + j + 1]
                };
                
                // Do the current 4 squares contain 1 or 3 black squares? (Which indicates an L-shape).
                if (subArray.Where(b => b).Count() % 2 == 1)
                    count++;
            }

        return count;
    }
}