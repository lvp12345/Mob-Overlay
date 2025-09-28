using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace MobOverlay
{
    public class TargetOverlay : AOPluginEntry
    {
        internal static string PluginDir;
        internal static MobOverlayConfig Config;
        private static PlayerGroupWindow _targetWindow;
        private static PlayerEntryView _targetView;
        private static Dynel _lastTarget;
        private static DateTime _lastUpdate = DateTime.MinValue;
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);

        [System.Obsolete]
        public override void Run(string pluginDir)
        {
            Chat.WriteLine("Mob Overlay loaded!\n" +
            "/mobtarget - toggles the target window");

            PluginDir = pluginDir;

            // Load configuration
            Config = MobOverlayConfig.LoadConfig(pluginDir);

            Utils.LoadCustomTextures($"{PluginDir}\\UI\\Textures\\", 42042069);

            // Create a single window to show target info
            _targetWindow = new PlayerGroupWindow("MobOverlay", $"{PluginDir}\\UI\\PlayerGroupWindow.xml", $"{PluginDir}\\UI\\PlayerEntryView.xml");
            _targetWindow.Show();
            _targetWindow.Window.MoveTo(100, 100);

            // Create a test target view to show immediately
            _targetView = new PlayerEntryView($"{PluginDir}\\UI\\PlayerEntryView.xml", "No Target", Profession.Unknown);
            _targetView.SetHealthText("0 / 0");
            _targetView.SetNanoText("0 / 0");
            _targetWindow.AddToGroup(_targetView.Root);
            _targetWindow.Root.FitToContents();

            Game.OnUpdate += OnUpdate;

            Chat.RegisterCommand("mobtarget", (string command, string[] param, ChatWindow chatWindow) =>
            {
                try
                {
                    if (_targetWindow != null && _targetWindow.Window != null)
                    {
                        // Clean up existing window
                        if (_targetView != null)
                        {
                            _targetWindow.RemoveFromGroup(_targetView.Root);
                            _targetView = null;
                        }
                        _targetWindow.Dispose();
                        _targetWindow = null;
                        Chat.WriteLine("Mob overlay closed.");
                    }
                    else
                    {
                        // Create new window
                        _targetWindow = new PlayerGroupWindow("MobOverlay", $"{PluginDir}\\UI\\PlayerGroupWindow.xml", $"{PluginDir}\\UI\\PlayerEntryView.xml");
                        _targetWindow.Show();
                        _targetWindow.Window.MoveTo(100, 100);
                        Chat.WriteLine("Mob overlay opened.");

                        // Update display if we already have a target
                        UpdateTargetDisplay();
                    }
                }
                catch (System.Exception e)
                {
                    Chat.WriteLine($"Mob Overlay Command Error: {e.Message}");
                    _targetWindow = null;
                    _targetView = null;
                }
            });
        }

        private static void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                // Only update if window exists
                if (_targetWindow == null || _targetWindow.Window == null)
                    return;

                // Get target as Dynel (works for both SimpleChar and NPCs)
                var currentTarget = Targeting.Target;

                if (currentTarget != _lastTarget)
                {
                    _lastTarget = currentTarget;
                    UpdateTargetDisplay();
                }

                // Only update every 3 seconds
                if (DateTime.Now - _lastUpdate < UpdateInterval)
                    return;

                _lastUpdate = DateTime.Now;

                // Update health and nano every 3 seconds if we have a target
                if (currentTarget != null && currentTarget.IsValid && _targetView != null)
                {
                    try
                    {
                        UpdateHealthText(currentTarget);
                        UpdateNanoText(currentTarget);

                        // Try to get health percentage
                        if (currentTarget is SimpleChar simpleChar)
                        {
                            if (_targetView != null)
                            {
                                _targetView.UpdateHealth(simpleChar.HealthPercent / 100f);
                                _targetView.UpdateNano(simpleChar.NanoPercent / 100f);
                            }
                        }
                        else
                        {
                            // For NPCs, calculate health percentage using GetStat
                            try
                            {
                                int health = currentTarget.GetStat(Stat.Health);
                                int maxHealth = currentTarget.GetStat(Stat.MaxHealth);
                                if (maxHealth > 0 && _targetView != null)
                                {
                                    float healthPercent = (float)health / maxHealth;
                                    _targetView.UpdateHealth(healthPercent);
                                }
                                else if (_targetView != null)
                                {
                                    _targetView.UpdateHealth(1.0f);
                                }

                                // Try to get nano for NPCs
                                int nano = currentTarget.GetStat(Stat.CurrentNano);
                                int maxNano = currentTarget.GetStat(Stat.MaxNanoEnergy);
                                if (maxNano > 0 && _targetView != null)
                                {
                                    float nanoPercent = (float)nano / maxNano;
                                    _targetView.UpdateNano(nanoPercent);
                                }
                                else if (_targetView != null)
                                {
                                    _targetView.UpdateNano(1.0f);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                // If health/nano not available, show full
                                if (_targetView != null)
                                {
                                    _targetView.UpdateHealth(1.0f);
                                    _targetView.UpdateNano(1.0f);
                                }
                                Chat.WriteLine($"NPC stat error: {ex.Message}");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Chat.WriteLine($"Target update error: {ex.Message}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Chat.WriteLine($"Mob Overlay OnUpdate Error: {e.Message}");
                Chat.WriteLine($"Stack trace: {e.StackTrace}");
            }
        }

        private static void UpdateTargetDisplay()
        {
            try
            {
                if (_targetWindow == null || _targetWindow.Window == null)
                    return;

                // Remove old target view safely
                if (_targetView != null)
                {
                    try
                    {
                        _targetWindow.RemoveFromGroup(_targetView.Root);
                    }
                    catch (System.Exception ex)
                    {
                        Chat.WriteLine($"Error removing target view: {ex.Message}");
                    }
                    _targetView = null;
                }

                // Add new target view if we have a target
                if (_lastTarget != null && _lastTarget.IsValid)
                {
                    try
                    {
                        // Get profession - default to Unknown for NPCs
                        var profession = Profession.Unknown;
                        if (_lastTarget is SimpleChar simpleChar)
                        {
                            profession = simpleChar.Profession;
                        }

                        _targetView = new PlayerEntryView($"{PluginDir}\\UI\\PlayerEntryView.xml", _lastTarget.Name, profession);

                        // Set initial health and nano
                        if (_lastTarget is SimpleChar sc)
                        {
                            _targetView.UpdateHealth(sc.HealthPercent / 100f);
                            _targetView.UpdateNano(sc.NanoPercent / 100f);
                        }
                        else
                        {
                            try
                            {
                                int health = _lastTarget.GetStat(Stat.Health);
                                int maxHealth = _lastTarget.GetStat(Stat.MaxHealth);
                                if (maxHealth > 0)
                                {
                                    float healthPercent = (float)health / maxHealth;
                                    _targetView.UpdateHealth(healthPercent);
                                }
                                else
                                {
                                    _targetView.UpdateHealth(1.0f);
                                }

                                int nano = _lastTarget.GetStat(Stat.CurrentNano);
                                int maxNano = _lastTarget.GetStat(Stat.MaxNanoEnergy);
                                if (maxNano > 0)
                                {
                                    float nanoPercent = (float)nano / maxNano;
                                    _targetView.UpdateNano(nanoPercent);
                                }
                                else
                                {
                                    _targetView.UpdateNano(1.0f);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                _targetView.UpdateHealth(1.0f);
                                _targetView.UpdateNano(1.0f);
                                Chat.WriteLine($"Error getting NPC stats: {ex.Message}");
                            }
                        }

                        UpdateHealthText(_lastTarget);
                        UpdateNanoText(_lastTarget);
                        _targetWindow.AddToGroup(_targetView.Root);

                        // Safely fit to contents
                        try
                        {
                            _targetWindow.Root.FitToContents();
                        }
                        catch (System.Exception ex)
                        {
                            Chat.WriteLine($"Error fitting to contents: {ex.Message}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Chat.WriteLine($"Error creating target view: {ex.Message}");
                        _targetView = null;
                    }
                }
                else
                {
                    try
                    {
                        // Show "No Target" when no target selected
                        _targetView = new PlayerEntryView($"{PluginDir}\\UI\\PlayerEntryView.xml", "No Target", Profession.Unknown);
                        _targetView.SetHealthText("0 / 0");
                        _targetView.SetNanoText("0 / 0");
                        _targetWindow.AddToGroup(_targetView.Root);
                        _targetWindow.Root.FitToContents();
                    }
                    catch (System.Exception ex)
                    {
                        Chat.WriteLine($"Error creating 'No Target' view: {ex.Message}");
                        _targetView = null;
                    }
                }
            }
            catch (System.Exception e)
            {
                Chat.WriteLine($"UpdateTargetDisplay Error: {e.Message}");
                Chat.WriteLine($"Stack trace: {e.StackTrace}");
            }
        }

        private static void UpdateHealthText(Dynel target)
        {
            if (_targetView == null || target == null || !target.IsValid)
                return;

            try
            {
                // Use the same approach as malis-mb-scanner and TargetHealthView
                if (target is SimpleChar simpleChar)
                {
                    int currentHealth = simpleChar.Health;
                    int maxHealth = simpleChar.MaxHealth;
                    _targetView.SetHealthText($"{currentHealth:N0} / {maxHealth:N0}");
                }
                else
                {
                    // For NPCs, try to get health using GetStat
                    try
                    {
                        int currentHealth = target.GetStat(Stat.Health);
                        int maxHealth = target.GetStat(Stat.MaxHealth);
                        _targetView.SetHealthText($"{currentHealth:N0} / {maxHealth:N0}");
                    }
                    catch
                    {
                        // If health not available for NPCs, show name only
                        _targetView.SetHealthText("NPC");
                    }
                }
            }
            catch (System.Exception e)
            {
                Chat.WriteLine($"UpdateHealthText Error: {e.Message}");
            }
        }

        private static void UpdateNanoText(Dynel target)
        {
            if (_targetView == null || target == null || !target.IsValid)
                return;

            try
            {
                // Use the same approach as malis-mb-scanner for nano
                if (target is SimpleChar simpleChar)
                {
                    int currentNano = simpleChar.Nano;
                    int maxNano = simpleChar.MaxNano;
                    _targetView.SetNanoText($"{currentNano:N0} / {maxNano:N0}");
                }
                else
                {
                    // For NPCs, try to get nano using GetStat
                    try
                    {
                        int currentNano = target.GetStat(Stat.CurrentNano);
                        int maxNano = target.GetStat(Stat.MaxNanoEnergy);
                        _targetView.SetNanoText($"{currentNano:N0} / {maxNano:N0}");
                    }
                    catch
                    {
                        // If nano not available for NPCs, show N/A
                        _targetView.SetNanoText("N/A");
                    }
                }
            }
            catch (System.Exception e)
            {
                Chat.WriteLine($"UpdateNanoText Error: {e.Message}");
            }
        }

        public override void Teardown()
        {
            _targetWindow?.Dispose();
        }
    }
}