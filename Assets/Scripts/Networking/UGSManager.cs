using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class UGSManager : MonoBehaviour
{
    private static Task initialization;
    public static Task EnsureReady()
    {
        if (initialization == null || initialization.IsFaulted || initialization.IsCanceled)
            initialization = Initialize();
        return initialization;
    }
    private static async Task Initialize()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    // Services are initialized on demand when opening an online room.
}
