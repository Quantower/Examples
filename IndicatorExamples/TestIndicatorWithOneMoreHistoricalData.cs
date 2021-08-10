using TradingPlatform.BusinessLayer;

namespace TestIndicator
{
    public class TestIndicatorWithOneMoreHistoricalData : Indicator
    {
        [InputParameter("Additional symbol")]
        public Symbol AdditionalSymbol { get; set; }

        private HistoricalData additionalData;

        public TestIndicatorWithOneMoreHistoricalData()
        {
            this.Name = "One more HD indicator";
            this.Description = "This indicator describes how to synchronize two (and more) HistoricalData";
        }

        protected override void OnInit()
        {
            if (this.AdditionalSymbol == null)
                return;

            this.additionalData = this.AdditionalSymbol.GetHistory(Period.MIN1, Core.TimeUtils.DateTimeUtcNow.AddDays(-1));
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.additionalData == null)
                return;

            // Get time of current bar from main HistoricalData
            var time = this.Time();

            // Find bar offset from additional HistoricalData
            int offset = (int)this.additionalData.GetIndexByTime(time.Ticks);

            // If offset is less 0, then additional HistoricalData doesn't contains bar with such time
            if (offset < 0)
                return;

            // Fetch bar`s data
            var bar = this.additionalData[offset];
            double open = bar[PriceType.Open];
            double high = bar[PriceType.High];
            double low = bar[PriceType.Low];
            double close = bar[PriceType.Close];

            // Do some stuff
        }

        protected override void OnClear()
        {
            this.additionalData?.Dispose();
        }
    }
}