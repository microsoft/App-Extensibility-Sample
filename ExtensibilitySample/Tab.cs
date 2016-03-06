using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensibilitySample
{

    public partial class MainPage : Page
    {
        List<Tab> tabs = new List<Tab>
        {
            new Tab() { Title="Image Editor", ClassType=typeof(EditorTab) },
            new Tab() { Title="Extensions", ClassType=typeof(ExtensionsTab) }
        };
    }

    public class Tab
    {
        public string Title { get; set; }
        public Type ClassType { get; set;  }
    }
}
