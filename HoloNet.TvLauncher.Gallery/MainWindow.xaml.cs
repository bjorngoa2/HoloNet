using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using HoloNet.TvLauncher.Gallery.Stories;

namespace HoloNet.TvLauncher.Gallery;

/// <summary>
/// Browses the launcher's views in each of their states (see <see cref="StoryCatalog"/>), without
/// starting the launcher or needing a game library or streaming sign-ins.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var stories = new ListCollectionView(StoryCatalog.All.ToList());
        stories.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Story.Group)));
        StoryList.ItemsSource = stories;
        StoryList.SelectedIndex = 0;
        Loaded += (_, _) => StoryList.Focus();
    }

    private void StoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StoryList.SelectedItem is not Story story)
            return;

        // A fresh view each time, so entrance animations play as they do in the launcher.
        Preview.Content = story.CreateView();
    }
}
