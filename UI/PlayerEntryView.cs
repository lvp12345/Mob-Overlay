using AOSharp.Common.GameData;
using AOSharp.Core.UI;
using System;

namespace MobOverlay
{
    internal class PlayerEntryView : XmlView
    {
        internal int GroupIndex;
        internal bool IsInLineOfSight;
        internal bool IsLeader;
        internal Action<PlayerEntryView> Selected;

        private readonly PowerBarView _powerBar;
        private readonly PowerBarView _nanoBar;
        private readonly View _select;
        private readonly TextView _targetName;
        private readonly TextView _healthText;
        private readonly TextView _nanoText;
        private bool _isSelected;
        private uint _currentColor = 0;

        internal PlayerEntryView(string viewPath, string playerName, Profession profession) : base(viewPath)
        {
            try
            {
                Root.FindChild("Health", out _powerBar);
                Root.FindChild("Nano", out _nanoBar);
                Root.FindChild("Select", out _select);
                Root.FindChild("TargetName", out _targetName);
                Root.FindChild("HealthText", out _healthText);
                Root.FindChild("NanoText", out _nanoText);

                GroupIndex = -1;
                IsInLineOfSight = true;
                IsLeader = false;
                _isSelected = false;
                Selected = null;

                if (_targetName != null)
                    _targetName.Text = playerName ?? "Unknown";
                if (_healthText != null)
                    _healthText.Text = "";
                if (_nanoText != null)
                    _nanoText.Text = "";

                UpdateBarColor();
            }
            catch (System.Exception e)
            {
                Chat.WriteLine($"PlayerEntryView constructor error: {e.Message}");
                throw;
            }
        }

        internal void UpdateHealth(float percent)
        {
            try
            {
                if (_powerBar != null)
                    _powerBar.Value = Math.Max(0f, Math.Min(1f, percent));
            }
            catch (System.Exception e)
            {
                Chat.WriteLine($"UpdateHealth error: {e.Message}");
            }
        }

        internal void SetHealthText(string healthText)
        {
            try
            {
                if (_healthText != null)
                    _healthText.Text = healthText ?? "";
            }
            catch (System.Exception e)
            {
                Chat.WriteLine($"SetHealthText error: {e.Message}");
            }
        }

        internal void UpdateNano(float percent)
        {
            try
            {
                if (_nanoBar != null)
                    _nanoBar.Value = Math.Max(0f, Math.Min(1f, percent));
            }
            catch (System.Exception e)
            {
                Chat.WriteLine($"UpdateNano error: {e.Message}");
            }
        }

        internal void SetNanoText(string nanoText)
        {
            try
            {
                if (_nanoText != null)
                    _nanoText.Text = nanoText ?? "";
            }
            catch (System.Exception e)
            {
                Chat.WriteLine($"SetNanoText error: {e.Message}");
            }
        }
        private void OnSelectClick(object sender, View e)
        {
            _targetName.SetColor(Color.TextSelected);
            _healthText.SetColor(Color.TextSelected);
            _nanoText.SetColor(Color.TextSelected);
            _isSelected = true;
            Selected?.Invoke(this);
        }

        internal void Deselect()
        {
            if (!_isSelected)
                return;

            _targetName.SetColor(Color.TextDefault);
            _healthText.SetColor(Color.TextDefault);
            _nanoText.SetColor(Color.TextDefault);
        }

        internal void SetIcon(Profession profession)
        {
            // Icon removed - no longer used
        }

        internal void UpdateBarColor()
        {
            uint color;

            if (IsInLineOfSight)
            {
                color = IsLeader ? Color.HealthBarLeader : Color.HealthBar;
            }
            else
            {
                color = IsLeader ? Color.HealthBarLeaderNoLos : Color.HealthBarNoLos;
            }

            if (_currentColor != color)
            {
                _powerBar.SetBarColor(color);
                _nanoBar.SetBarColor(0xFF0080FF); // Blue color for nano
            }
        }
    }
}