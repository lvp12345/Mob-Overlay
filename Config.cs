using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace MobOverlay
{
    public class MobOverlayConfig
    {
        private static string _savePath;
        public Vector2 ScreenCoords;
        public List<int> HiddenUsers;

        public MobOverlayConfig Load(string pluginDir)
        {
            try
            {
                _savePath = $"{pluginDir}\\MobOverlayConfig.json";

                if (File.Exists(_savePath))
                {
                    return JsonConvert.DeserializeObject<MobOverlayConfig>(File.ReadAllText(_savePath));
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_savePath));
                    MobOverlayConfig defaults = new MobOverlayConfig
                    {

                        HiddenUsers = new List<int>(),
                        ScreenCoords = new Vector2(0, 0)
                    };

                    if (defaults != null)
                        File.WriteAllText(_savePath, JsonConvert.SerializeObject(defaults, Formatting.Indented));

                    return defaults;
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
                return default;
            }
        }


        public void Save()
        {
            try
            {
                File.WriteAllText(_savePath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
            }
        }

        public static MobOverlayConfig LoadConfig(string configFolder)
        {
            MobOverlayConfig instance = new MobOverlayConfig();
            return instance.Load(configFolder);
        }
    }
}
