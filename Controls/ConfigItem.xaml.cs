using IPConfigApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace IPConfigApp.Controls
{
    /// <summary>
    /// Interaction logic for ConfigItem.xaml
    /// </summary>
    public partial class ConfigItem : UserControl
    {
        public ConfigItem()
        {
            InitializeComponent();
        }


        public ConfigItemModel ConfigItemData
        {
            get { return (ConfigItemModel)GetValue(ConfigItemDataProperty); }
            set { SetValue(ConfigItemDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConfigItemModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConfigItemDataProperty = DependencyProperty.Register("ConfigItemData", typeof(ConfigItemModel), typeof(ConfigItem));

    }
}
