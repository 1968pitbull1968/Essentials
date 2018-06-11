﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Torch.Commands;
using NLog;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using Vector3D = VRageMath.Vector3D;

namespace Essentials.Commands
{
    [Category("cleanup")]
    public class CleanupModule : CommandModule
    {
        [Command("scan", "Find grids matching the given conditions: hastype, notype, hassubtype, nosubtype, blockslessthan, blocksgreaterthan, ownedby")]
        public void Scan()
        {
            var count = ScanConditions(Context.Args).Count();
            Context.Respond($"Found {count} grids matching the given conditions.");
        }

        [Command("list", "Lists grids matching the given conditions: hastype, notype, hassubtype, nosubtype, blockslessthan, blocksgreaterthan, ownedby")]
        public void List()
        {
            var grids = ScanConditions(Context.Args).OrderBy(g => g.DisplayName).ToList();
            Context.Respond(String.Join("\n", grids.Select((g, i) => $"{i + 1}. {grids[i].DisplayName} ({grids[i].BlocksCount} block(s))")));
            Context.Respond($"Found {grids.Count} grids matching the given conditions.");
        }

        [Command("delete", "Delete grids matching the given conditions")]
        public void Delete()
        {
            var count = 0;
            foreach (var grid in ScanConditions(Context.Args))
            {
                EssentialsPlugin.Log.Info($"Deleting grid: {grid.EntityId}: {grid.DisplayName}");
                grid.Close();
                count++;
            }

            Context.Respond($"Deleted {count} grids matching the given conditions.");
            EssentialsPlugin.Log.Info($"Cleanup deleted {count} grids matching conditions {string.Join(", ", Context.Args)}");
        }

        private IEnumerable<MyCubeGrid> ScanConditions(IReadOnlyList<string> args)
        {
            var conditions = new List<Func<MyCubeGrid, bool>>();

            for (var i = 0; i < args.Count; i += 2)
            {
                if (i + 1 > args.Count)
                    break;

                var arg = args[i];
                var parameter = args[i + 1];

                switch (arg)
                {
                    case "hastype":
                        conditions.Add(g => g.HasBlockType(parameter));
                        break;
                    case "notype":
                        conditions.Add(g => !g.HasBlockType(parameter));
                        break;
                    case "hassubtype":
                        conditions.Add(g => g.HasBlockSubtype(parameter));
                        break;
                    case "nosubtype":
                        conditions.Add(g => !g.HasBlockSubtype(parameter));
                        break;
                    case "blockslessthan":
                        conditions.Add(g => BlocksLessThan(g, parameter));
                        break;
                    case "blocksgreaterthan":
                        conditions.Add(g => BlocksGreaterThan(g, parameter));
                        break;
                    case "ownedby":
                        conditions.Add(g => OwnedBy(g, parameter));
                        break;
                    case "name":
                        conditions.Add(g => NameMatches(g, parameter));
                        break;
                    case "nopower":
                        conditions.Add(g=>!HasPower(g));
                        break;
                    case "haspower":
                        conditions.Add(g => HasPower(g));
                        break;
                    case "insideplanet":
                        conditions.Add(g => InsidePlanet(g));
                        break;
                    default:
                        Context.Respond($"Unknown argument '{arg}'");
                        yield break;
                }
            }

            foreach (var group in MyCubeGridGroups.Static.Logical.Groups)
            {
                if (group.Nodes.All(grid => conditions.TrueForAll(func => func(grid.NodeData))))
                    foreach (var grid in group.Nodes)
                        yield return grid.NodeData;
            }
        }

        private bool NameMatches(MyCubeGrid grid, string str)
        {
            var regex = new Regex(str);
            return regex.IsMatch(grid.DisplayName ?? "");
        }

        private bool BlocksLessThan(MyCubeGrid grid, string str)
        {
            if (int.TryParse(str, out int count))
                return grid.BlocksCount < count;

            return false;
        }

        private bool BlocksGreaterThan(MyCubeGrid grid, string str)
        {
            if (int.TryParse(str, out int count))
                return grid.BlocksCount > count;

            return false;
        }

        private bool HasPower(MyCubeGrid grid)
        {
            foreach (var b in grid.GetFatBlocks())
            {
                var c = b.Components?.Get<MyResourceSourceComponent>();
                if (c == null)
                    continue;

                if (c.HasCapacityRemainingByType(MyResourceDistributorComponent.ElectricityId) && c.ProductionEnabledByType(MyResourceDistributorComponent.ElectricityId))
                    return true;
            }

            return false;
        }

        private bool InsidePlanet(MyCubeGrid grid)
        {
            var s = grid.PositionComp.WorldVolume;
            var voxels = new List<MyVoxelBase>();
            MyGamePruningStructure.GetAllVoxelMapsInSphere(ref s, voxels);

            if (!voxels.Any())
                return false;

            foreach (var v in voxels)
            {
                var planet = v as MyPlanet;
                if (planet == null)
                    continue;

                var dist2center = Vector3D.DistanceSquared(s.Center, planet.PositionComp.WorldVolume.Center);
                if (dist2center <= (planet.MaximumRadius * planet.MaximumRadius) / 2)
                    return true;
            }

            return false;
        }

        private bool OwnedBy(MyCubeGrid grid, string str)
        {
            long identityId;

            if (string.Compare(str, "nobody", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return grid.BigOwners.Count == 0;
            }

            if (string.Compare(str, "pirates", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                identityId = MyPirateAntennas.GetPiratesId();
            }
            else
            {
                var player = Utilities.GetPlayerByNameOrId(str);
                if (player == null)
                    return false;

                identityId = player.IdentityId;
            }

            return grid.BigOwners.Contains(identityId);
        }
    }
}
