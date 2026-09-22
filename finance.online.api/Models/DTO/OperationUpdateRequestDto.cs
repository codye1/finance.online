using System.ComponentModel.DataAnnotations;

namespace finance.online.api.Models.DTO
{
    public class OperationUpdateRequestDto
    {
        public string? Type { get; set; }

        public string? CategoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int? Amount { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}