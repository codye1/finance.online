using System.ComponentModel.DataAnnotations;

namespace finance.online.api.Models.DTO
{
    public class CategoryUpdateRequestDto
    {
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }
    }
}