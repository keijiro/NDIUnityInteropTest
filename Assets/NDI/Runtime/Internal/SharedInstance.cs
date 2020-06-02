using AssemblyReloadEvents = UnityEditor.AssemblyReloadEvents;

namespace NDI {

static class SharedInstance
{
    static public NdiFind Find => GetFind();

    static bool _initialized;
    static NdiFind _ndiFind;

    static NdiFind GetFind()
    {
        Setup();
        return _ndiFind;
    }

    static void Setup()
    {
        if (_initialized) return;

        AssemblyReloadEvents.beforeAssemblyReload += OnDomainReload;

        _ndiFind = NdiFind.Create();

        _initialized = true;
    }

    static void OnDomainReload()
    {
        _ndiFind?.Dispose();
        _ndiFind = null;

        _initialized = false;
    }
}

}
