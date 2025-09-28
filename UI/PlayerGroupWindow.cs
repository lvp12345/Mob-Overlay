using AOSharp.Common.GameData.UI;
using AOSharp.Core.UI;
using System;

namespace MobOverlay
{
    internal class PlayerGroupWindow : XmlWindow
    {
        internal PlayerGroupWindow(string name, string windowPath, string playerEntryViewPath, WindowStyle windowStyle = WindowStyle.Default, WindowFlags flags = WindowFlags.AutoScale | WindowFlags.NoFade) : base(name, windowPath, windowStyle, flags)
        {
        }

        internal void RemoveFromGroup(View view)
        {
            Root.RemoveChild(view);
            view.FitToContents();
        }

        internal void AddToGroup(View view)
        {
            Root.AddChild(view, true);
            view.FitToContents();
        }

        protected override void OnWindowCreating()
        {
            try
            {
                Window.FindView("Root", out Root);
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        internal override void Show()
        {
            base.Show();
        }

        internal override void Dispose()
        {
            Root.DeleteAllChildren();
            base.Dispose();
        }
    }
}