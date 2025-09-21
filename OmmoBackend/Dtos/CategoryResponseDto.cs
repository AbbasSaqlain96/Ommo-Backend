namespace OmmoBackend.Dtos
{
    public class CategoryResponseDto
    {
        public int CategoryId { get; set; }
        public string CategoryDescription { get; set; }
        public string CategoryName { get; set; }
        public string CatType { get; set; }
        public int? CarrierId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
