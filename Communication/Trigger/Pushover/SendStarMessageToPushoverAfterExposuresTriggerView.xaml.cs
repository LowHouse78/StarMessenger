﻿using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.StarMessenger.Communication.Trigger.Pushover {

    [Export(typeof(ResourceDictionary))]
    public partial class SendStarMessageToPushoverAfterExposuresTriggerView : ResourceDictionary {

        public SendStarMessageToPushoverAfterExposuresTriggerView() 
        {
            InitializeComponent();
        }
    }
}