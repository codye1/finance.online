using System.ComponentModel.DataAnnotations;

namespace finance.online.api.Models.DTO
{
    public class OperationCreateRequestDto
    {
        [Required]
        public string Type { get; set; } = null!;

        [Required]
        public string CategoryId { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int Amount { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}