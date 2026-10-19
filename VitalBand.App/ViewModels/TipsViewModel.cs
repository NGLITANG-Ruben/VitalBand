using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Models;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class TipsViewModel(HealthService health) : BaseViewModel
{
    private List<HealthTip> _all = new();

    public ObservableCollection<HealthTip> Tips { get; } = new();
    public ObservableCollection<FaqItem> Faq { get; } = new();

    [ObservableProperty] private string filter = "ALL";
    [ObservableProperty] private string dailyTitle = "";
    [ObservableProperty] private string dailyText = "";

    public async Task LoadAsync()
    {
        var content = await health.LoadAsync();
        _all = content.Tips;
        var daily = await health.TipOfTheDayAsync();
        DailyTitle = daily?.Title ?? "";
        DailyText = daily?.Text ?? "";
        Apply();
        if (Faq.Count == 0)
            foreach (var f in content.Faq) Faq.Add(f);
    }

    private void Apply()
    {
        Tips.Clear();
        foreach (var t in _all.Where(t => Filter == "ALL" || t.Category == Filter))
            Tips.Add(t);
    }

    [RelayCommand]
    private void SetFilter(string f)
    {
        Filter = f;
        Apply();
    }

    [RelayCommand]
    private Task BackAsync() => Shell.Current.GoToAsync("..");
}
