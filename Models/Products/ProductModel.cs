using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Models.Products
{
    public class CreateProductRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
    }

    public class UpdateProductRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
    }

    public class ProductDTO
    {
        public int Id { get; set; } 
        public string KeygenId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
