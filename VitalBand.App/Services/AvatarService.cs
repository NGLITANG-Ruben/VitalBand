namespace VitalBand.App.Services;

// Photo de profil : choisie dans la galerie et copiée dans le stockage privé de l'application
public class AvatarService(ApiService api)
{
    private string FilePath => System.IO.Path.Combine(FileSystem.AppDataDirectory, $"avatar_{api.UserId}.jpg");

    public bool Has => api.UserId > 0 && System.IO.File.Exists(FilePath);

    public ImageSource? Load()
    {
        if (!Has) return null;
        var path = FilePath;
        return ImageSource.FromStream(() => System.IO.File.OpenRead(path));
    }

    public async Task<bool> PickAsync()
    {
        var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Choisir une photo" });
        if (photo is null) return false;
        using var source = await photo.OpenReadAsync();
        using var target = System.IO.File.Create(FilePath);
        await source.CopyToAsync(target);
        return true;
    }

    public void Remove()
    {
        if (Has) System.IO.File.Delete(FilePath);
    }
}
