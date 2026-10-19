using CommunityToolkit.Mvvm.ComponentModel;

namespace VitalBand.App.Models;

public class HealthTip
{
    public string Category { get; set; } = "";   // Tension, Glycémie, Mode de vie
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}

public partial class FaqItem : ObservableObject
{
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    [ObservableProperty] private bool isOpen;
}

public class HealthContent
{
    public List<HealthTip> Tips { get; set; } = new();
    public List<FaqItem> Faq { get; set; } = new();
}
