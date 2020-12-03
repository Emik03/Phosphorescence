using UnityEngine;

/// <summary>
/// On the Subject of Phosphorescence - A module created by Emik, with the model by Aero.
/// </summary>
public class PhosphorescenceScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMColorblindMode Colorblind;
    public KMHighlightable[] MarkerHighlightables;
    public KMModSettings ModSettings;
    public KMSelectable Color, Number;
    public KMSelectable[] Buttons, Markers;
    public Renderer[] Tiles, ButtonRenderers, MarkerRenderers;
    public TextMesh[] ScreenText, ButtonText;
    public Texture ColorblindTexture;
    public Transform Panel, Screen;

    internal Init init;

    private void Awake()
    {
        Words.Init();
        Module.OnActivate += (init = new Init(this)).Activate;
    }
}
