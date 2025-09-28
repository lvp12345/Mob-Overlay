using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using System;
using System.Collections.Generic;

namespace MobOverlay
{
    public static class SettingsController
    {
        private static List<Settings> settingsToSave = new List<Settings>();
        public static Dictionary<string, string> settingsWindows = new Dictionary<string, string>();
        private static bool IsCommandRegistered;

        public static Window settingsWindow;
        public static View settingsView;

        public static void RegisterSettingsWindow(string settingsName, string settingsWindowPath, Settings settings)
        {
            RegisterChatCommandIfNotRegistered();
            settingsWindows[settingsName] = settingsWindowPath;
            settingsToSave.Add(settings);
        }

        public static void CleanUp()
        {
            settingsToSave.ForEach(settings => settings.Save());
        }

        private static void RegisterChatCommandIfNotRegistered()
        {
            if (!IsCommandRegistered)
            {
                Chat.RegisterCommand("moboverlay", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    try
                    {
                        settingsWindow = Window.Create(new Rect(50, 50, 400, 300), "MobOverlay", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

                        if (settingsWindows.ContainsKey("Mob Overlay"))
                        {
                            settingsView = View.CreateFromXml(settingsWindows["Mob Overlay"]);
                            settingsWindow.AppendChild(settingsView, true);

                            // Setup info button
                            if (settingsWindow.FindView("MobOverlayInfoView", out Button infoButton))
                            {
                                infoButton.Clicked += InfoButtonClicked;
                            }

                            settingsWindow.Show(true);
                        }
                    }
                    catch (Exception e)
                    {
                        Chat.WriteLine($"MobOverlay: Error opening settings: {e.Message}");
                    }
                });

                IsCommandRegistered = true;
            }
        }

        private static void InfoButtonClicked(object sender, ButtonBase e)
        {
            try
            {
                var infoWindow = Window.CreateFromXml("Info", "UI\\MobOverlayInfoView.xml",
                    windowSize: new Rect(0, 0, 440, 510),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                infoWindow.Show(true);
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"MobOverlay: Error opening info window: {ex.Message}");
            }
        }
    }
}
