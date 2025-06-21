#region "copyright"
/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion

using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.StarMessenger.SequenceItems.Pushover {

    [Export(typeof(ResourceDictionary))]
    public partial class StarMessageToPushoverView : ResourceDictionary {

        public StarMessageToPushoverView() 
        {
            InitializeComponent();
        }
    }
}