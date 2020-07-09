using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;
using VRC;
using VRC.Core;

namespace AdvancedSafety
{
    public static class PortalHiding
    {
        public static void OnApplicationStart()
        {
            unsafe
            {
                var originalMethod = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(
                        typeof(ObjectInstantiator).GetMethod(nameof(ObjectInstantiator._InstantiateObject)))
                    .GetValue(null);
                Imports.Hook((IntPtr) (&originalMethod),
                    typeof(PortalHiding).GetMethod(nameof(InstantiateObjectPatch),
                        BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());
                ourDelegate = Marshal.GetDelegateForFunctionPointer<InstantiateObjectDelegate>(originalMethod);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void InstantiateObjectDelegate(IntPtr thisPtr, IntPtr objectName, Vector3 position,
            Quaternion rotation, int networkId, IntPtr player);

        private static InstantiateObjectDelegate ourDelegate;
        
        private static void InstantiateObjectPatch(IntPtr thisPtr, IntPtr objectNamePtr, Vector3 position, Quaternion rotation, int networkId, IntPtr playerPtr)
        {
            try
            {
                ourDelegate(thisPtr, objectNamePtr, position, rotation, networkId, playerPtr);

                if (!AdvancedSafetySettings.HidePortalsFromBlockedUsers && !AdvancedSafetySettings.HidePortalsFromNonFriends || playerPtr == IntPtr.Zero) 
                    return;

                var player = new Player(playerPtr);
                var objectName = IL2CPP.Il2CppStringToManaged(objectNamePtr);

                if (objectName != "Portals/PortalInternalDynamic") return;
                var apiUser = player.prop_APIUser_0;
                if (apiUser == null) return;
                if (APIUser._currentUser?.id == apiUser.id) return;
                if (AdvancedSafetySettings.HidePortalsFromBlockedUsers && IsBlockedEitherWay(apiUser.id) || 
                    AdvancedSafetySettings.HidePortalsFromNonFriends && !APIUser.IsFriendsWith(apiUser.id))
                {
                    var instantiator = new ObjectInstantiator(thisPtr);
                    var dict = instantiator.field_Private_Dictionary_2_Int32_ObjectNPrivateStVeQuGaInStUnique_0;
                    if (dict.ContainsKey(networkId))
                    {
                        var someStruct = dict[networkId];
                        MelonModLogger.Log($"Disabling portal from ͏blocked user or non-friend {apiUser.displayName}");
                        MelonCoroutines.Start(HideGameObjectAfterDelay(someStruct.field_Public_GameObject_0));
                    }
                }
            }
            catch (Exception ex)
            {
                MelonModLogger.LogError($"Exception in portal hider patch: {ex}");
            }
        }

        private static IEnumerator HideGameObjectAfterDelay(GameObject go)
        {
            yield return null; // let it get initialized properly for photon
            yield return null;

            go.SetActive(false);
        }

        private static bool IsBlockedEitherWay(string userId)
        {
            if (userId == null) return false;
            
            var moderationManager = ModerationManager.prop_ModerationManager_0;
            if (moderationManager == null) return false;
            if (APIUser.CurrentUser?.id == userId)
                return false;
            
            foreach (var playerModeration in moderationManager.field_Private_List_1_ApiPlayerModeration_0)
            {
                if (playerModeration != null && playerModeration.moderationType == ApiPlayerModeration.ModerationType.Block && playerModeration.targetUserId == userId)
                    return true;
            }
            
            foreach (var playerModeration in moderationManager.field_Private_List_1_ApiPlayerModeration_1)
            {
                if (playerModeration != null && playerModeration.moderationType == ApiPlayerModeration.ModerationType.Block && playerModeration.targetUserId == userId)
                    return true;
            }
            return false;
            
        }
    }
}