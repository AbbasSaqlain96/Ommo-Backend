namespace OmmoBackend.Dtos
{
    public class UserConfirmationWebhookDto
    {
        public Guid call_id { get; set; }
        public int user_id { get; set; }
        public string broker_question { get; set; }
    }

}
