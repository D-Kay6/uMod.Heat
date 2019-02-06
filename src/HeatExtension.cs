using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using uMod.Extensions;
using uMod.Plugins;
using uMod.Unity;
using LogType = UnityEngine.LogType;

namespace uMod.Heat
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class HeatExtension : Extension
    {
        // Get assembly info
        internal static Assembly Assembly = Assembly.GetExecutingAssembly();
        internal static AssemblyName AssemblyName = Assembly.GetName();
        internal static VersionNumber AssemblyVersion = new VersionNumber(AssemblyName.Version.Major, AssemblyName.Version.Minor, AssemblyName.Version.Build);
        internal static string AssemblyAuthors = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly, typeof(AssemblyCompanyAttribute), false)).Company;

        /// <summary>
        /// Gets whether this extension is for a specific game
        /// </summary>
        public override bool IsGameExtension => true;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "Heat";

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => AssemblyAuthors;

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => AssemblyVersion;

        /// <summary>
        /// Gets the branch of this extension
        /// </summary>
        public override string Branch => "public"; // TODO: Handle this programmatically

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        /// <summary>
        /// Default game-specific references for use in plugins
        /// </summary>
        public override string[] DefaultReferences => new[]
        {
            "uLink"
        };

        /// <summary>
        /// List of assemblies allowed for use in plugins
        /// </summary>
        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "mscorlib", "System", "System.Core", "uMod", "uLink", "UnityEngine"
        };

        /// <summary>
        /// List of namespaces allowed for use in plugins
        /// </summary>
        public override string[] WhitelistNamespaces => new[]
        {
            "CodeHatch", "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "uLink", "UnityEngine"
        };

        /// <summary>
        /// List of filter matches to apply to console output
        /// </summary>
        public static string[] Filter =
        {
        };

        /// <summary>
        /// Initializes a new instance of the HeatExtension class
        /// </summary>
        /// <param name="manager"></param>
        public HeatExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterPluginLoader(new HeatPluginLoader());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="directory"></param>
        public override void LoadPluginWatchers(string directory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            CSharpPluginLoader.PluginReferences.UnionWith(DefaultReferences);
        }

        private static void HandleLog(string message, string stackTrace, LogType logType) // TODO: Make use of this
        {
            if (!string.IsNullOrEmpty(message) && !Filter.Any(message.Contains))
            {
                Interface.uMod.RootLogger.HandleMessage(message, stackTrace, logType.ToLogType());
            }
        }
    }
}
