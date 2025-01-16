using FileExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FileManager
{
    public class FileItem : IListItem
    {
        public string Name { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string DateModified { get; set; }
        public ImageSource Icon { get; set; }
        public string Path { get; set; }
    }
}
