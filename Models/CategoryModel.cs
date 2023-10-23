using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Line;
using System.Text;
using System.Threading.Tasks;

namespace Parcial Lenguaje Zemog.Models
{
    internal class CategoryModel
    {
        [DisplayName("Categorie Id")]
        public int Id { get; set; }
        [DisplayName("Categorie Name")]
        [Required(ErrorMessage = "Categorie name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Categorie name must be between 3 and 50")]
        public string Name { get; set; }

        [DisplayName("Observation")]
        [Required(ErrorMessage = "Categorie observation is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Categorie observation must be between 3 and 200")]
        public string Observation { get; set; }
    }
}
