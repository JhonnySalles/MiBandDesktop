using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MiBandDesktop.App.ViewModels;

namespace MiBandDesktop_App.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        ViewModel = new DashboardViewModel();
        this.InitializeComponent();
        this.Loaded += async (s, e) => await ViewModel.RefreshAsync();
    }
}
