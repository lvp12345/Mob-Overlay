using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MobOverlay
{
    public class MobOverlay : AOPluginEntry
    {
        public static Settings _settings;

        private static Dictionary<Identity, float> _lastHealthUpdate = new Dictionary<Identity, float>();
        private static TargetOverlayWindow _targetOverlayWindow;
        private static SimpleChar _lastTarget;

        public override void Run()
        {
            try
            {
                base.Run();
                _settings = new Settings("MobOverlay");

                // Initialize settings variables
                _settings.AddVariable("Enable", false);
                _settings.AddVariable("ShowHealthOverlay", true);
                _settings.AddVariable("ShowHostileMobs", true);
                _settings.AddVariable("ShowNeutralMobs", false);
                _settings.AddVariable("ShowFriendlyMobs", false);
                _settings.AddVariable("OverlayRange", 50);
                _settings.AddVariable("ShowHealthNumbers", true);
                _settings.AddVariable("ShowHealthBars", true);
                _settings.AddVariable("OverlaySize", 1.0f);

                // Target overlay settings
                _settings.AddVariable("EnableTargetOverlay", true);
                _settings.AddVariable("TargetOverlayMoveable", true);
                _settings.AddVariable("TargetOverlayOffsetX", 0);
                _settings.AddVariable("TargetOverlayOffsetY", -50);

                SettingsController.RegisterSettingsWindow("Mob Overlay", PluginDirectory + "\\UI\\MobOverlaySettingWindow.xml", _settings);

                // Load custom textures for health bars
                LoadCustomTextures();

                // Initialize target overlay window using Mob Overlay approach
                Utils.LoadCustomTextures($"{PluginDirectory}\\UI\\Textures\\", 42042070);
                _targetOverlayWindow = new TargetOverlayWindow("MobOverlay", PluginDirectory + "\\UI\\TargetOverlayWindow.xml", WindowStyle.Popup, WindowFlags.AutoScale | WindowFlags.NoFade);
                _targetOverlayWindow.Show();

                Game.OnUpdate += OnUpdate;

                Chat.WriteLine("MobOverlay Loaded!");
                Chat.WriteLine("/moboverlay for settings.");
                Chat.WriteLine("Now shows actual 3D health overlays over mobs!");
                Chat.WriteLine("Target overlay shows current target health with moveable UI!");
            }
            catch (Exception e)
            {
                Chat.WriteLine($"MobOverlay: Error during initialization: {e.Message}");
            }
        }

        public override void Teardown()
        {
            _targetOverlayWindow?.Dispose();
            SettingsController.CleanUp();
        }

        private void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                if (_settings["Enable"].AsBool())
                {
                    if (_settings["ShowHealthOverlay"].AsBool())
                    {
                        UpdateHealthOverlays();
                    }

                    if (_settings["EnableTargetOverlay"].AsBool())
                    {
                        UpdateTargetOverlay();
                    }
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine($"MobOverlay: Error in OnUpdate: {e.Message}");
            }
        }

        private void UpdateHealthOverlays()
        {
            try
            {
                var localPlayer = DynelManager.LocalPlayer;
                if (localPlayer == null || !localPlayer.IsValid)
                    return;

                var overlayRange = _settings["OverlayRange"].AsInt32();
                var showHealthNumbers = _settings["ShowHealthNumbers"].AsBool();
                var showHealthBars = _settings["ShowHealthBars"].AsBool();
                var overlaySize = _settings["OverlaySize"].AsFloat();

                // Get all characters and NPCs within range
                var allMobs = new List<SimpleChar>();

                if (_settings["ShowHostileMobs"].AsBool())
                {
                    allMobs.AddRange(DynelManager.NPCs.Where(npc =>
                        npc.IsValid &&
                        npc.IsAlive &&
                        Vector3.Distance(localPlayer.Position, npc.Position) <= overlayRange &&
                        npc.FightingTarget != null));
                }

                if (_settings["ShowNeutralMobs"].AsBool())
                {
                    allMobs.AddRange(DynelManager.NPCs.Where(npc =>
                        npc.IsValid &&
                        npc.IsAlive &&
                        Vector3.Distance(localPlayer.Position, npc.Position) <= overlayRange &&
                        npc.FightingTarget == null));
                }

                if (_settings["ShowFriendlyMobs"].AsBool())
                {
                    allMobs.AddRange(DynelManager.Characters.Where(character =>
                        character.IsValid &&
                        character.IsAlive &&
                        character.Identity != localPlayer.Identity &&
                        Vector3.Distance(localPlayer.Position, character.Position) <= overlayRange));
                }

                // Draw overlays for each mob
                foreach (var mob in allMobs)
                {
                    DrawHealthOverlay(mob, showHealthNumbers, showHealthBars, overlaySize);
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine($"MobOverlay: Error in UpdateHealthOverlays: {e.Message}");
            }
        }

        private void DrawHealthOverlay(SimpleChar mob, bool showNumbers, bool showBars, float size)
        {
            try
            {
                if (mob == null || !mob.IsValid || !mob.IsAlive)
                    return;

                var healthPercent = mob.HealthPercent;
                var position = mob.Position;

                // Adjust position to be above the mob
                position.Y += 2.0f;

                // Draw health bar as a sphere with color based on health percentage
                if (showBars)
                {
                    if (healthPercent > 75)
                        AOSharp.Core.Debug.DrawSphere(position, size, DebuggingColor.Green);
                    else if (healthPercent > 50)
                        AOSharp.Core.Debug.DrawSphere(position, size, DebuggingColor.Yellow);
                    else if (healthPercent > 25)
                        AOSharp.Core.Debug.DrawSphere(position, size, DebuggingColor.Red);
                    else
                        AOSharp.Core.Debug.DrawSphere(position, size, DebuggingColor.Red);
                }

                // Draw health numbers (we'll use a smaller sphere with different color for now)
                if (showNumbers)
                {
                    var numberPosition = position;
                    numberPosition.Y += 1.0f;

                    // Use white sphere to represent health numbers
                    // In a more advanced implementation, we could draw actual text
                    AOSharp.Core.Debug.DrawSphere(numberPosition, size * 0.5f, DebuggingColor.White);

                    // Update chat with health info only when health changes significantly
                    if (ShouldUpdateHealthInfo(mob))
                    {
                        string healthInfo = $"{mob.Name}: {mob.Health:N0}/{mob.MaxHealth:N0} ({healthPercent:F1}%)";
                        Chat.WriteLine(healthInfo);
                        _lastHealthUpdate[mob.Identity] = healthPercent;
                    }
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine($"MobOverlay: Error in DrawHealthOverlay: {e.Message}");
            }
        }

        private bool ShouldUpdateHealthInfo(SimpleChar mob)
        {
            if (!_lastHealthUpdate.ContainsKey(mob.Identity))
                return true;

            var lastHealth = _lastHealthUpdate[mob.Identity];
            var currentHealth = mob.HealthPercent;

            // Update if health changed by more than 10%
            return Math.Abs(currentHealth - lastHealth) > 10.0f;
        }

        private void LoadCustomTextures()
        {
            try
            {
                // Load custom textures for health bars (similar to Mob Overlay)
                var texturesPath = PluginDirectory + "\\UI\\Textures\\";

                // Load with a unique texture ID to avoid conflicts
                if (System.IO.Directory.Exists(texturesPath))
                {
                    // Use a unique ID for MobOverlay textures
                    Utils.LoadCustomTextures(texturesPath, 42042070);
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine($"MobOverlay: Error loading custom textures: {e.Message}");
            }
        }

        private void UpdateTargetOverlay()
        {
            try
            {
                var currentTarget = Targeting.Target;
                SimpleChar targetChar = null;

                // Try to cast to SimpleChar if target exists
                if (currentTarget != null && currentTarget.IsValid)
                {
                    targetChar = currentTarget as SimpleChar;
                }

                // Check if target changed
                if (targetChar != _lastTarget)
                {
                    _lastTarget = targetChar;

                    if (targetChar == null)
                    {
                        _targetOverlayWindow?.Hide();
                        return;
                    }
                }

                // If we have a valid target, update the overlay
                if (targetChar != null && targetChar.IsValid && targetChar.IsAlive)
                {
                    _targetOverlayWindow?.UpdateTarget(targetChar);

                    // For now, position the window at a fixed location
                    // In the future, we could implement 3D-to-2D projection
                    var offsetX = _settings["TargetOverlayOffsetX"].AsInt32();
                    var offsetY = _settings["TargetOverlayOffsetY"].AsInt32();

                    // Position at center of screen with offsets
                    var screenSize = Window.GetScreenSize();
                    var centerX = screenSize.X / 2 + offsetX;
                    var centerY = screenSize.Y / 2 + offsetY;

                    _targetOverlayWindow?.UpdatePosition((int)centerX, (int)centerY);
                    _targetOverlayWindow?.Show();
                }
                else
                {
                    _targetOverlayWindow?.Hide();
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine($"MobOverlay: Error in UpdateTargetOverlay: {e.Message}");
            }
        }
    }
}
