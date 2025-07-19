using SteamKit2;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GamesSizeCalculator.SteamSizeCalculation;

public class SteamApiClient : IDisposable, ISteamApiClient
{
    SteamClient steamClient;
    CallbackManager manager;
    SteamUser steamUser;
    SteamApps steamApps;

    private bool isRunning = false;

    public bool IsConnected { get; private set; } = false;

    public bool IsLoggedIn { get; private set; } = false;

    public DateTime LastUsed { get; private set; } = DateTime.Now;

    public SteamApiClient()
    {
        steamClient = new SteamClient();
        manager = new CallbackManager(steamClient);
        steamUser = steamClient.GetHandler<SteamUser>();
        steamApps = steamClient.GetHandler<SteamApps>();
        manager.Subscribe<SteamClient.ConnectedCallback>(onConnected);
        manager.Subscribe<SteamClient.DisconnectedCallback>(onDisconnected);
        manager.Subscribe<SteamUser.LoggedOnCallback>(onLoggedOn);
        manager.Subscribe<SteamUser.LoggedOffCallback>(onLoggedOff);
    }

    private AutoResetEvent onConnectedEvent = new(false);
    private EResult onConnectedResult;
    private void onConnected(SteamClient.ConnectedCallback callback)
    {
        onConnectedResult = callback.Result;
        onConnectedEvent.Set();
    }

    private AutoResetEvent onDisconnectedEvent = new(false);
    private void onDisconnected(SteamClient.DisconnectedCallback callback)
    {
        isRunning = false;
        onDisconnectedEvent.Set();
    }

    private AutoResetEvent onLoggedOnEvent = new(false);
    private EResult onLoggedOnResult;
#pragma warning disable CS1998
    private async void onLoggedOn(SteamUser.LoggedOnCallback callback)
#pragma warning restore CS1998
    {
        onLoggedOnResult = callback.Result;
        onLoggedOnEvent.Set();
    }

    private AutoResetEvent onLoggedOffEvent = new(false);
    private void onLoggedOff(SteamUser.LoggedOffCallback callback)
    {
        onLoggedOffEvent.Set();
    }

    public async Task<EResult> Connect()
    {
        steamClient.Connect();
        isRunning = true;
        var result = EResult.OK;

#pragma warning disable CS4014
        Task.Run(() =>
        {
            while (isRunning)
            {
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        });
#pragma warning restore CS4014

        await Task.Run(() =>
        {
            onConnectedEvent.WaitOne(10000);
            if (onConnectedResult != EResult.OK)
            {
                this.IsConnected = false;
                result = onConnectedResult;
            }
            else
            {
                this.IsConnected = true;
            }
        });

        return result;
    }

    public async Task<EResult> Login()
    {
        var result = EResult.OK;
        steamUser.LogOnAnonymous();

        await Task.Run(() =>
        {
            onLoggedOnEvent.WaitOne(10000);
            if (onLoggedOnResult != EResult.OK)
            {
                this.IsLoggedIn = false;
                result = onLoggedOnResult;
            }
            else
            {
                this.IsLoggedIn = true;
            }
        });

        return result;
    }

    public void Logout()
    {
        steamClient.Disconnect();
        IsConnected = false;
        IsLoggedIn = false;
        isRunning = false;
    }

    private async Task PrepareCall()
    {
        if (!IsConnected || !steamClient.IsConnected)
        {
            var connect = await Connect();
            if (connect != EResult.OK)
            {
                connect = await Connect();
                if (connect != EResult.OK)
                {
                    throw new Exception("Failed to connect to Steam " + connect);
                }
            }

            IsConnected = true;
        }

        if (!IsLoggedIn)
        {
            var logon = await Login();
            if (logon != EResult.OK)
            {
                throw new Exception("Failed to logon to Steam " + logon);
            }

            IsLoggedIn = true;
        }
    }

    public async Task<KeyValue> GetProductInfo(uint id)
    {
        await PrepareCall();

        try
        {
            SteamApps.PICSProductInfoCallback productInfo;
            AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet resultSet = null;
            var productJob = steamApps.PICSGetProductInfo(id, package: null, onlyPublic: false);

            // Workardound for rare case where PICSGetProductInfo would get stuck if there's some issue with computer's network.
            // For example if PC is woken up from sleep.
            var tsk = productJob.ToTask();
            if (tsk.Wait(10000))
            {
                resultSet = tsk.Result;
            }
            else
            {
                throw new Exception("Failed to get product info for app (timeout) " + id);
            }

            if (resultSet.Complete)
            {
                productInfo = resultSet.Results.First();
            }
            else
            {
                productInfo = resultSet.Results.FirstOrDefault(prodCallback => prodCallback.Apps.ContainsKey(id));
            }

            if (productInfo == null)
            {
                throw new Exception("Failed to get product info for app " + id);
            }

            LastUsed = DateTime.Now;

            return productInfo.Apps[id].KeyValues;
        }
        catch (Exception e)
        {
            throw new Exception("Failed to get product info for app " + id + ". " + e.Message, e);
        }
    }

    public void Dispose()
    {
        Logout();
    }
}
