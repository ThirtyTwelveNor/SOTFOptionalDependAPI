using RedLoader;
using SonsSdk;
using System.Collections;
using UnityEngine;

namespace OptionalDependAPI;

public class OptionalDependAPI : SonsMod
{
    protected override void OnInitializeMod()
    {
        // Do your early mod initialization which doesn't involve game or sdk references here
        Config.Init();
    }

    protected override void OnSdkInitialized()
    {
        // Do your mod initialization which involves game or sdk references here
        // This is for stuff like UI creation, event registration etc.
        OptionalDependAPIUi.Create();

        // Add in-game settings ui for your mod.
        // SettingsRegistry.CreateSettings(this, null, typeof(Config));


        DelayedScan().RunCoro();
    }

    protected override void OnGameStart()
    {
        // This is called once the player spawns in the world and gains control.
    }

    private static Dictionary<string, List<object>> _apis = new Dictionary<string, List<object>>();
    private static Dictionary<string, List<Action<IAPIAccess>>> _callbacks = new Dictionary<string, List<Action<IAPIAccess>>>();

    public static OptionalDependAPI Instance { get; private set; }
    public OptionalDependAPI()
    {
        Instance = this;
        // Uncomment any of these if you need a method to run on a specific update loop.
        //OnUpdateCallback = MyUpdateMethod;
        //OnLateUpdateCallback = MyLateUpdateMethod;
        //OnFixedUpdateCallback = MyFixedUpdateMethod;
        //OnGUICallback = MyGUIMethod;

        // Uncomment this to automatically apply harmony patches in your assembly.
        //HarmonyPatchAll = true;
    }

    public static void Register(string apiName, object api)
    {
        /* PROVIDER: How to register your API

        public class API
        {
            // Define methods that other mods can use
            public void IpsumMethod(Type arg1, Type arg2, Type arg3)
            {
                // Add your real method implementation here, e.g.:
                IpsumClass.IpsumMethod(arg1, arg2, arg3);
            }
            
            // Add any other methods you want to expose
            public bool IsIpsumActive() => IpsumClass.IsActive;
        }
        public void RegisterAPI()
        {
            // Register your API with the system
            OptionalDependAPI.OptionalDependAPI.Register("IpsumClass", new API());
        }
        */
        if (!_apis.ContainsKey(apiName))
        {
            _apis[apiName] = new List<object>();
        }

        _apis[apiName].Add(api);

        if (_callbacks.TryGetValue(apiName, out var waitingCallbacks))
        {
            var wrapper = new APIAccessWrapper(api);
            foreach (var callback in waitingCallbacks)
            {
                try { callback(wrapper); }
                catch (Exception ex) { RLog.Msg($"Error in callback for {apiName}: {ex.Message}"); }
            }
        }
        /* Example Usage
        public class API
        {
            public void IpsumMethod(Type arg1, Type arg2, Type arg3) => IpsumClass.IpsumMethod(arg1, arg2, arg3);
        }
        public void RegisterAPI()
        {
            OptionalDependAPI.OptionalDependAPI.Register("IpsumClass", new API());
        }
        */
    }

    public static void Subscribe(string apiName, Action<IAPIAccess> callback)
    {
        /* CONSUMER: How to use an optional dependency
        // Add these static fields to your class
        private static OptionalDependAPI.IAPIAccess _ipsumClassAPI;
        private static Action<Type1, Type2, Type3> _ipsumMethod;
        
        // Set up the subscription
        protected override void OnSdkInitialized() 
        {
            // Register for the API
            OptionalDependAPI.OptionalDependAPI.Subscribe("IpsumClass", OnIpsumClassAPI); 
        }
        
        // Callback when the API becomes available
        private static void OnIpsumClassAPI(OptionalDependAPI.IAPIAccess api)
        {
            _ipsumClassAPI = api;
            // Get strongly-typed method references
            _ipsumMethod = api.GetMethod<Action<Type1, Type2, Type3>>("IpsumMethod");
        }
        
        // How to use the API in your code
        public static void YourMethod()
        {
            // Check if API is available before using
            if (_ipsumMethod != null) { return; }

            // Call the method directly
            _ipsumMethod(arg1, arg2, arg3);
            
            // Or use null-conditional invoke to be extra safe
            _ipsumMethod?.Invoke(arg1, arg2, arg3);
            
            // For methods you don't have a delegate for(however less performant)
            _ipsumClassAPI.TryInvoke("AnotherMethod", someArg);
        }
        */
        if (!_callbacks.ContainsKey(apiName))
            _callbacks[apiName] = new List<Action<IAPIAccess>>();

        _callbacks[apiName].Add(callback);

        if (_apis.TryGetValue(apiName, out var apiList))
        {
            foreach (var api in apiList)
            {
                try { callback(new APIAccessWrapper(api)); }
                catch { }
            }
        }

        /* Example usage
        protected override void OnSdkInitialized() 
        {
            OptionalDependAPI.OptionalDependAPI.Subscribe("IpsumClass", OnIpsumClassAPI); 
        }

        private static void OnIpsumClassAPI(OptionalDependAPI.IAPIAccess api)
        {
            _ipsumClassAPI = api
            _ipsumMethod = api.GetMethod<Action<arg1, arg2, arg3>>("IpsumMethod");
        }

        private static IAPIAccess _ipsumClassAPI;
        private static Action<arg1, arg2, arg3> _ipsumMethod;



        anywhere else in code;
        _ipsumMethod(arg1, arg2, arg3);
        or
        _ipsumMethod?.Invoke(arg1, arg2, arg3);
         */
    }

    public static bool TryInvoke(string apiName, string methodName, params object[] args)
    {
        if (!_apis.TryGetValue(apiName, out var apiList) || apiList.Count == 0)
            return false;

        bool success = false;
        foreach (var api in apiList)
        {
            try
            {
                var method = api.GetType().GetMethod(methodName);
                if (method != null)
                {
                    method.Invoke(api, args);
                    success = true;
                }
            }
            catch { }
        }

        return success;

        /* Example usage
        _ipsumClassAPI.TryInvoke("IpsumMethod", arg1, arg2, arg3);
        */
    }

    private IEnumerator DelayedScan()
    {
        yield return new WaitForSeconds(1f);

        foreach (var mod in RedLoader.ModTypeBase<SonsMod>.RegisteredMods)
        {
            if (mod.GetType().GetMethod("RegisterAPI") != null)
            {
                try { mod.GetType().GetMethod("RegisterAPI").Invoke(mod, null); }
                catch (Exception ex) { RLog.Msg($"Error registering API for {mod.GetType().Name}: {ex.Message}"); }
            }
        }
    }

    public static T GetAPI<T>(string apiName) where T : class
    {
        if (_apis.TryGetValue(apiName, out var apiList) && apiList.Count > 0)
        {
            foreach (var api in apiList)
            {
                if (api is T typedApi)
                    return typedApi;
            }
        }
        return null;
    }

    public static List<T> GetAllAPIs<T>(string apiName) where T : class
    {
        List<T> result = new List<T>();

        if (_apis.TryGetValue(apiName, out var apiList))
        {
            foreach (var api in apiList)
            {
                if (api is T typedApi)
                    result.Add(typedApi);
            }
        }

        return result;
    }
}


public interface IAPIAccess
{
    bool TryInvoke(string methodName, params object[] args);
    T GetMethod<T>(string methodName) where T : Delegate;
}

public class APIAccessWrapper : IAPIAccess
{
    private object _api;

    public APIAccessWrapper(object api)
    {
        _api = api;
    }

    public bool TryInvoke(string methodName, params object[] args)
    {
        try
        {
            var method = _api.GetType().GetMethod(methodName);
            if (method != null)
            {
                method.Invoke(_api, args);
                return true;
            }
        }
        catch
        {
            return false;
        }
        return false;
    }

    public T GetMethod<T>(string methodName) where T : Delegate
    {
        try
        {
            var method = _api.GetType().GetMethod(methodName);
            if (method != null)
            {
                return (T)Delegate.CreateDelegate(typeof(T), _api, method);
            }
        }
        catch { }
        return null;
    }
}