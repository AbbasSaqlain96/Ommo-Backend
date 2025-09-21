using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record CreateSubscriptionRequest
    {
        [Required(ErrorMessage = "Dispatch Id is required.")] 
        public int DispatchId { get; init; }

        [Required(ErrorMessage = "Carrier Id is required.")]
        public int CarrierId { get; init; }
        
        [Required(ErrorMessage = "Requested Modules is required.")]
        public List<int> RequestedModules { get; init; }
    }
}