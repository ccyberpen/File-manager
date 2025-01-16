using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

public class IconHelper
{
    // Импорт функции SHGetFileInfo из shell32.dll для получения информации о файле
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    // Структура для хранения информации о файле, возвращаемой функцией SHGetFileInfo
    [StructLayout(LayoutKind.Sequential)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;           // Дескриптор иконки
        public int iIcon;              // Индекс иконки
        public uint dwAttributes;      // Атрибуты файла
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;   // Отображаемое имя файла
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;      // Тип файла
    }

    // Константы для флагов, используемых при вызове SHGetFileInfo
    private const uint SHGFI_ICON = 0x100;      // Запрос иконки
    private const uint SHGFI_LARGEICON = 0x0;   // Запрос большой иконки
    private const uint SHGFI_SMALLICON = 0x1;   // Запрос маленькой иконки

    // Метод для получения иконки файла
    public static BitmapImage GetFileIcon(string filePath, bool largeIcon = false)
    {
        SHFILEINFO shinfo = new SHFILEINFO();
        uint flags = SHGFI_ICON | (largeIcon ? SHGFI_LARGEICON : SHGFI_SMALLICON);

        // Получение информации о файле
        SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

        if (shinfo.hIcon == IntPtr.Zero)
            return null;

        // Конвертация иконки в BitmapImage
        Icon icon = Icon.FromHandle(shinfo.hIcon);
        using (var ms = new System.IO.MemoryStream())
        {
            icon.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
    }
}