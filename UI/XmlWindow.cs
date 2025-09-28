using AOSharp.Common.GameData.UI;
using AOSharp.Core.UI;

namespace MobOverlay
{
    internal abstract class XmlWindow : AOSharpWindow
    {
        internal View Root;

        internal XmlWindow(string name, string path, WindowStyle windowStyle, WindowFlags flags) : base(name, path, windowStyle, flags)
        {
        }

        internal void Toggle()
        {
            if (Window == null)
                Show();
            else
                Dispose();
        }

        internal new virtual void Show()
        {
            base.Show();
        }

        internal virtual void Dispose()
        {
            Close();
        }
    }

    internal abstract class XmlView 
    {
        public View Root;

        public XmlView(string viewPath)
        {
            Root = View.CreateFromXml(viewPath);
        }
    }
}
