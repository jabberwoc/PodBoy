using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using ReactiveUI;

namespace PodBoy.Behavior
{
    public class ColumnGridSplitterBehavior : Behavior<Grid>
    {
        public static readonly DependencyProperty DetailColumnProperty = DependencyProperty.Register("DetailColumn",
            typeof(ColumnDefinition), typeof(ColumnGridSplitterBehavior), new UIPropertyMetadata());

        public static readonly DependencyProperty ItemsColumnProperty = DependencyProperty.Register("ItemsColumn",
            typeof(ColumnDefinition), typeof(ColumnGridSplitterBehavior), new UIPropertyMetadata());

        public static readonly DependencyProperty ToggleDetailProperty = DependencyProperty.Register("ToggleDetail",
            typeof(bool), typeof(ColumnGridSplitterBehavior), new UIPropertyMetadata());

        public ColumnDefinition DetailColumn
        {
            get { return (ColumnDefinition) GetValue(DetailColumnProperty); }
            set { SetValue(DetailColumnProperty, value); }
        }

        public ColumnDefinition ItemsColumn
        {
            get { return (ColumnDefinition) GetValue(ItemsColumnProperty); }
            set { SetValue(DetailColumnProperty, value); }
        }

        public bool ToggleDetail
        {
            get { return (bool) GetValue(ToggleDetailProperty); }
            set { SetValue(ToggleDetailProperty, value); }
        }

        public GridLength DesiredDetailGridLength { get; private set; }

        protected override void OnAttached()
        {
            base.OnAttached();

            InitSettings();

            this.WhenAnyValue(_ => _.ToggleDetail).Subscribe(ToggleShowDetail);
        }

        private void ToggleShowDetail(bool shouldShow)
        {
            if (shouldShow)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (DesiredDetailGridLength.Value < 5)
                {
                    DesiredDetailGridLength = new GridLength(ItemsColumn.Width.Value / 3, GridUnitType.Star);
                }

                DetailColumn.Width = DesiredDetailGridLength;
                ItemsColumn.Width = new GridLength(ItemsColumn.Width.Value - DetailColumn.Width.Value, GridUnitType.Star);
            }
            else
            {
                DesiredDetailGridLength = DetailColumn.Width;
                DetailColumn.Width = new GridLength(0, GridUnitType.Star);
                // TODO check if too large?
                ItemsColumn.Width = new GridLength(ItemsColumn.Width.Value + DesiredDetailGridLength.Value,
                    GridUnitType.Star);
            }
        }

        private void InitSettings()
        {
            // sync column widths with settings
            ItemsColumn.Width = Properties.Settings.Default.EpisodeColumnWidth;
            DetailColumn.Width = Properties.Settings.Default.DetailColumnWidth;
        }

        public void SaveSettings()
        {
            UserSettings.SaveColumnSettings(ItemsColumn.Width, DetailColumn.Width);
        }
    }
}