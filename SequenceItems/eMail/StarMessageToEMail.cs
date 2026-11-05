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
using NINA.Sequencer.SequenceItem;
using NINA.StarMessenger.Communication.Email;
using NINA.StarMessenger.Processing;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.SequenceItems.Email
{
    [ExportMetadata("Name", "Sends a StarMessage via EMail")]
    [ExportMetadata("Description", "Sends a StarMessage via EMail")]
    [ExportMetadata("Icon", "StarMessageEMailIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarMessageToEMail : StarMessageInstructionBase
    {
        [ImportingConstructor]
        public StarMessageToEMail(IImageHistoryVM history, IApplicationStatusMediator applicationStatusMediator)
            : base(history, applicationStatusMediator)
        {
        }

        public StarMessageToEMail(StarMessageToEMail deepCopy) : this(deepCopy.History, deepCopy.ApplicationStatusMediator)
        {
            CopyMetaData(deepCopy);
        }

        public override object Clone()
        {
            return new StarMessageToEMail(this)
            {
                WaitForDelivery = WaitForDelivery
            };
        }

        internal override IStarMessageChannel CreateChannel()
        {
            return new EMailClient();
        }
    }
}
