using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class CreateCategoryRequest
    {
        [Required]
        public string CategoryName { get; set; }

        [Required]
        [MaxLength(300)] // Roughly 50 words
        public string CategoryDescription { get; set; }
    }
}
