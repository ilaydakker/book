using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BookWeb.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name ="Category Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, 100, ErrorMessage = "Range must be between 0 and 100.")]
        [Display(Name="Display Order")]
        //[ValidateNever]
        public int? DisplayOrder { get; set; }


    }
}
