using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Interfaces;
using AOSharp.Core;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MobOverlay
{
    internal class Utils
    {
        internal static IEnumerable<List<PlayerEntry>> GroupByDistance(IEnumerable<PlayerEntry> simpleChars, float threshold)
        {
            var groups = new List<List<PlayerEntry>>();

            foreach (var simpleChar1 in simpleChars)
            {
                var group = new List<PlayerEntry>();

                foreach (var simpleChar2 in simpleChars)
                {
                    if (!PositionHistoryDistanceCheck(simpleChar1, simpleChar2, 0.05f))
                        continue;

                    if (groups.SelectMany(x => x).Any(x => x.Identity == simpleChar1.Identity || x.Identity == simpleChar2.Identity))
                        continue;

                    if (!group.Any(x=>x.Identity == simpleChar1.Identity))
                        group.Add(simpleChar1);

                    if (!group.Any(x => x.Identity == simpleChar2.Identity))
                        group.Add(simpleChar2);  
                }

                groups.Add(group);
            }

            return groups.Where(x => x.Count() > 1);
        }

        private static bool PositionHistoryDistanceCheck(PlayerEntry cache1, PlayerEntry cache2, float threshold)
        {
            if (cache1.Identity == cache2.Identity)
                return false;

            foreach (var pos1 in cache1.PositionHistory)
            {
                foreach (var pos2 in cache2.PositionHistory)
                {
                    if (Vector3.Distance(pos1, pos2) <= threshold)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static void InfoRequest(Identity target)
        {
            Network.Send(new CharacterActionMessage()
            {
                Unknown = 0,
                Action = CharacterActionType.InfoRequest,
                Identity = DynelManager.LocalPlayer.Identity,
                Target = target,
            });
        }

        internal static void LoadCustomTextures(string path, int startId)
        {
            DirectoryInfo textureDir = new DirectoryInfo(path);

            foreach (var file in textureDir.GetFiles("*.png").OrderBy(x => x.Name))
            {
                GuiResourceManager.CreateGUITexture(file.Name.Replace(".png", "").Remove(0, 4), startId++, file.FullName);
            }
        }
    }
}
