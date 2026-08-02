using System.Windows;
using System.Windows.Input;

namespace GameMacro.App;

public partial class CategoryNameDialog : Window
{
    public string CategoryName => NameBox.Text.Trim();

    public CategoryNameDialog(string initialName = "")
    {
        InitializeComponent();
        NameBox.Text = initialName;
        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
        PreviewKeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) DialogResult = false;
            if (args.Key == Key.Enter) Accept();
        };
    }

    private void Accept_Click(object sender, RoutedEventArgs e) => Accept();
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Accept()
    {
        if (CategoryName.Length == 0)
        {
            MessageBox.Show(this, "职业分类名称不能为空。", "无法保存", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
    }
}
