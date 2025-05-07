using System.ComponentModel;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
using UIKit;

namespace Telepathic.Platforms.MacCatalyst;

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
            
            // Create a new appearance object
            var navigationBarAppearance = new UINavigationBarAppearance();
            
            // Mac Catalyst sometimes needs a different approach
            // First set to opaque to override system defaults
            navigationBarAppearance.ConfigureWithOpaqueBackground();
            
            // These are the key settings to remove the line
            navigationBarAppearance.ShadowColor = UIColor.Clear;
            navigationBarAppearance.ShadowImage = new UIImage();
            
            // Mac Catalyst specific
            // Force the shadow to be invisible by setting its height to 0
            navigationBarAppearance.ShadowColor = null;
            
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
            
            // Additional line-removing settings for Mac Catalyst
            navBar.ShadowImage = new UIImage();
            navBar.SetBackgroundImage(new UIImage(), UIBarMetrics.Default);
            
            // Ensure translucency is disabled which can affect line appearance
            navBar.Translucent = false;
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
            
            // For Mac Catalyst, use opaque background for more consistent results
            topBarAppearance.ConfigureWithOpaqueBackground();
            
            // Remove shadow and clear appearance
            topBarAppearance.ShadowColor = UIColor.Clear;
            
            // Set the background color according to your app's theme
            if (appearance?.BackgroundColor != null)
            {
                var color = appearance.BackgroundColor.ToPlatform();
                topBarAppearance.BackgroundColor = color;
            }
            else
            {
                // Default color if none specified
                topBarAppearance.BackgroundColor = UIColor.SystemBackground;
            }
            
            // Apply to all states
            topBar.StandardAppearance = topBarAppearance;
            topBar.ScrollEdgeAppearance = topBarAppearance;
        }
        public void UpdateLayout(UITabBarController controller)
        {

        }
    }