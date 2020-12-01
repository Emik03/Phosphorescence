using Newtonsoft.Json;
using UnityEngine;

namespace PhosphorescenceExtensions
{
    public class ModSettingsJSON
    {
        public bool VRMode { get; set; }
        public int StreamDelay { get; set; }

        public static void Get(PhosphorescenceScript pho, out bool vrMode, out int streamDelay)
        {
            vrMode = false;
            streamDelay = 0;

            try
            {
                ModSettingsJSON settings = JsonConvert.DeserializeObject<ModSettingsJSON>(pho.ModSettings.Settings);

                if (settings != null)
                {
                    vrMode = settings.VRMode;
                    streamDelay = 15 * settings.StreamDelay;
                }
            }
            catch (JsonReaderException e)
            {
                // In the case of catastrophic failure and devastation.
                Debug.LogFormat("[Phosphorescence #{0}]: JSON error: \"{1}\", resorting to default values.", pho.init.moduleId, e.Message);
            }
        }
    }
}
