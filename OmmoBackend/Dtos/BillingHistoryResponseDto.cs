namespace OmmoBackend.Dtos
{
    public class BillingHistoryResponseDto
    {
        //public int Last12MonthMinuteConsumed { get; set; }
        //public decimal Last12MonthTotalBilled { get; set; }
        public List<BillingHistoryRecordDto> Records { get; set; } = new();

        public int Last4RecordMinuteConsumed { get; set; }
        public decimal Last4RecordTotalBilled { get; set; }
    }
}
