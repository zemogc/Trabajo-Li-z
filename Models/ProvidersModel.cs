using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcial Lenguaje Zemog.Models
{
    internal class ProvidersModel
    {
        [DisplayName("Provider Id")]
        public int Id { get; set; }
        [DisplayName("Provider Name")]
        [Required(ErrorMessage = "Provider name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Provider name must be between 3 and 50")]
        public string Name { get; set; }

        [DisplayName("Observation")]
        [Required(ErrorMessage = "Provider observation is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Provider observation must be between 3 and 200")]
        public string Observation { get; set; }
    }
}
