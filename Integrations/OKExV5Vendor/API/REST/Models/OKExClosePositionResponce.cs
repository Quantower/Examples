// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKExV5Vendor.API.REST.Models
{
    class OKExClosePositionResponce
    {
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        [JsonProperty("posSide")]
        public OKExPositionSide? PositionSide { get; set; }
    }
}
