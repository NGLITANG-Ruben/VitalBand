using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Models;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class HistoryViewModel(DataService data) : BaseViewModel
{
    public ObservableCollection<MeasurementItem> Items { get; } = new();
    [ObservableProperty] private string filter = "ALL";
    [ObservableProperty] private bool isEmpty;

    public async Task LoadAsync()
    {
        IsBusy = true;
        var (list, error) = await data.GetMeasurementsAsync(Filter == "ALL" ? null : Filter);
        var (th, _) = await data.GetThresholdsAsync();
        IsBusy = false;

        if (list is null)
        {
            _ = ShowToastAsync(error ?? "Une erreur est survenue.");
            return;
        }
        IReadOnlyList<Threshold> t = th ?? new List<Threshold>();
        Items.Clear();
        foreach (var m in list) Items.Add(MeasurementItem.From(m, t));
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task SetFilterAsync(string f)
    {
        Filter = f;
        await LoadAsync();
    }

    [RelayCommand]
    private Task OpenAsync(MeasurementItem item) =>
        Shell.Current.GoToAsync("entry", new Dictionary<string, object> { ["item"] = item.Source });

    [RelayCommand]
    private Task NewAsync() => Shell.Current.GoToAsync("entry");
}
