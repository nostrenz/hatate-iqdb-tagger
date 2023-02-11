using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Options = Hatate.Properties.Settings;
using ListViewItem = System.Windows.Controls.ListViewItem;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

namespace Hatate.View.Window
{
    public partial class ValidMatchRules : System.Windows.Window
    {
        public ValidMatchRules(System.Windows.Window owner)
        {
            InitializeComponent();

            this.Owner = owner;

            // Add match types
            foreach (var value in System.Enum.GetValues(typeof(IqdbApi.Enums.MatchType))) {
                Combo_MatchType.Items.Add(value);
            }

            this.CheckBox_MatchType.IsChecked = Options.Default.CheckMatchType;
            this.Combo_MatchType.SelectedItem = Options.Default.MatchType;
            this.TextBox_MinimumTagsCount.Text = Options.Default.TagsCount.ToString();
            this.Slider_Similarity.Value = Options.Default.Similarity;
            this.Slider_SimilarityThreshold.Value = Options.Default.SimilarityThreshold;
            this.Slider_SimilarityThreshold.ToolTip = this.Label_SimilarityThreshold.ToolTip;

            this.LoadSources();

            // Create sources list context menu
            ContextMenu context = new ContextMenu();
            MenuItem item = new MenuItem();

            item.Header = "Move up";
            item.Tag = "up";
            item.Click += this.ContextMenu_MenuItem_MoveSourceUpOrDown;
            context.Items.Add(item);

            item = new MenuItem();
            item.Header = "Move down";
            item.Tag = "down";
            item.Click += this.ContextMenu_MenuItem_MoveSourceUpOrDown;
            context.Items.Add(item);

            this.ListView_Sources.ContextMenu = context;

            this.UpdateLabels();
        }

        /*
		============================================
		Private
		============================================
		*/

        private void LoadSources()
        {
            List<SourceItem> sourceItems = new List<SourceItem>();

            foreach (Source source in App.sources.SourcesList) {
                SourceItem sourceItem = new SourceItem(source);
                sourceItem.MoveUpRequested += new EventHandler(this.SourceItem_MoveUp);
                sourceItem.MoveDownRequested += new EventHandler(this.SourceItem_MoveDown);

                sourceItems.Add(sourceItem);
            }

            this.UpdateSources(sourceItems);
        }

        private void UpdateSources(List<SourceItem> sourceItems)
        {
            // Sort by ordering
            sourceItems = sourceItems.OrderBy(sourceItem => sourceItem.Ordering).ToList();

            foreach (SourceItem sourceItem in sourceItems) {
                this.ListView_Sources.Items.Add(sourceItem);
            }
        }

        /// <summary>
        /// Move a SourceItem up or down in the Sources list.
        /// </summary>
        /// <param name="selectedItem"></param>
        /// <param name="up"></param>
        private void MoveSourceItemUpOrDown(SourceItem selectedItem, bool up)
        {
            int indexOfSelectedItem = this.ListView_Sources.Items.IndexOf(selectedItem);

            // Don't move source up if it's already at the top
            if (up && indexOfSelectedItem < 1) {
                return;
            } else if (!up && indexOfSelectedItem == this.ListView_Sources.Items.Count - 1) {
                return;
            }

            SourceItem otherItem = this.ListView_Sources.Items.GetItemAt(indexOfSelectedItem + (up ? -1 : 1)) as SourceItem;

            // Exchange ordering with the item above
            byte selectedItemOrdering = selectedItem.Ordering;
            byte otherItemOrdering = otherItem.Ordering;

            if (selectedItemOrdering == otherItemOrdering) {
                if (up) otherItemOrdering -= 1;
                else otherItemOrdering += 1;
            }

            selectedItem.Ordering = otherItemOrdering;
            otherItem.Ordering = selectedItemOrdering;

            // Build new list
            List<SourceItem> sourceItems = new List<SourceItem>();

            foreach (SourceItem sourceItem in this.ListView_Sources.Items) {
                sourceItems.Add(sourceItem);
            }

            // Update sources
            this.ListView_Sources.Items.Clear();
            this.UpdateSources(sourceItems);
        }

        private void UpdateLabels()
        {
            this.Label_Similarity.Content = "Minimum similarity (" + (int)this.Slider_Similarity.Value + "%)";
		}

        /*
		============================================
		Event
		============================================
		*/

        private void CheckBox_MatchType_Click(object sender, RoutedEventArgs e)
        {
            this.Label_MatchType.IsEnabled = (bool)this.CheckBox_MatchType.IsChecked;
            this.Combo_MatchType.IsEnabled = (bool)this.CheckBox_MatchType.IsChecked;
        }

        private void ListView_Sources_PreviewMouseMoveEvent(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is ListViewItem && e.RightButton == System.Windows.Input.MouseButtonState.Pressed) {
                ListViewItem draggedItem = sender as ListViewItem;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        private void ListView_Sources_Drop(object sender, DragEventArgs e)
        {
            SourceItem droppedData = e.Data.GetData(typeof(SourceItem)) as SourceItem;
            SourceItem target = ((ListViewItem)(sender)).DataContext as SourceItem;
            sbyte removedIndex = (sbyte)ListView_Sources.Items.IndexOf(droppedData);
            sbyte targetIndex = (sbyte)ListView_Sources.Items.IndexOf(target);

            if (removedIndex == targetIndex) {
                return;
            }

            this.ListView_Sources.Items.RemoveAt(removedIndex);
            this.ListView_Sources.Items.Insert(targetIndex, droppedData);

            // Update tag indexes for ordering
            foreach (SourceItem sourceItem in this.ListView_Sources.Items) {
                sourceItem.Ordering = (byte)(1 + this.ListView_Sources.Items.IndexOf(sourceItem));
            }
        }

        private void Slider_SimilarityThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.Label_SimilarityThreshold.Content = "Similarity threshold (" + (byte)this.Slider_SimilarityThreshold.Value + "%)";

            string toolTipText = "Sources positioned higher in the list will be prefered to those with a ";
            toolTipText += "\nhigher match similarity as long as the difference is within this threshold.";
            toolTipText += "\n\nExample: Even if Danbooru is positioned first and Gelbooru second in the";
            toolTipText += "\nlist above, a value of " + (byte)this.Slider_SimilarityThreshold.Value + "% means that a Gelbooru result with 100% similarity";
            toolTipText += "\nwill still be prefered over a Danbooru one with " + (100 - (byte)this.Slider_SimilarityThreshold.Value) + "% similarity.";

            this.Label_SimilarityThreshold.ToolTip = toolTipText;
            this.Slider_SimilarityThreshold.ToolTip = toolTipText;
        }

        private void ContextMenu_MenuItem_MoveSourceUpOrDown(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;

            if (mi == null) {
                return;
            }

            SourceItem selectedItem = this.ListView_Sources.SelectedItem as SourceItem;

            this.MoveSourceItemUpOrDown(selectedItem, (string)mi.Tag == "up");
        }

        /// <summary>
        /// Called when clicking on the "Up" button on a SourceItem.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SourceItem_MoveUp(object sender, EventArgs e)
        {
            SourceItem sourceItem = sender as SourceItem;

            this.MoveSourceItemUpOrDown(sourceItem, true);
        }

        /// <summary>
        /// Called when clicking on the "Down" button on a SourceItem.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SourceItem_MoveDown(object sender, EventArgs e)
        {
            SourceItem sourceItem = sender as SourceItem;

            this.MoveSourceItemUpOrDown(sourceItem, false);
        }

        private void Sliders_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded) {
                this.UpdateLabels();
            }
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            App.sources.Clear();

            // Sources
            foreach (SourceItem sourceItem in this.ListView_Sources.Items) {
                App.sources.Add(new Source(sourceItem));
            }

            Options.Default.CheckMatchType = (bool)this.CheckBox_MatchType.IsChecked;
            Options.Default.MatchType = (IqdbApi.Enums.MatchType)this.Combo_MatchType.SelectedItem;
            Options.Default.TagsCount = Int32.Parse(this.TextBox_MinimumTagsCount.Text);
            Options.Default.Similarity = (byte)this.Slider_Similarity.Value;
            Options.Default.SimilarityThreshold = (byte)this.Slider_SimilarityThreshold.Value;
            Options.Default.Sources = App.sources.Serialize();

            Options.Default.Save();

            this.Close();
        }
    }
}
