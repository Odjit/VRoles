using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace VRoles.Services;
class UserService
{
    Dictionary<string, Entity> playerNameToUserEntityCache = new (StringComparer.InvariantCultureIgnoreCase);
    Dictionary<ulong, Entity> platformIdToUserEntityCache = [];

    EntityQuery userQuery;

    public UserService()
    {
        var eqb = new EntityQueryBuilder(Allocator.Temp)
            .AddAll(new(Il2CppType.Of<User>(), ComponentType.AccessMode.ReadOnly));
        userQuery = Core.EntityManager.CreateEntityQuery(ref eqb);
        eqb.Dispose();
    }

    public User GetUser(string playerName)
    {
        if (!playerNameToUserEntityCache.TryGetValue(playerName, out var userEntity))
        {
            RefreshCache();

            if (!playerNameToUserEntityCache.TryGetValue(playerName, out userEntity))
                return User.Empty;
        }
        return userEntity.Read<User>();
    }

    public User GetUser(ulong platformId)
    {
        if (!platformIdToUserEntityCache.TryGetValue(platformId, out var userEntity))
        {
            RefreshCache();

            if (!platformIdToUserEntityCache.TryGetValue(platformId, out userEntity))
                return User.Empty;
        }
        return userEntity.Read<User>();
    }

    void RefreshCache()
    {
        playerNameToUserEntityCache.Clear();
        platformIdToUserEntityCache.Clear();
        var userEntities = userQuery.ToEntityArray(Allocator.Temp);
        foreach (var entity in userEntities)
        {
            var user = entity.Read<User>();
            if (user.LocalCharacter.GetEntityOnServer() == Entity.Null) continue;
            var name = user.LocalCharacter.GetEntityOnServer().Read<PlayerCharacter>().Name;
            playerNameToUserEntityCache.Add(name.Value, entity);
            platformIdToUserEntityCache.Add(user.PlatformId, entity);
        }
    }
}
