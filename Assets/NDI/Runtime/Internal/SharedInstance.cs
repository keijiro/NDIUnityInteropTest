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

    #if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnDomainReload;
    #endif

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
