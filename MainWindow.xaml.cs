using System;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Collections.Specialized;
using System.Threading.Tasks;


namespace FileManager
{
    public partial class MainWindow : Window
    {
        public FileSystem fileSystem = new FileSystem();
        public MainWindow()
        {
            InitializeComponent();
            DirectoryTreeView.SelectedItemChanged += DirectoryTreeView_SelectedItemChanged;
        }

        // Обработчик события загрузки TreeView
        private void DirectoryTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateDrives();
            SelectTreeViewItemByPath("C:/");
        }
        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFiles("C:/");
        }
        private void PathTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePathTextBox("C:/");
        }
        // Обработчик нажатий клавиш/комбинаций
        private void FilesListView_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.C && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                Copy();
            }
            else if (e.Key == Key.V && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                PasteFilesFromClipboard();
            }
            else if (e.Key == Key.Delete)
            {
                DeleteSelectedFiles();
            }
            else if (e.Key == Key.F2)
            {
                RenameSelectedFile();
            }
            else if (e.Key == Key.Enter)
            {
                OpenFolderOrFile();
            }
            else if (e.Key == Key.Z && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                GotoPreviousDirectory();
            }
            else if (e.Key == Key.N && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                CreateNewItem();
            }
            else if(e.Key == Key.F3)
            {
                PreviewFile();
            }
        }
        private void Copy()
        {
            var selectedItems = FilesListView.SelectedItems.Cast<FileItem>();
            var filePaths = new StringCollection();
            filePaths.AddRange(selectedItems.Select(item => item.Path).ToArray());
            if(fileSystem.CopyFilesToClipboard(filePaths) != 1)
            {
                MessageBox.Show($"Не удалось удалить файлы");
            }
        }
        private void PreviewFile()
        {
            if (FilesListView.SelectedItem is FileItem selectedItem)
            {
                string filePath = selectedItem.Path;

                if (File.Exists(filePath))
                {
                    PreviewWindow preview = new PreviewWindow(filePath);
                    preview.Owner = this;
                    preview.ShowDialog();
                }
            }
        }
        private void DeleteSelectedFiles()
        {
            var selectedItems = FilesListView.SelectedItems.Cast<FileItem>().ToList();
            if (selectedItems.Any())
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить выбранные файлы?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    foreach (var item in selectedItems)
                    {
                       
                        if(fileSystem.MoveToRecycleBin(item.Path) != 1)
                        {
                            MessageBox.Show($"Не удалось удалить файл: {item.Name}");
                        }
                            
                    }
                    LoadFiles(PathTextBox.Text); // Обновление списка файлов
                }
            }
        }
        private void CopyFileWithProgress(string sourcePath, string destPath, ProgressWindow progressWindow, int currentFile, int totalFiles)
        {
            const long bufferSize = 1024 * 1024*1024;
            byte[] buffer = new byte[bufferSize];

            using (FileStream sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            using (FileStream destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write))
            {
                long totalBytes = sourceStream.Length;
                long bytesRead = 0;
                int bytesReadInCurrentOperation;

                while ((bytesReadInCurrentOperation = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileSystem.MoveFile(destStream, buffer, bytesReadInCurrentOperation);

                    double progress = (double)bytesRead / totalBytes * 100;
                    Dispatcher.Invoke(() => progressWindow.UpdateProgress(progress, $"Копирование {currentFile + 1} из {totalFiles} файлов..."));
                }
            }
        }
        
        //Копировать директорию
        
        //Вставка файлов из буфера обмена
        private async void PasteFilesFromClipboard()
        {
            if (Clipboard.ContainsFileDropList())
            {
                var filePaths = Clipboard.GetFileDropList().Cast<string>().ToList();
                if (filePaths.Any())
                {
                    var targetDirectory = PathTextBox.Text; // Путь к текущей директории
                    if (!Directory.Exists(targetDirectory))
                    {
                        MessageBox.Show("Целевая директория не существует.");
                        return;
                    }

                    var progressWindow = new ProgressWindow();
                    progressWindow.Show();

                    await Task.Run(() =>
                    {
                        int totalFiles = filePaths.Count;
                        int currentFile = 0;

                        foreach (var filePath in filePaths)
                        {
                            var fileName = System.IO.Path.GetFileName(filePath);
                            var targetFilePath = System.IO.Path.Combine(targetDirectory, fileName);

                            if (File.Exists(targetFilePath))
                            {
                                if(filePath == targetFilePath)
                                {
                                    fileName = "Copy_" + fileName;
                                    targetFilePath = System.IO.Path.Combine(targetDirectory, fileName);
                                }
                                else
                                {
                                    var result = MessageBox.Show($"Файл с именем \"{fileName}\" уже существует. Перезаписать?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                    if (result == MessageBoxResult.No)
                                    {
                                        fileName = "Copy_" + fileName;
                                        targetFilePath = System.IO.Path.Combine(targetDirectory, fileName);// Пропустить копирование этого файла
                                    }
                                }
                            }

                            if (File.Exists(filePath))
                            {
                                CopyFileWithProgress(filePath, targetFilePath, progressWindow, currentFile, totalFiles);      
                            }
                            else if (Directory.Exists(filePath))
                            {

                                if(fileSystem.CopyDirectory(filePath, targetFilePath) != 1)
                                {
                                    MessageBox.Show($"Не удалось скопировать директорию:");
                                }
                            }

                            currentFile++;
                        }
                    });

                    progressWindow.Close();
                    LoadFiles(targetDirectory); // Обновление списка файлов
                }
            }
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
            int itemCount = 0;
            try
            {
                var dirInfo = new DirectoryInfo(path);
                if (!String.IsNullOrEmpty(path) && !String.IsNullOrEmpty(Path.GetDirectoryName(path)))
                {
                    FilesListView.Items.Add(new BackItem
                    {
                        Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/back.png")),
                        Name = "[..]"
                    }) ;
                    itemCount -= 1;
                }
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
                itemCount += FilesListView.Items.Count;
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
        private void OpenFolderOrFile()
        {
            var selectedFile = FilesListView.SelectedItem as FileItem;
            if (selectedFile != null)
            {
                var selectedTreeItem = DirectoryTreeView.SelectedItem as TreeViewItem;
                if (selectedTreeItem != null)
                {

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
            else
            {
                var back = FilesListView.SelectedItem as BackItem;
                if (back != null)
                {
                    GotoPreviousDirectory();
                }
            }
        }
        // Обработчик двойного клика в ListView
        private void FilesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenFolderOrFile();
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
        private void GotoPreviousDirectory()
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

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Copy();
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            PasteFilesFromClipboard();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedFiles();
        }
        private void RenameSelectedFile()
        {
            if (FilesListView.SelectedItem is FileItem selectedFile)
            {
                string currentPath = PathTextBox.Text;
                string oldName = selectedFile.Name;
                string oldFullPath = Path.Combine(currentPath, oldName);

                // Показать диалог для ввода нового имени
                RenameDialog renameDialog = new RenameDialog(oldName);
                renameDialog.Owner = this;
                if (renameDialog.ShowDialog() == true)
                {
                    string newName = renameDialog.NewName.Trim();

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        MessageBox.Show("Имя не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (newName.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Имя не изменилось
                        return;
                    }

                    string newFullPath = Path.Combine(currentPath, newName);

                    // Проверка существования файла или директории с новым именем
                    if (File.Exists(newFullPath) || Directory.Exists(newFullPath))
                    {
                        MessageBox.Show("Файл или папка с таким именем уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    try
                    {
                        
                        if(fileSystem.RenameFile(oldFullPath,newFullPath) != 1)
                        {
                            MessageBox.Show("Исходный файл или папка не существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Обновить имя и путь в привязанном объекте
                        selectedFile.Name = newName;
                        selectedFile.Path = newFullPath;

                        // Обновить отображение
                        LoadFiles(currentPath);

                        // Обновить статус
                        StatusBar.Items.Clear();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при переименовании: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            RenameSelectedFile();
        }
        private void CreateNewItem()
        {
            // Открываем диалоговое окно для выбора типа элемента
            var dialog = new NewItemDialog();
            if (dialog.ShowDialog() == true)
            {
                string itemType = dialog.SelectedItemType; // "Folder" или "File"
                string name = dialog.ItemName;

                // Получаем текущий путь из PathTextBox
                string currentPath = PathTextBox.Text;

                if (string.IsNullOrWhiteSpace(currentPath) || !Directory.Exists(currentPath))
                {
                    MessageBox.Show("Текущий путь недействителен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string newPath = Path.Combine(currentPath, name);

                
                if(fileSystem.CreateNewFile(itemType, newPath) != 1)
                {
                    MessageBox.Show($"Не удалось создать элемент", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LoadFiles(currentPath);
            }
        }
        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewItem();
        }
    }
}