using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensibilitySample
{
    public class ExtensionModel
    {
        public string Title { get; private set; }

        public ExtensionModel(string title)
        {
            this.Title = title;
        }

        public override string ToString()
        {
            return this.Title;
        }

        static public ObservableCollection<ExtensionModel> GetSampleData()
        {
            var collection = new ObservableCollection<ExtensionModel>();
            collection.Add(new ExtensionModel("Page 1"));
            collection.Add(new ExtensionModel("Page 2"));
            return collection;
        }
    }
}
