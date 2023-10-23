using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcial Lenguaje Zemog.Models
{
    internal class ProductModel
    {
        [DisplayName("Product Id")]
        public int Id { get; set; }

        [DisplayName("Product Name")]
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Product name must be between 3 and 50")]
        public string Name { get; set; }

        [DisplayName("Price")]
        [Required(ErrorMessage = "Product Price is required")]
        public int Price { get; set; }

        [DisplayName("Stock")]
        [Required(ErrorMessage = "Product stock is required")]
        public int Stock { get; set; }

        [DisplayName("Categorie Id")]
        [Required(ErrorMessage = "Categorie_id is required")]
        public int Categorie_id { get; set; }


    }
}
