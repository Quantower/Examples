
using TradingPlatform.BusinessLayer;
using TradingPlatform.PresentationLayer.Renderers.Table;

namespace OrderHistoryUsingExternalSourcePanel
{
    class OrderHistoryUsingExternalSourceTableItem : TableItem
    {
        private string[] data;

        public OrderHistoryUsingExternalSourceTableItem()
        {

        }

        public OrderHistoryUsingExternalSourceTableItem(string[] data)
        {
            this.data = data;
        }

        private static List<TableColumnDefinition> columnsDefinition = new List<TableColumnDefinition>()
        {
            new TableColumnDefinition(loc.key("Account"), TableComparingType.String) { AllowGrouping = true},
            new TableColumnDefinition(loc.key("Symbol"), TableComparingType.String){ AllowGrouping = true},
            new TableColumnDefinition(loc.key("Side"), TableComparingType.String) { AllowGrouping = true},
            new TableColumnDefinition(loc.key("Order type"), TableComparingType.String) {  AllowGrouping = true},
            new TableColumnDefinition(loc.key("Quantity"), TableComparingType.Double) {AllowGrouping = false, },
            new TableColumnDefinition(loc.key("Price"), TableComparingType.Double) {AllowGrouping = false},
            new TableColumnDefinition(loc.key("Trigger Price"), TableComparingType.Double) { AllowGrouping = false },
            new TableColumnDefinition(loc.key("Tiff"), TableComparingType.String) {AllowGrouping = true},
            new TableColumnDefinition(loc.key("Status"), TableComparingType.String) {AllowGrouping = true },
            new TableColumnDefinition(loc.key("Order id"), TableComparingType.String) { AllowGrouping = true },
        };

        public override List<TableColumnDefinition> ColumnsDefinition
        {
            get
            {
                return columnsDefinition;
            }
        }

        public override (object value, string formattedValue) GetCellValue(int columnIndex, bool RequireFormattedValue = true)
        {
            OrderHistory orderHistory = (OrderHistory)DataObject;

            string formattedValue = null;
            object value = null;

            switch (columnIndex)
            {
                case 0:
                    value = data[0];                    
                    break;
                case 1:
                    value = data[1];
                    break;
                case 2:
                    value = data[2];
                    break;
                case 3:
                    value = data[3];
                    break;
                case 4:
                    value = double.Parse(data[4]);
                    formattedValue = data[4];
                    break;
                case 5:
                    value = double.Parse(data[5]);
                    formattedValue = data[5];
                    break;
                case 6:
                    value = double.Parse(data[6]);
                    formattedValue = data[6];
                    break;
                case 7:
                    value = data[7];
                    break;
                case 8:
                    value = data[8];
                    break;
                case 9:
                    value = data[9];
                    break;
            }

            if (RequireFormattedValue && formattedValue == null && value != null)
                formattedValue = value.ToString();

            return (value, formattedValue);
        }
    }
}
