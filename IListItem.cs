using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FileExplorer
{
    public interface IListItem
    {
        string Name { get; set; }
        ImageSource Icon { get; set; }
    }
}
