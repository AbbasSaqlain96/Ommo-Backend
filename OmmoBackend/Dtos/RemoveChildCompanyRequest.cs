using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record RemoveChildCompanyRequest
    {
        [Required(ErrorMessage = "Parent Id is required.")]
        public int ParentId { get; init; }

        [Required(ErrorMessage = "Carrier or Dispatch Id is required.")]
        public int CarrierOrDispatchId { get; init; }

        [Required(ErrorMessage = "Company Type is required.")]
        [Range(1, 2, ErrorMessage = "Type must be 1 (Carrier) or 2 (Dispatch Service).")]
        public int Type { get; init; }
    }
}