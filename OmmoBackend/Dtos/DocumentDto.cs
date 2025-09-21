using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class DocumentDto
    {
        [Required]
        [SwaggerSchema("Document type ID, e.g., 1 for Medical Card, 2 for Social Security.")]
        public int DocTypeId { get; set; }

        [Required]
        [SwaggerSchema("The name of the document type, e.g., 'Medical Card' or 'Social Security'.")]
        public string DocTypeName { get; set; }

        [Required]
        [SwaggerSchema("The file to upload for the specified document type.")]
        public IFormFile Document { get; set; }
    }
}
