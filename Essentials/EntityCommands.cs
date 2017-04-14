﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Essentials
{
    [Category("entity")]
    public class EntityCommands : CommandModule
    {
        [Command("stop", "Stops an entity from moving")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Stop(string entityName)
        {
            if (!Utilities.TryGetEntityByNameOrId(entityName, out IMyEntity entity))
            {
                Context.Respond($"Entity '{entityName}' not found.");
                return;
            }

            entity.Physics?.ClearSpeed();
            Context.Respond($"Entity '{entity.DisplayName}' stopped");
        }

        [Command("delete", "Delete an entity.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Delete(string entityName)
        {
            var name = entityName;
            if (string.IsNullOrEmpty(name))
                return;

            if (!Utilities.TryGetEntityByNameOrId(name, out IMyEntity entity))
            {
                Context.Respond($"Entity '{name}' not found.");
                return;
            }

            if (entity is IMyCharacter)
            {
                Context.Respond("You cannot delete characters.");
                return;
            }

            entity.Close();
            Context.Respond($"Entity '{entity.DisplayName}' deleted");
        }

        [Command("find", "Find entities with the given text in their name.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Find(string name)
        {
            var search = name;
            if (string.IsNullOrEmpty(search))
                return;

            var sb = new StringBuilder("Found entities:\n");
            foreach (var entity in MyEntities.GetEntities())
            {
                if (entity is IMyVoxelBase voxel && voxel.StorageName.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                    sb.AppendLine($"{voxel.StorageName} ({entity.EntityId})");
                else if (entity.DisplayName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    //This can be null??? :keen:
                    sb.AppendLine($"{entity.DisplayName} ({entity.EntityId})");
            }

            Context.Respond(sb.ToString());
        }

        [Command("tp", "Teleport to another entity or teleport another entity to you.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Teleport(string destination, string entityToMove = null)
        {
            /*
            IMyEntity targetEntity;
            IMyEntity destEntity;
            switch (Context.Args.Count)
            {
                case 1:
                    targetEntity = Context.Player.Controller.ControlledEntity.Entity;
                    Utilities.TryGetEntityByNameOrId(Context.Args[0], out destEntity);
                    break;
                case 2:
                    Utilities.TryGetEntityByNameOrId(Context.Args[0], out targetEntity);
                    Utilities.TryGetEntityByNameOrId(Context.Args[1], out destEntity);
                    break;
                default:
                    Context.Respond("Wrong number of arguments.");
                    return;
            }*/

            Utilities.TryGetEntityByNameOrId(destination, out IMyEntity destEntity);

            IMyEntity targetEntity;
            if (string.IsNullOrEmpty(entityToMove))
                targetEntity = Context.Player.Controller.ControlledEntity.Entity;
            else
                Utilities.TryGetEntityByNameOrId(entityToMove, out targetEntity);

            if (targetEntity == null)
            {
                Context.Respond("Target entity not found.");
                return;
            }

            if (destEntity == null)
            {
                Context.Respond("Destination entity not found");
                return;
            }

            var targetPos = MyEntities.FindFreePlace(destEntity.GetPosition(), (float)targetEntity.WorldAABB.Extents.Max());
            if (targetPos == null)
            {
                Context.Respond("No free place to teleport.");
                return;
            }

            targetEntity.SetPosition(targetPos.Value);
        }
    }
}
