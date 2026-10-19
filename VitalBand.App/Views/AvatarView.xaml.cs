using Microsoft.Maui.Controls.Shapes;

namespace VitalBand.App.Views;

public partial class AvatarView : ContentView
{
    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(AvatarView), null,
            propertyChanged: (b, _, n) =>
            {
                var v = (AvatarView)b;
                v.HasImage = n != null;
                v.ShowInitials = n == null;
            });
    public static readonly BindableProperty InitialsProperty =
        BindableProperty.Create(nameof(Initials), typeof(string), typeof(AvatarView), "");
    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(AvatarView), 48.0,
            propertyChanged: (b, _, _) => ((AvatarView)b).Apply());
    public static readonly BindableProperty HasImageProperty =
        BindableProperty.Create(nameof(HasImage), typeof(bool), typeof(AvatarView), false);
    public static readonly BindableProperty ShowInitialsProperty =
        BindableProperty.Create(nameof(ShowInitials), typeof(bool), typeof(AvatarView), true);

    public ImageSource? Source { get => (ImageSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public string Initials { get => (string)GetValue(InitialsProperty); set => SetValue(InitialsProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public bool HasImage { get => (bool)GetValue(HasImageProperty); set => SetValue(HasImageProperty, value); }
    public bool ShowInitials { get => (bool)GetValue(ShowInitialsProperty); set => SetValue(ShowInitialsProperty, value); }

    public AvatarView()
    {
        InitializeComponent();
        Apply();
    }

    private void Apply()
    {
        Box.WidthRequest = Size;
        Box.HeightRequest = Size;
        Box.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(Size / 2) };
        InitialsLabel.FontSize = Size * 0.38;
    }
}
