namespace VitalBand.App.Views;

public partial class FormField : ContentView
{
    public static readonly BindableProperty CaptionProperty =
        BindableProperty.Create(nameof(Caption), typeof(string), typeof(FormField), "");
    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FormField), "");
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FormField), "", BindingMode.TwoWay);
    public static readonly BindableProperty IsPasswordProperty =
        BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(FormField), false,
            propertyChanged: (b, _, n) => { var f = (FormField)b; f.IsHidden = (bool)n; f.ToggleText = "Afficher"; });
    public static readonly BindableProperty IsHiddenProperty =
        BindableProperty.Create(nameof(IsHidden), typeof(bool), typeof(FormField), false);
    public static readonly BindableProperty ToggleTextProperty =
        BindableProperty.Create(nameof(ToggleText), typeof(string), typeof(FormField), "Afficher");
    public static readonly BindableProperty InputKeyboardProperty =
        BindableProperty.Create(nameof(InputKeyboard), typeof(Keyboard), typeof(FormField), Keyboard.Default);
    public static readonly BindableProperty HasErrorProperty =
        BindableProperty.Create(nameof(HasError), typeof(bool), typeof(FormField), false);
    public static readonly BindableProperty ErrorProperty =
        BindableProperty.Create(nameof(Error), typeof(string), typeof(FormField), "",
            propertyChanged: (b, _, n) => ((FormField)b).OnErrorChanged(n as string));

    public string Caption { get => (string)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }
    public Keyboard InputKeyboard { get => (Keyboard)GetValue(InputKeyboardProperty); set => SetValue(InputKeyboardProperty, value); }
    public bool IsHidden { get => (bool)GetValue(IsHiddenProperty); set => SetValue(IsHiddenProperty, value); }
    public string ToggleText { get => (string)GetValue(ToggleTextProperty); set => SetValue(ToggleTextProperty, value); }
    public bool HasError { get => (bool)GetValue(HasErrorProperty); set => SetValue(HasErrorProperty, value); }
    public string Error { get => (string)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    public FormField() => InitializeComponent();

    private void OnToggleTapped(object? sender, TappedEventArgs e)
    {
        IsHidden = !IsHidden;
        ToggleText = IsHidden ? "Afficher" : "Masquer";
    }

    private void OnErrorChanged(string? value)
    {
        var has = !string.IsNullOrEmpty(value);
        HasError = has;
        Box.Stroke = new SolidColorBrush(Color.FromArgb(has ? "#E5173F" : "#0F6B5E"));
    }
}
