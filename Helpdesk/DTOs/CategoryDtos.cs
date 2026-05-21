using System.ComponentModel.DataAnnotations;

namespace Helpdesk.DTOs
{
    public class CreateCategoryDto
    {
        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateCategoryDto : GetByIdDto
    {
        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
    }

    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}