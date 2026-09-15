using System;
using System.Windows.Input;
using Astra.Models;
using Playnite.SDK;

namespace Astra.Views
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => execute(parameter);
    }

    public class RecapViewModel : ViewModelBase
    {
        private readonly Astra plugin;
        private readonly AstraSettings settings;

        private int year;
        public int Year
        {
            get => year;
            set
            {
                if (SetValue(ref year, value))
                {
                    settings.LastSelectedYear = value;
                    Refresh();
                }
            }
        }

        private RecapData recap;
        public RecapData Recap
        {
            get => recap;
            private set => SetValue(ref recap, value);
        }

        private string statusMessage;
        public string StatusMessage
        {
            get => statusMessage;
            set => SetValue(ref statusMessage, value);
        }

        public ICommand PreviousYearCommand { get; }
        public ICommand NextYearCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ClearDataCommand { get; }
        public ICommand ImportGameActivityCommand { get; }

        public RecapViewModel(Astra plugin, AstraSettings settings)
        {
            this.plugin = plugin;
            this.settings = settings;

            PreviousYearCommand = new RelayCommand(_ => Year--);
            NextYearCommand = new RelayCommand(_ => Year++, _ => Year < DateTime.Now.Year);
            ExportCommand = new RelayCommand(_ => Export());
            ClearDataCommand = new RelayCommand(_ => ClearData());
            ImportGameActivityCommand = new RelayCommand(_ => ImportGameActivity());

            year = settings.LastSelectedYear > 0 ? settings.LastSelectedYear : DateTime.Now.Year;
            Refresh();
        }

        private void Refresh()
        {
            Recap = plugin.RecapAggregator.BuildRecap(Year);
        }

        private void Export()
        {
            var path = plugin.Api.Dialogs.SaveFile("JSON file|*.json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            plugin.RecapExporter.ExportToFile(Recap, path);
            StatusMessage = $"Recap exported to {path}";
        }

        private void ClearData()
        {
            var result = plugin.Api.Dialogs.ShowMessage(
                "This permanently deletes all of Astra's recorded play sessions. Your Playnite library and playtime are not affected. Continue?",
                "Clear Astra Data",
                System.Windows.MessageBoxButton.YesNo);

            if (result != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }

            plugin.Database.ClearAllData();
            Refresh();
            StatusMessage = "Astra's session history has been cleared.";
        }

        private void ImportGameActivity()
        {
            var importResult = plugin.GameActivityImporter.Import(plugin.GetExtensionsDataRoot());

            if (!importResult.SourceFound)
            {
                StatusMessage = "GameActivity data was not found. Is the GameActivity plugin installed?";
                return;
            }

            StatusMessage = $"Imported {importResult.SessionsImported} session(s) from GameActivity " +
                             $"({importResult.SessionsSkippedDuplicate} already present, " +
                             $"{importResult.FilesFailedToParse.Count} file(s) unreadable).";
            Refresh();
        }
    }
}
