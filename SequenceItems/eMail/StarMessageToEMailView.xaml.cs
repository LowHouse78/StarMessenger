using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.StarMessenger.SequenceItems.Email {

    [Export(typeof(ResourceDictionary))]
    public partial class StarMessageToEMailView : ResourceDictionary {

        public StarMessageToEMailView() 
        {
            InitializeComponent();
        }
    }
}