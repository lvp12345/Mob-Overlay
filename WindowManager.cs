using AOSharp.Common.GameData.UI;
using AOSharp.Common.GameData;
using AOSharp.Core.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core;

namespace MobOverlay
{
    internal static class WindowManager
    {
        private static List<PlayerGroupWindow> _playerGroupWindows;
        private static string _playerGroupWindowPath;
        private static string _playerEntryViewPath;
        private static Dictionary<Identity, PlayerEntryView> _playerEntryViews;

        internal static void Init(string playerGroupWindowPath, string playerEntryViewPath)
        {
            _playerGroupWindowPath = playerGroupWindowPath;
            _playerEntryViewPath = playerEntryViewPath;
            _playerGroupWindows = new List<PlayerGroupWindow>();
            _playerEntryViews = new Dictionary<Identity, PlayerEntryView>();

            MultiboxProcessor.ProfessionUpdate += OnProfessionUpdate;
            MultiboxProcessor.GroupUpdate += OnGroupUpdate;
            MultiboxProcessor.SpawnUpdate += OnSpawnUpdate;
            MultiboxProcessor.DespawnUpdate += OnDespawnUpdate;
            MultiboxProcessor.HealthUpdate += OnHealthUpdate;
            MultiboxProcessor.LineOfSightUpdate += OnLineOfSightUpdate;
            MultiboxProcessor.LeaderUpdate += OnLeaderUpdate;
        }

        private static void OnLeaderUpdate(LeaderUpdateArgs args)
        {
            if (!_playerEntryViews.TryGetValue(args.Identity, out var entryView))
                return;

            entryView.IsLeader = args.IsLeader;
            entryView.UpdateBarColor();
        }

        private static void OnLineOfSightUpdate(LineOfSightUpdateArgs args)
        {
            if (!_playerEntryViews.TryGetValue(args.Identity, out var entryView))
                return;

            entryView.IsInLineOfSight = args.IsLineOfSight;
            entryView.UpdateBarColor();
        }


        private static void OnHealthUpdate(HealthUpdateArgs args)
        {
            if (!_playerEntryViews.TryGetValue(args.Identity, out var entryView))
                return;

            entryView.UpdateHealth(args.HealthPercent);
        }

        private static void OnSpawnUpdate(PlayerSpawnArgs args)
        {
            if (_playerEntryViews.ContainsKey(args.Identity))
                return;

            AddNewEntry(args.Identity, args.Name);
        }

        private static PlayerEntryView AddNewEntry(Identity identity, string name)
        {
            Profession prof = DynelManager.Find(identity, out SimpleChar simpleChar) &&
                simpleChar.Profession > Profession.Unknown && simpleChar.Profession <= Profession.Shade ?
                simpleChar.Profession
                : Profession.Unknown;

            var playerEntry = new PlayerEntryView(_playerEntryViewPath, name, prof);
            playerEntry.Selected += OnSelect;
            _playerEntryViews.Add(identity, playerEntry);

            return playerEntry;
        }

        private static void OnSelect(PlayerEntryView selectedEntryView)
        {
            foreach (var entryView in _playerEntryViews)
            {
                if (entryView.Value == selectedEntryView)
                {
                    Targeting.SetTarget(entryView.Key);
                    continue;
                }   

                entryView.Value.Deselect();
            }
        }

        private static void OnDespawnUpdate(PlayerDespawnArgs args)
        {
            if (!_playerEntryViews.TryGetValue(args.Identity, out var entryView))
                return;

            if (args.Index != -1)
                _playerGroupWindows[args.Index].RemoveFromGroup(entryView.Root);
        }

        private static void OnGroupUpdate(PlayerGroupUpdateArgs args)
        {
            if (!_playerEntryViews.TryGetValue(args.Identity, out var entryView))
            {
                entryView = AddNewEntry(args.Identity, args.Name);
            }

            if (args.NewIndex == -1)
            {
                _playerGroupWindows[args.OldIndex].RemoveFromGroup(entryView.Root);
                _playerGroupWindows[args.OldIndex].Root.FitToContents();
                return;
            }

            var expectedWindowsCount = args.NewIndex - _playerGroupWindows.Count() + 1;

            if (expectedWindowsCount > 0)
            {
                for (int i = 0; i < expectedWindowsCount; i++)
                {
                    RegisterWindow();
                }
            }

            if (args.OldIndex != -1)
            {
                _playerGroupWindows[args.OldIndex].RemoveFromGroup(entryView.Root);
                _playerGroupWindows[args.OldIndex].Root.FitToContents();
            }

            _playerGroupWindows[args.NewIndex].AddToGroup(entryView.Root);

            _playerGroupWindows[args.NewIndex].Root.FitToContents();
        }

        private static void OnProfessionUpdate(ProfessionUpdateArgs args)
        {
            if (!_playerEntryViews.TryGetValue(args.Identity, out var entryView))
                return;

            entryView.SetIcon(args.Profession);
        }

        internal static void RegisterWindow()
        {
            var groupWindow = new PlayerGroupWindow($"MobOverlay_{_playerGroupWindows.Count}", _playerGroupWindowPath, _playerEntryViewPath, (WindowStyle)4, WindowFlags.AutoScale | WindowFlags.NoFade);
            groupWindow.Show();
            groupWindow.Window.MoveTo(TargetOverlay.Config.ScreenCoords.X + _playerGroupWindows.Count * 141, TargetOverlay.Config.ScreenCoords.Y);
            _playerGroupWindows.Add(groupWindow);
        }

        internal static void MoveWindows(float startX, float startY)
        {
            for (int i = 0; i < _playerGroupWindows.Count; i++)
                _playerGroupWindows[i].Window.MoveTo(startX + i * 141, startY);
        }

        internal static void UnregisterWindow(int index)
        {
            _playerGroupWindows[index].Window.Close();
        }

        internal static void Dispose()
        {

            foreach (var groupWindow in _playerGroupWindows)
            {
                groupWindow.Dispose();
            }

            _playerGroupWindows.Clear();
            _playerEntryViews.Clear();
        }
    }
}
