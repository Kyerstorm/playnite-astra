using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using Astra.Data;
using Astra.Services;
using Astra.Views;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;

namespace Astra
{
    public class Astra : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("24da3dbe-cf32-4871-a858-f12e0be5cbb7");

        public AstraSettingsViewModel SettingsViewModel { get; }

        public IPlayniteAPI Api => PlayniteApi;

        internal readonly AstraDatabase Database;
        internal readonly SessionTracker SessionTracker;
        internal readonly RecapAggregator RecapAggregator;
        internal readonly RecapExporter RecapExporter;
        internal readonly GameActivityImporter GameActivityImporter;

        public Astra(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties { HasSettings = true };

            SettingsViewModel = new AstraSettingsViewModel(this);

            Database = new AstraDatabase(GetPluginUserDataPath());
            SessionTracker = new SessionTracker(Database);
            RecapAggregator = new RecapAggregator(Database, new PlayniteGameInfoProvider(api));
            RecapExporter = new RecapExporter();
            GameActivityImporter = new GameActivityImporter(Database);
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            try
            {
                SessionTracker.RecordCompletedSession(args.Game.Id, (long)args.ElapsedSeconds);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Astra failed to record a completed session.");
            }
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            yield return new SidebarItem
            {
                Title = "Astra",
                Type = SiderbarItemType.View,
                // Resolved by Playnite relative to the extension's install folder.
                Icon = "icon.png",
                Opened = () => new RecapView
                {
                    DataContext = new RecapViewModel(this, SettingsViewModel.Settings)
                }
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return SettingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunView)
        {
            return new SettingsView { DataContext = SettingsViewModel };
        }

        /// <summary>Root of Playnite's per-plugin data storage, used to find GameActivity's data.</summary>
        internal string GetExtensionsDataRoot()
        {
            return Directory.GetParent(GetPluginUserDataPath()).FullName;
        }
    }
}
