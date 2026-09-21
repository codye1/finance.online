using System.ComponentModel.DataAnnotations;

namespace finance.online.api.Models.DTO
{
    public class CategoryCreateRequestDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Color { get; set; } = null!;
    }
}