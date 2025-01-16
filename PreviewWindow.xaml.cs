using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FileManager
{
    public partial class PreviewWindow : Window
    {
        public PreviewWindow(string filePath)
        {
            InitializeComponent();
            LoadFile(filePath);
        }

        private void LoadFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();

            try
            {
                if (IsImage(extension))
                {
                    Image img = new Image();
                    BitmapImage bitmap = new BitmapImage(new Uri(filePath));
                    img.Source = bitmap;
                    img.Stretch = System.Windows.Media.Stretch.Uniform;
                    PreviewContent.Content = img;
                }
                else if (IsText(extension))
                {
                    TextBox textBox = new TextBox
                    {
                        Text = File.ReadAllText(filePath),
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };
                    PreviewContent.Content = textBox;
                }
                else if (IsVideo(extension) || IsAudio(extension))
                {
                    MediaElement media = new MediaElement
                    {
                        LoadedBehavior = MediaState.Manual,
                        UnloadedBehavior = MediaState.Stop,
                        Source = new Uri(filePath),
                        Stretch = System.Windows.Media.Stretch.Uniform
                    };
                    
                   
                    Grid mediaGrid = new Grid();
                    mediaGrid.Children.Add(media);
                    if (IsAudio(extension))
                    {
                        Image img = new Image
                        {
                            Source = new BitmapImage(new Uri("pack://application:,,,/Resources/audio.png")),
                            Stretch = System.Windows.Media.Stretch.Uniform,
                            Height = 300,
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        mediaGrid.Children.Add(img);
                    }
                    PreviewContent.Content = mediaGrid;
                   
                    media.Play();
                }
                else
                {
                    TextBlock unsupported = new TextBlock
                    {
                        Text = "Предпросмотр для данного типа файла не поддерживается.",
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(20)
                    };
                    PreviewContent.Content = unsupported;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private bool IsImage(string extension)
        {
            string[] supportedImages = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            return Array.Exists(supportedImages, ext => ext == extension);
        }

        private bool IsText(string extension)
        {
            return extension == ".txt" || extension == ".log";
        }

        private bool IsVideo(string extension)
        {
            string[] supportedVideos = { ".mp4", ".avi", ".mov", ".wmv" };
            return Array.Exists(supportedVideos, ext => ext == extension);
        }

        private bool IsAudio(string extension)
        {
            string[] supportedAudios = { ".mp3", ".wav", ".aac", ".wma" };
            return Array.Exists(supportedAudios, ext => ext == extension);
        }
    }
}