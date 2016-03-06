using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensibilitySample
{
    public class TabData
    {
        public string Title { get; private set; }

        public TabData(string title)
        {
            this.Title = title;
        }

        public override string ToString()
        {
            return this.Title;
        }

        static public ObservableCollection<TabData> GetSampleData()
        {
            var collection = new ObservableCollection<TabData>();
            collection.Add(new TabData("Page 1"));
            collection.Add(new TabData("Page 2"));
            return collection;
        }
    }
}
