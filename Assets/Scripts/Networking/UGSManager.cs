using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;

public class UGSManager : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            // Initialize Unity Gaming Services
            await UnityServices.InitializeAsync();

            // Sign in anonymously if not already signed in
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log("Unity Services Ready!");
            Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);
        }
        catch (Exception e)
        {
            Debug.LogError("UGS Initialization Failed: " + e.Message);
        }
    }
}