using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MiBandDesktop_App.Views;

namespace MiBandDesktop_App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        MiBandDesktop.App.Services.WindowIconHelper.SetWindowIcon(this, "AppIcon.ico");

        this.SizeChanged += MainWindow_SizeChanged;
        AppWindow.Changed += AppWindow_Changed;

        RestorePaneState();
    }

    private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
    {
        if (args.DidPresenterChange || args.DidSizeChange)
        {
            TriggerLayoutRefresh();
        }
    }

    private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        TriggerLayoutRefresh();
    }

    private void TriggerLayoutRefresh()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                NavView?.InvalidateMeasure();
                NavView?.InvalidateArrange();
                NavView?.UpdateLayout();

                RootFrame?.InvalidateMeasure();
                RootFrame?.InvalidateArrange();
                RootFrame?.UpdateLayout();

                if (RootFrame?.Content is FrameworkElement page)
                {
                    page.InvalidateMeasure();
                    page.InvalidateArrange();
                    page.UpdateLayout();
                }
            }
            catch { }
        });
    }

    private static string PaneStateFilePath => System.IO.Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
        "MiBandDesktop",
        "pane_state.txt"
    );

    private void RestorePaneState()
    {
        try
        {
            if (System.IO.File.Exists(PaneStateFilePath))
            {
                var content = System.IO.File.ReadAllText(PaneStateFilePath);
                if (bool.TryParse(content, out var isOpen))
                {
                    NavView.IsPaneOpen = isOpen;
                }
            }
        }
        catch { }
    }

    private void SavePaneState(bool isOpen)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(PaneStateFilePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(PaneStateFilePath, isOpen.ToString());
        }
        catch { }
    }

    private void NavView_PaneOpened(NavigationView sender, object args) => SavePaneState(true);
    private void NavView_PaneClosed(NavigationView sender, object args) => SavePaneState(false);

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        NavView.SelectedItem = NavView.MenuItems[0];
        NavigateTo(typeof(DashboardPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavigateTo(typeof(SettingsPage));
        }
        else if (args.SelectedItemContainer != null)
        {
            var tag = args.SelectedItemContainer.Tag?.ToString();
            HandleNavigation(tag);
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked || (sender.SettingsItem is NavigationViewItemBase settingsItem && args.InvokedItemContainer == settingsItem))
        {
            NavigateTo(typeof(SettingsPage));
        }
        else if (args.InvokedItemContainer != null)
        {
            var tag = args.InvokedItemContainer.Tag?.ToString();
            HandleNavigation(tag);
        }
    }

    private void HandleNavigation(string? tag)
    {
        var pageType = tag switch
        {
            "DashboardPage" => typeof(DashboardPage),
            "HistoryPage" => typeof(HistoryPage),
            "DevicesPage" => typeof(DevicesPage),
            "WeatherPage" => typeof(WeatherPage),
            "BandSettingsPage" => typeof(BandSettingsPage),
            "TestLabPage" => typeof(TestLabPage),
            _ => typeof(DashboardPage)
        };
        NavigateTo(pageType);
    }

    private void NavigateTo(System.Type pageType)
    {
        if (RootFrame.CurrentSourcePageType != pageType)
        {
            RootFrame.Navigate(pageType);
        }
    }
}
