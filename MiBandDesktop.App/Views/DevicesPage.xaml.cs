using Microsoft.UI.Xaml.Controls;
using MiBandDesktop.App.ViewModels;

namespace MiBandDesktop_App.Views;

public sealed partial class DevicesPage : Page
{
    public DevicesViewModel ViewModel { get; }

    public DevicesPage()
    {
        ViewModel = new DevicesViewModel();
        this.InitializeComponent();
        this.Loaded += async (s, e) => await ViewModel.LoadSavedDevicesAsync();
    }
}
