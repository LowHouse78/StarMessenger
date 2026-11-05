#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel.Composition;
using Newtonsoft.Json;
using NINA.Sequencer.Trigger;
using NINA.StarMessenger.Communication.Email;
using NINA.StarMessenger.Processing;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.Communication.Trigger.Email
{
    [ExportMetadata("Name", "StarMessage via EMail after Exposures")]
    [ExportMetadata("Description", "Sends a StarMessage via EMail after n Exposures")]
    [ExportMetadata("Icon", "StarMessageEMailIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SendStarMessageToEMailAfterExposuresTrigger : StarMessageAfterExposuresTriggerBase
    {
        [ImportingConstructor]
        public SendStarMessageToEMailAfterExposuresTrigger(IImageHistoryVM history) : base(history)
        {
        }

        private SendStarMessageToEMailAfterExposuresTrigger(SendStarMessageToEMailAfterExposuresTrigger deepCopy) : this(deepCopy.History)
        {
            CopyMetaData(deepCopy);
        }

        public override object Clone()
        {
            return new SendStarMessageToEMailAfterExposuresTrigger(this)
            {
                AfterExposures = AfterExposures
            };
        }

        internal override IStarMessageChannel CreateChannel()
        {
            return new EMailClient();
        }
    }
}
