using System;
using uMod.Plugins;

namespace uMod.Heat
{
    /// <summary>
    /// Responsible for loading the core Heat plugin
    /// </summary>
    public class HeatPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(Heat) };
    }
}
