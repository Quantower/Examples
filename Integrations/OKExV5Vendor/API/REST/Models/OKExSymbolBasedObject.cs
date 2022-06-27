// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace OKExV5Vendor.API.REST.Models
{
    abstract class OKExSymbolBasedObject
    {
        public abstract OKExInstrumentType InstrumentType { get; set; }
        public abstract string OKExInstrumentId { get; set; }

        public string UniqueInstrumentId => this.InstrumentType == OKExInstrumentType.Spot || this.InstrumentType == OKExInstrumentType.Margin
            ? this.OKExInstrumentId + "-COMMON"
            : this.OKExInstrumentId;
    }
}
