// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using OKExV5Vendor.API.REST.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKExV5Vendor.API.Misc
{
    interface IOKExLeverageProvider
    {
        void PopulateLeverage(OKExSymbol symbol);
    }
}
