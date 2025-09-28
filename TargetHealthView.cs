using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace MobOverlay
{
    internal class TargetHealthView : XmlView
    {
        private PowerBarView _powerBar;
        private BitmapView _charIcon;
        private Button _select;
        private TextView _leftText;
        private TextView _rightText;
        private uint _currentColor;

        internal TargetHealthView(string viewPath) : base(viewPath)
        {
            Root.FindChild("Health", out _powerBar);
            Root.FindChild("Select", out _select);
            Root.FindChild("LeftText", out _leftText);
            Root.FindChild("RightText", out _rightText);
            Root.FindChild("Profession", out _charIcon);

            _leftText.Text = "No Target";
            _rightText.Text = "";

            UpdateBarColor();
            _charIcon.SetBitmap("GFX_GUI_PLANETMAP_PLAYER_MARKER");
        }

        internal void UpdateTarget(SimpleChar target)
        {
            if (target == null || !target.IsValid)
            {
                _leftText.Text = "No Target";
                _rightText.Text = "";
                _powerBar.Value = 0;
                _charIcon.SetBitmap("GFX_GUI_PLANETMAP_PLAYER_MARKER");
                return;
            }

            _leftText.Text = target.Name ?? "Unknown";
            _rightText.Text = $"{target.Health:N0}/{target.MaxHealth:N0}";
            _powerBar.Value = target.HealthPercent;

            if (target.Profession > 0 && (int)target.Profession <= 14)
                _charIcon.SetBitmap($"GFX_GUI_ICON_PROFESSION_{(uint)target.Profession}");
            else
                _charIcon.SetBitmap("GFX_GUI_PLANETMAP_PLAYER_MARKER");
        }

        internal void UpdateBarColor()
        {
            uint color = Color.HealthBar;
            
            if (_currentColor != color)
            {
                _powerBar.SetBarColor(color);
                _currentColor = color;
            }
        }
    }
}
