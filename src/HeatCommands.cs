using CodeHatch.Engine.Core.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using uMod.Libraries;
using uMod.Libraries.Universal;
using uMod.Plugins;
using CommandAttribute = CodeHatch.Engine.Core.Commands.CommandAttribute;

namespace uMod.Heat
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class HeatCommands : ICommandSystem
    {
        #region Initialization

        // The universal provider
        private readonly HeatProvider provider = HeatProvider.Instance;

        // The console player
        private readonly HeatConsolePlayer consolePlayer;

        // Command handler
        private readonly CommandHandler commandHandler;

        // All registered commands
        internal IDictionary<string, RegisteredCommand> registeredCommands;

        // Registered commands
        internal class RegisteredCommand
        {
            /// <summary>
            /// The plugin that handles the command
            /// </summary>
            public readonly Plugin Source;

            /// <summary>
            /// The name of the command
            /// </summary>
            public readonly string Command;

            /// <summary>
            /// The callback
            /// </summary>
            public readonly CommandCallback Callback;

            /// <summary>
            /// The original callback
            /// </summary>
            public CommandAttribute OriginalCallback;

            /// <summary>
            /// Initializes a new instance of the RegisteredCommand class
            /// </summary>
            /// <param name="source"></param>
            /// <param name="command"></param>
            /// <param name="callback"></param>
            public RegisteredCommand(Plugin source, string command, CommandCallback callback)
            {
                Source = source;
                Command = command;
                Callback = callback;
            }
        }

        /// <summary>
        /// Initializes the command system
        /// </summary>
        public HeatCommands()
        {
            registeredCommands = new Dictionary<string, RegisteredCommand>();
            commandHandler = new CommandHandler(CommandCallback, registeredCommands.ContainsKey);
            consolePlayer = new HeatConsolePlayer();
        }

        private bool CommandCallback(IPlayer caller, string cmd, string[] args)
        {
            return registeredCommands.TryGetValue(cmd, out RegisteredCommand command) && command.Callback(caller, cmd, args);
        }

        #endregion Initialization

        #region Command Registration

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            // Convert command to lowercase and remove whitespace
            command = command.ToLowerInvariant().Trim();

            // Set up a new universal command
            RegisteredCommand newCommand = new RegisteredCommand(plugin, command, callback);

            // Check if command can be overridden
            if (!CanOverrideCommand(command))
            {
                throw new CommandAlreadyExistsException(command);
            }

            // Check if command already exists in another universal plugin
            if (registeredCommands.TryGetValue(command, out RegisteredCommand cmd))
            {
                if (cmd.OriginalCallback != null)
                {
                    newCommand.OriginalCallback = cmd.OriginalCallback;
                }

                string newPluginName = plugin?.Name ?? "An unknown plugin"; // TODO: Localization
                string previousPluginName = cmd.Source?.Name ?? "an unknown plugin"; // TODO: Localization
                Interface.uMod.LogWarning($"{newPluginName} has replaced the '{command}' command previously registered by {previousPluginName}"); // TODO: Localization
            }

            // Check if command already exists as a native command
            if (CommandManager.RegisteredCommands.ContainsKey(command))
            {
                if (newCommand.OriginalCallback == null)
                {
                    newCommand.OriginalCallback = CommandManager.RegisteredCommands[command];
                }

                CommandManager.RegisteredCommands.Remove(command);
                if (cmd == null)
                {
                    string newPluginName = plugin?.Name ?? "An unknown plugin"; // TODO: Localization
                    string message = $"{newPluginName} has replaced the '{command}' command previously registered by {provider.GameName.Humanize()}"; // TODO: Localization
                    Interface.uMod.LogWarning(message);
                }
            }

            // Register command
            registeredCommands[command] = newCommand;
            CommandAttribute commandAttribute = new CommandAttribute("/" + command, string.Empty)
            {
                Method = (Action<CommandInfo>)Delegate.CreateDelegate(typeof(Action<CommandInfo>), this,
                    GetType().GetMethod(nameof(HandleCommand), BindingFlags.NonPublic | BindingFlags.Instance)) // TODO: Handle potential NullReferenceException
            };
            CommandManager.RegisteredCommands[command] = commandAttribute;
        }

        private void HandleCommand(CommandInfo cmdInfo)
        {
            if (registeredCommands.TryGetValue(cmdInfo.Label.ToLowerInvariant(), out RegisteredCommand _))
            {
                HandleChatMessage(cmdInfo.Player.IPlayer ?? consolePlayer, cmdInfo.Command);
            }
        }

        #endregion Command Registration

        #region Command Unregistration

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        public void UnregisterCommand(string command, Plugin plugin)
        {
            if (registeredCommands.TryGetValue(command, out RegisteredCommand cmd))
            {
                // Check if the command belongs to the plugin
                if (plugin == cmd.Source)
                {
                    // Remove the chat command
                    registeredCommands.Remove(command);

                    // If this was originally a native Heat command then restore it, otherwise remove it
                    if (cmd.OriginalCallback != null)
                    {
                        CommandManager.RegisteredCommands[cmd.Command] = cmd.OriginalCallback;
                    }
                    else
                    {
                        CommandManager.RegisteredCommands.Remove(cmd.Command);
                    }
                }
            }
        }

        #endregion Command Unregistration

        #region Message Handling

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleChatMessage(IPlayer player, string message) => commandHandler.HandleChatMessage(player, message);

        #endregion Message Handling

        #region Command Overriding

        /// <summary>
        /// Checks if a command can be overridden
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private bool CanOverrideCommand(string command)
        {
            if (!registeredCommands.TryGetValue(command, out RegisteredCommand cmd) || !cmd.Source.IsCorePlugin)
            {
                return !HeatExtension.RestrictedCommands.Contains(command);
            }

            return true;
        }

        #endregion Command Overriding
    }
}
