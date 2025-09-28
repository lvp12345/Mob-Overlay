using AOSharp.Core.UI;

namespace MobOverlay
{
    internal class PlayerGroupView :XmlView
    {
        private readonly View _groupRoot;

        internal PlayerGroupView(string viewPath, string groupName) : base(viewPath)
        {
            Root.FindChild("GroupRoot", out _groupRoot);
            Root.FindChild("GroupName", out TextView groupNameView);
            groupNameView.Text = groupName;
        }

        internal void RemoveFromGroup(View view)
        {
            _groupRoot.RemoveChild(view);
            view.FitToContents();
            _groupRoot.FitToContents();
        }

        internal void AddToGroup(View view)
        {
            _groupRoot.AddChild(view, true);
            view.FitToContents();
            _groupRoot.FitToContents();
        }
    }
}