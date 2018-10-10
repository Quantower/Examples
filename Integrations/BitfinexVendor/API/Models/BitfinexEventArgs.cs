using BitfinexVendor.API.Models;
using System;
using System.Collections.Generic;

namespace BitfinexVendor.API
{
    class BitfinexEventArgs : EventArgs
    {
        public BitfinexTicker Ticker { get; internal set; }

        public BitfinexBookItem BookItem { get; internal set; }

        public List<BitfinexBookItem> Book { get; internal set; }

        public BitfinexTrade Trade { get; internal set; }

        public bool IsSnapshotData { get; internal set; }
    }
}