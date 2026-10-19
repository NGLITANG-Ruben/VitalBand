using System.Windows.Input;

namespace VitalBand.App.Views;

// En-tête commun à toutes les pages : logo VitalBand, titre, sous-titre, bouton d'action ou photo de profil
public partial class PageHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(PageHeader), "");
    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(PageHeader), "");
    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(PageHeader), "",
            propertyChanged: (b, _, n) => ((PageHeader)b).HasAction = !string.IsNullOrEmpty(n as string));
    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(PageHeader), null);
    public static readonly BindableProperty ActionFilledProperty =
        BindableProperty.Create(nameof(ActionFilled), typeof(bool), typeof(PageHeader), false);
    public static readonly BindableProperty HasActionProperty =
        BindableProperty.Create(nameof(HasAction), typeof(bool), typeof(PageHeader), false);
    public static readonly BindableProperty ShowAvatarProperty =
        BindableProperty.Create(nameof(ShowAvatar), typeof(bool), typeof(PageHeader), false);
    public static readonly BindableProperty AvatarSourceProperty =
        BindableProperty.Create(nameof(AvatarSource), typeof(ImageSource), typeof(PageHeader), null);
    public static readonly BindableProperty InitialsProperty =
        BindableProperty.Create(nameof(Initials), typeof(string), typeof(PageHeader), "");

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string ActionText { get => (string)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
    public bool ActionFilled { get => (bool)GetValue(ActionFilledProperty); set => SetValue(ActionFilledProperty, value); }
    public bool HasAction { get => (bool)GetValue(HasActionProperty); set => SetValue(HasActionProperty, value); }
    public bool ShowAvatar { get => (bool)GetValue(ShowAvatarProperty); set => SetValue(ShowAvatarProperty, value); }
    public ImageSource? AvatarSource { get => (ImageSource?)GetValue(AvatarSourceProperty); set => SetValue(AvatarSourceProperty, value); }
    public string Initials { get => (string)GetValue(InitialsProperty); set => SetValue(InitialsProperty, value); }

    public PageHeader() => InitializeComponent();
}
