using System.ComponentModel;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
using UIKit;

namespace Telepathic.Platforms.iOS;

public class CustomAppShell : ShellRenderer
    {
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
        }
        protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker()
        {
            return new MyBottomNavViewAppearanceTracker();
        }
        protected override IShellNavBarAppearanceTracker CreateNavBarAppearanceTracker()
        {
            return new NoLineAppearanceTracker();
        }
    }
    public class NoLineAppearanceTracker : IShellNavBarAppearanceTracker
    {
        public void Dispose()
        {
        }
        public void ResetAppearance(UINavigationController controller)
        {
        }
    public void SetAppearance(UINavigationController controller, ShellAppearance appearance)
    {
        var navBar = controller.NavigationBar;
        var navigationBarAppearance = new UINavigationBarAppearance();
        navigationBarAppearance.ConfigureWithDefaultBackground();

        // These are the key settings to remove the line
        navigationBarAppearance.ShadowColor = UIColor.Clear;
        navigationBarAppearance.ShadowImage = new UIImage();

        // Set the background color according to your app's theme
        if (appearance?.BackgroundColor != null)
        {
            var color = appearance.BackgroundColor.ToPlatform();
            navigationBarAppearance.BackgroundColor = color;
        }

        // Apply the appearance to all appearance states
        navBar.StandardAppearance = navigationBarAppearance;
        navBar.ScrollEdgeAppearance = navigationBarAppearance;
        navBar.CompactAppearance = navigationBarAppearance;

        // Additional line-removing settings
        navBar.ShadowImage = new UIImage();
        navBar.SetBackgroundImage(new UIImage(), UIBarMetrics.Default);
            
        // navBar.TintColor = Application.Current.Resources[""];
        }
        public void SetHasShadow(UINavigationController controller, bool hasShadow)
        {
        }
        public void UpdateLayout(UINavigationController controller)
        {
        }
    }
    class MyBottomNavViewAppearanceTracker : IShellTabBarAppearanceTracker
    {
        public void Dispose()
        {

        }
        public void ResetAppearance(UITabBarController controller)
        {

        }
        public void SetAppearance(UITabBarController controller, ShellAppearance appearance)
        {
            var topBar = controller.TabBar;
            var topBarAppearance = new UITabBarAppearance();
            topBarAppearance.ConfigureWithTransparentBackground();
            topBarAppearance.ShadowColor = UIColor.Clear;
            topBarAppearance.BackgroundColor = UIColor.Clear;
            topBarAppearance.BackgroundEffect = null;
            topBar.ScrollEdgeAppearance = topBar.StandardAppearance = topBarAppearance;
        }
        public void UpdateLayout(UITabBarController controller)
        {

        }
    }