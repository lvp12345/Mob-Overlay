using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace MobOverlay
{
    public class TargetOverlayWindow : XmlWindow
    {
        private TargetHealthView _targetHealthView;
        private string _pluginDirectory;

        public TargetOverlayWindow(string name, string path, WindowStyle windowStyle, WindowFlags flags) : base(name, path, windowStyle, flags)
        {
            _pluginDirectory = System.IO.Path.GetDirectoryName(path);
        }

        protected override void OnWindowCreating()
        {
            try
            {
                // Use exact same approach as malis-mb-scanner PlayerGroupWindow
                Window.FindView("Root", out Root);

                // Create the target health view and add it to the root
                _targetHealthView = new TargetHealthView(_pluginDirectory + "\\TargetHealthView.xml");
                Root.AddChild(_targetHealthView.Root, true);
            }
            catch (Exception e)
            {
                Chat.WriteLine($"TargetOverlayWindow: Error in OnWindowCreating: {e.Message}");
            }
        }

        public void UpdateTarget(SimpleChar target)
        {
            _targetHealthView?.UpdateTarget(target);
        }

        public void Hide()
        {
            if (Window != null)
                Close();
        }

        public new void Show()
        {
            base.Show();
        }

        public void UpdatePosition(int x, int y)
        {
            if (Window != null)
            {
                Window.MoveTo(x, y);
            }
        }

        internal override void Dispose()
        {
            _targetHealthView = null;
            base.Dispose();
        }
    }
}
