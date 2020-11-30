using UnityEngine;

/// <summary>
/// On the Subject of Phosphorescence - A module created by Emik, with the model by Aero.
/// </summary>
public class PhosphorescenceScript : MonoBehaviour
{
    public class ModSettingsJSON
    {
        public bool OneTapHolds;
        public int StreamDelay;
    }

    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMSelectable Color, Number;
    public KMSelectable[] Buttons;
    public Renderer[] Tiles, ButtonRenderers;
    public TextMesh[] Text;
    public Transform Panel, Screen;

    internal Init init;

    private void Awake()
    {
        Module.OnActivate += (init = new Init(this)).Activate;
        //Words.ReturnAllWords();
    }
}
