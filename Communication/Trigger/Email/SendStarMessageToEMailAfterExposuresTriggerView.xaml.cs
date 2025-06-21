using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.StarMessenger.Communication.Trigger.Email
{

    [Export(typeof(ResourceDictionary))]
    public partial class SendStarMessageToEMailAfterExposuresTriggerView : ResourceDictionary {

        public SendStarMessageToEMailAfterExposuresTriggerView()
        {
            InitializeComponent();

        }
    }
}