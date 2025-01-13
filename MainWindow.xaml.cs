using System;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;


namespace FileManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DirectoryTreeView.SelectedItemChanged += DirectoryTreeView_SelectedItemChanged;
        }

        // Обработчик события загрузки TreeView
        private void DirectoryTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateDrives();
        }

        // Метод для заполнения TreeView дисками
        private void PopulateDrives()
        {
            DirectoryTreeView.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                // Проверяем, доступен ли диск
                TreeViewItem driveItem = new TreeViewItem
                {
                    Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new Image
                            {
                                Source = new BitmapImage(new Uri("pack://application:,,,/Resources/drive.png")),
                                Width = 32,
                                Height = 32,
                                Margin = new Thickness(0, 0, 5, 0)
                            },
                            new TextBlock { Text = drive.Name }
                        }
                    },
                    Tag = drive.Name
                };
                driveItem.Expanded += Folder_Expanded;

                // Добавляем фиктивный элемент для возможности разворачивания
                if (drive.IsReady && HasSubDirectories(drive.Name))
                {
                    driveItem.Items.Add(null);
                }

                DirectoryTreeView.Items.Add(driveItem);
            }
        }

        // Обработчик события разворачивания узла
        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;

            // Если первый элемент — фиктивный, удаляем его и загружаем реальные подкаталоги
            if (item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();
                try
                {
                    string path = item.Tag as string;
                    foreach (var directory in Directory.GetDirectories(path))
                    {
                        TreeViewItem subItem = new TreeViewItem
                        {
                            Header = new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                Children =
                                {
                                    new Image
                                    {
                                        Source = new BitmapImage(new Uri("pack://application:,,,/Resources/folder.png")),
                                        Width = 16,
                                        Height = 16,
                                        Margin = new Thickness(0, 0, 5, 0)
                                    },
                                    new TextBlock { Text = System.IO.Path.GetFileName(directory) }
                                }
                            },
                            Tag = directory
                        };
                        subItem.Expanded += Folder_Expanded;

                        // Добавляем фиктивный элемент для возможности разворачивания
                        if (HasSubDirectories(directory))
                        {
                            subItem.Items.Add(null);
                        }

                        item.Items.Add(subItem);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Недостаточно прав для доступа к некоторым каталогам
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        // Проверка, есть ли подкаталоги в каталоге
        private bool HasSubDirectories(string path)
        {
            try
            {
                return Directory.GetDirectories(path).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        // Обработчик изменения выбранного элемента TreeView
        private void DirectoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = DirectoryTreeView.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                string path = selectedItem.Tag as string;
                if (!string.IsNullOrEmpty(path))
                {
                    LoadFiles(path);
                    UpdatePathTextBox(path);
                }
            }
        }

        // Метод для обновления TextBox с текущим путем
        private void UpdatePathTextBox(string path)
        {
            PathTextBox.Text = path;
        }

        // Метод для загрузки файлов и папок в ListView
        private void LoadFiles(string path)
        {
            FilesListView.Items.Clear();
            if (String.IsNullOrEmpty(Path.GetDirectoryName(path)) || String.IsNullOrEmpty(path))
                BackButton.IsEnabled = false;
            else
                BackButton.IsEnabled = true;
            try
            {
                var dirInfo = new DirectoryInfo(path);

                // Загрузка папок
                foreach (var directory in dirInfo.GetDirectories())
                {
                    FilesListView.Items.Add(new FileItem
                    {
                        Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/folder.png")),
                        Name = directory.Name,
                        Size = "<DIR>",
                        Type = "Папка",
                        DateModified = directory.LastWriteTime.ToString(),
                        Path = System.IO.Path.Combine(path, directory.Name)
                    });
                }

                // Загрузка файлов
                foreach (var file in dirInfo.GetFiles())
                {
                    FilesListView.Items.Add(new FileItem
                    {
                        
                        Icon = IconHelper.GetFileIcon(file.FullName, false),
                        Name = file.Name,
                        Size = FileSizeConverter.ReturnConvertedSize(file.Length),
                        Type = file.Extension,
                        DateModified = file.LastWriteTime.ToString(),
                        Path = System.IO.Path.Combine(path, file.Name)
                    });
                    ;
                }

                // Обновить статусную строку
                var itemCount = FilesListView.Items.Count;
                StatusBar.Items.Clear();
                StatusBar.Items.Add(new StatusBarItem { Content = $"Элементов: {itemCount}" });
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Недостаточно прав для доступа к этому каталогу.", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("Каталог не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // Обработчик двойного клика в ListView
        private void FilesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedFile = FilesListView.SelectedItem as FileItem;
            if (selectedFile != null)
            {
                var selectedTreeItem = DirectoryTreeView.SelectedItem as TreeViewItem;
                if (selectedTreeItem != null)
                {
                    //string currentPath = PathTextBox.Text;
                    /*string newPath = System.IO.Path.Combine(currentPath, selectedFile.Name);*/

                    if (Directory.Exists(selectedFile.Path))
                    {
                        // Найти или создать узел для новой папки
                        TreeViewItem targetItem = FindTreeViewItem(DirectoryTreeView, selectedFile.Path);
                        if (targetItem != null)
                        {
                            targetItem.IsSelected = true;
                            targetItem.BringIntoView();
                            targetItem.Focus();
                        }
                        else
                        {
                            // Если узел не найден, обновляем текущий узел
                            LoadFiles(selectedFile.Path);

                            // Можно также автоматически выбрать и развернуть узел
                        }
                    }
                    else
                    {
                        // Открыть файл с помощью системного приложения
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = selectedFile.Path,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Не удалось открыть файл: {ex.Message}");
                        }
                    }
                }
            }
        }

        // Метод для поиска TreeViewItem по пути
        private TreeViewItem FindTreeViewItem(ItemsControl container, string path)
        {
            if (container == null)
                return null;

            foreach (var obj in container.Items)
            {
                // Получаем соответствующий TreeViewItem для объекта
                TreeViewItem item = container.ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;

                // Если контейнер еще не сгенерирован, принудительно его сгенерируем
                if (item == null)
                {
                    // Сворачиваем и разворачиваем узел, чтобы сгенерировать контейнер
                    container.Items.Refresh();
                    item = container.ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                }

                if (item == null)
                    continue; // Не удалось получить TreeViewItem, переходим к следующему элементу

                if (item.Tag == null)
                    continue; // Если Tag равен null, пропускаем элемент

                if (item.Tag.ToString().Equals(path, StringComparison.InvariantCultureIgnoreCase))
                {
                    return item;
                }
                else
                {
                    // Рекурсивный поиск среди дочерних элементов
                    TreeViewItem found = FindTreeViewItem(item, path);
                    if (found != null)
                        return found;
                }
            }
            UpdatePathTextBox(path);
            return null;
        }

        // Обработчик нажатия клавиш в TextBox (например, Enter для перехода по пути)
        private void PathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            string currentTextBoxText = PathTextBox.Text;
            if (e.Key == Key.Enter)
            {
                string path = PathTextBox.Text.Trim();
                if (Directory.Exists(path))
                {
                    SelectTreeViewItemByPath(path);
                    LoadFiles(path);
                }
                else
                {
                    MessageBox.Show("Указанный путь неверен или не существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdatePathTextBox(currentTextBoxText);
                }
            }
        }

        // Метод для выбора элемента TreeView по указанному пути
        private void SelectTreeViewItemByPath(string path)
        {
            string[] parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            TreeViewItem currentItem = null;

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                bool found = false;
                ItemsControl parent = currentItem == null ? (ItemsControl)DirectoryTreeView : currentItem;

                foreach (TreeViewItem item in parent.Items)
                {
                    string itemPath = item.Tag as string;
                    if (itemPath.Equals(part + Path.DirectorySeparatorChar, StringComparison.InvariantCultureIgnoreCase) ||
                        itemPath.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                    {
                        currentItem = item;
                        currentItem.IsExpanded = true;
                        currentItem.BringIntoView();
                        found = true;
                        break;
                    }
                }

                if (!found)
                    break;
            }

            if (currentItem != null)
            {
                currentItem.IsSelected = true;
            }
            else
            {
                MessageBox.Show("Не удалось найти указанный путь в навигации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //Обработчик нажатия кнопки "назад"
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            string path = PathTextBox.Text;

            if (!String.IsNullOrEmpty(path) && !String.IsNullOrEmpty(Path.GetDirectoryName(path)))
            {
                string parentDirectory = Path.GetDirectoryName(path);
                UpdatePathTextBox(parentDirectory);
                LoadFiles(parentDirectory);
            }
            else
            {
                MessageBox.Show("Родительского каталога нет.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }


    // Класс для представления файлов и папок в ListView
    public class FileItem
    {
        public string Name { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string DateModified { get; set; }
        public ImageSource Icon { get; set; }
        public string Path { get; set; }
    }
}