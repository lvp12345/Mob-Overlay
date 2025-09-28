using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Misc;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MobOverlay
{
    internal static class MultiboxProcessor
    {
        private static Dictionary<Identity, PlayerEntry> _playerEntries;
        private static List<PlayerEntry> _playerHistoryBuffer;
        internal static Action<PlayerGroupUpdateArgs> GroupUpdate;
        internal static Action<LeaderUpdateArgs> LeaderUpdate;
        internal static Action<PlayerDespawnArgs> DespawnUpdate;
        internal static Action<PlayerSpawnArgs> SpawnUpdate;
        internal static Action<ProfessionUpdateArgs> ProfessionUpdate;
        internal static Action<HealthUpdateArgs> HealthUpdate;
        internal static Action<LineOfSightUpdateArgs> LineOfSightUpdate;
        private static AutoResetInterval _hpAndLosTick;
        private static AutoResetInterval _groupUpdateTick;
        internal static bool IsRunning { get; private set; }
        internal static double _lastHistoryBufferTick { get; private set; }

        internal static List<PlayerEntry> GetPlayerEntries() => _playerEntries.Values.ToList();

        internal static void Init(int hpAndLosTick = 100, int groupUpdateTick = 1000)
        {
            IsRunning = false;
            _playerEntries = new Dictionary<Identity, PlayerEntry>();
            _hpAndLosTick = new AutoResetInterval(hpAndLosTick);
            _groupUpdateTick = new AutoResetInterval(groupUpdateTick);
            _playerHistoryBuffer = new List<PlayerEntry>();
            _lastHistoryBufferTick = Time.NormalTime;
        }

        internal static void Start()
        {
            if (IsRunning)
                return;

            foreach (var player in DynelManager.Players)
            {
                RegisterPlayer(player.Name, Profession.Unknown, player.Identity);
            }

            IsRunning = true;

            Game.OnUpdate += OnUpdate;
            Network.N3MessageReceived += OnMessageReceived;
        }

        internal static void Stop()
        {
            if (!IsRunning)
                return;

            IsRunning = false;

            Game.OnUpdate -= OnUpdate;
            Network.N3MessageReceived -= OnMessageReceived;

            _playerEntries.Clear();
        }


        private static void OnUpdate(object sender, float e)
        {
            DrawUpdate();

            if (!_hpAndLosTick.Elapsed)
                HealthAndLosUpdate();

            if (_groupUpdateTick.Elapsed)
                UpdateGroups();
        }

        private static void DrawUpdate()
        {
            foreach (var player in DynelManager.Players)
            {
                if (!_playerEntries.TryGetValue(player.Identity, out PlayerEntry playerEntry))
                    continue;

                if (playerEntry.GroupIndex == -1)
                    continue;

                Debug.DrawLine(DynelManager.LocalPlayer.Position, player.Position, DebuggingColor.LightBlue);
            }
        }

        private static void HealthAndLosUpdate()
        {
            foreach (var player in DynelManager.Players)
            {
                if (player == DynelManager.LocalPlayer)
                    continue;

                if (!_playerEntries.TryGetValue(player.Identity, out PlayerEntry playerEntry))
                    continue;

                if (playerEntry.LineOfSight != player.IsInLineOfSight)
                {
                    playerEntry.LineOfSight = player.IsInLineOfSight;

                    LineOfSightUpdate?.Invoke(new LineOfSightUpdateArgs
                    {
                        IsLineOfSight = playerEntry.LineOfSight,
                        Identity = playerEntry.Identity
                    });
                }

                if (playerEntry.HealthPercent != player.HealthPercent)
                {
                    playerEntry.HealthPercent = player.HealthPercent;

                    HealthUpdate?.Invoke(new HealthUpdateArgs
                    {
                        HealthPercent = playerEntry.HealthPercent / 100,
                        Identity = playerEntry.Identity
                    });
                }
            }
        }
        private static void OnMessageReceived(object sender, N3Message msg)
        {
            if (msg is CharDCMoveMessage charDCMoveMsg)
                OnPositionUpdate(charDCMoveMsg);
            else if (msg is DespawnMessage despawnMsg)
                OnDespawn(despawnMsg.Identity);
            else if (msg is SimpleCharFullUpdateMessage fullUpdateMsg)
                OnSpawn(fullUpdateMsg.Name, fullUpdateMsg.Identity);
            else if (msg is InfoPacketMessage infoPacketMsg)
                OnProfessionUpdate(infoPacketMsg);
        }

        internal static void OnSpawn(string name, Identity identity)
        {
            RegisterPlayer(name, Profession.Unknown, identity);
        }

        internal static void OnDespawn(Identity identity)
        {
            if (!_playerEntries.TryGetValue(identity, out var playerEntry))
                return;

            foreach (var player in _playerHistoryBuffer.ToList())
            {
                if (player.Identity != identity)
                    continue;

                _playerHistoryBuffer.Remove(player);
            }

            _playerEntries.Remove(identity);
            DespawnUpdate?.Invoke(new PlayerDespawnArgs
            {
                Identity = identity,
                Index = playerEntry.GroupIndex
            });
        }

        internal static void OnProfessionUpdate(InfoPacketMessage infoPacket)
        {
            if (!(infoPacket.Info is CharacterInfoPacket characterInfoPacket))
                return;

            if (!_playerEntries.TryGetValue(infoPacket.Identity, out var playerCache))
                return;

            if (playerCache.Profession != Profession.Unknown)
                return;

            playerCache.Profession = characterInfoPacket.Profession;

            ProfessionUpdate?.Invoke(new ProfessionUpdateArgs
            {
                Identity = playerCache.Identity,
                Profession = playerCache.Profession
            });
        }

        internal static void OnPositionUpdate(CharDCMoveMessage charDCMove)
        {
            if (charDCMove.MoveType == MovementAction.Update)
                return;

            if (!_playerEntries.TryGetValue(charDCMove.Identity, out var playerEntry))
                return;

            playerEntry.UpdatePosition(charDCMove.Position);

            if (playerEntry.GroupIndex != -1 && Time.NormalTime > _lastHistoryBufferTick + 0.1f)
            {
                _playerHistoryBuffer.Add(playerEntry);
                _lastHistoryBufferTick = Time.NormalTime;
            }
        }

        private static void RegisterPlayer(string name, Profession profession, Identity identity)
        {
            if (TargetOverlay.Config.HiddenUsers.Contains(identity.Instance))
                return;

            var playerCache = new PlayerEntry(name, profession, identity);
          
            _playerEntries.Add(identity, playerCache);
          
            SpawnUpdate?.Invoke(new PlayerSpawnArgs
            {
                Identity = identity,
                Name = name
            });

            Utils.InfoRequest(identity);
        }

        private static void UpdateGroups()
        {
            var groupByDistance = Utils.GroupByDistance(_playerEntries.Values.OrderBy(x => x.Identity.Instance).ToList(), 0.05f);

            if (groupByDistance.Count() == 0)
                return;

            int index = 0;

            List<PlayerEntry> buffer = new List<PlayerEntry>();
         
            foreach (var playerProximityGroup in groupByDistance)
            {
                foreach (var player in playerProximityGroup)
                {
                    if (!_playerEntries.TryGetValue(player.Identity, out var playerEntry))
                        continue;

                    bool indexChanged = playerEntry.GroupIndex != index;
                    buffer.Add(player);

                    if (indexChanged)
                    {
                        GroupIndexUpdate(playerEntry, index);
                        continue;
                    }

                    if (playerProximityGroup.Count(x => x.IsLeader) > 1)
                    {
                        SetLeader(playerEntry, false);
                        continue;
                    }

                    if (_playerHistoryBuffer.Count() == 0)
                        continue;

                    if (_playerHistoryBuffer.Count() < index + 1)
                        continue;

                    var indexBuffer = _playerHistoryBuffer.Where(x => x.GroupIndex == index);

                    if (indexBuffer.Count() < 10)
                        continue;

                    PlayerEntry leader = indexBuffer.GroupBy(x => x).OrderByDescending(g => g.Count()).Select(x => x.Key).FirstOrDefault();

                    if (leader == null)
                        continue;

                    if (leader.Identity == playerEntry.Identity && !playerEntry.IsLeader)
                        SetLeader(playerEntry, true);
                }

                index++;
            }

            foreach (var loner in _playerEntries.Values.Where(x => x.GroupIndex != -1).Except(buffer))
            {
                SetLeader(loner, false);

                GroupUpdate?.Invoke(new PlayerGroupUpdateArgs
                {
                    OldIndex = loner.GroupIndex,
                    NewIndex = -1,
                    Identity = loner.Identity,
                    Name = loner.Name
                });

                loner.GroupIndex = -1;
            }
        }

        private static void SetLeader(PlayerEntry playerEntry, bool isLeader)
        {
            LeaderUpdate?.Invoke(new LeaderUpdateArgs
            {
                Identity = playerEntry.Identity,
                IsLeader = isLeader,
            });

            playerEntry.IsLeader = isLeader;
        }

        private static void GroupIndexUpdate(PlayerEntry playerEntry, int newIndex)
        {
            GroupUpdate?.Invoke(new PlayerGroupUpdateArgs
            {
                OldIndex = playerEntry.GroupIndex,
                NewIndex = newIndex,
                Identity = playerEntry.Identity,
                Name = playerEntry.Name
            });

            _playerHistoryBuffer.RemoveAll(x => x.GroupIndex == playerEntry.GroupIndex);
            _playerHistoryBuffer.RemoveAll(x => x.GroupIndex == newIndex);

            playerEntry.GroupIndex = newIndex;
        }
    }

    internal class LeaderUpdateArgs
    {
        public Identity Identity { get; set; }
        public bool IsLeader { get; set; }
    }

    internal class PlayerGroupUpdateArgs
    {
        public int OldIndex { get; set; }
        public int NewIndex { get; set; }
        public Identity Identity { get; set; }
        public string Name { get; set; }
    }

    internal class PlayerSpawnArgs
    {
        public Identity Identity { get; set; }
        public string Name { get; set; }
    }

    internal class PlayerDespawnArgs
    {
        public Identity Identity { get; set; }
        public int Index { get; set; }
    }

    internal class HealthUpdateArgs
    {
        internal Identity Identity { get; set; }
        internal float HealthPercent { get; set; }
    }

    internal class ProfessionUpdateArgs
    {
        internal Identity Identity { get; set; }
        internal Profession Profession { get; set; }
    }

    internal class HealthPercentUpdateArgs
    {
        internal Identity Identity { get; set; }
        internal float Percentage { get; set; }
    }

    internal class LineOfSightUpdateArgs
    {
        internal Identity Identity { get; set; }
        internal bool IsLineOfSight { get; set; }
    }
}