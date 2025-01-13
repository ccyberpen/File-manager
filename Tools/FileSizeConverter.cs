using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class FileSizeConverter
{
    //Конвертация веса файла в более компактный формат
    public static string ReturnConvertedSize(long fileSizeInBytes)
    {
        if (fileSizeInBytes < 1024)
        {
            return $"{fileSizeInBytes} байт";
        }
        else if (fileSizeInBytes < 1024 * 1024)
        {
            return $"{(fileSizeInBytes / 1024.0):F2} КБ";
        }
        else if (fileSizeInBytes < 1024 * 1024 * 1024)
        {
            return $"{(fileSizeInBytes / (1024.0 * 1024)):F2} МБ";
        }
        else
        {
            return $"{(fileSizeInBytes / (1024.0 * 1024 * 1024)):F2} ГБ";
        }
    }
}
