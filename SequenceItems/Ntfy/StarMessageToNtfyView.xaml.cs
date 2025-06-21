using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.StarMessenger.SequenceItems.Ntfy {

    [Export(typeof(ResourceDictionary))]
    public partial class StarMessageToNtfyView : ResourceDictionary {

        public StarMessageToNtfyView() 
        {
            InitializeComponent();
        }
    }
}