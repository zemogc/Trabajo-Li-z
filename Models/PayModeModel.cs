using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace Parcial Lenguaje Zemog.Models
{
    internal class PayModeModel
    {
        public int Id { get; set; }
        [DisplayName("Pay Mode Name")]
        [Required(ErrorMessage = "Pay Mode name is required")]
        [StringLength(50,MinimumLength = 3,ErrorMessage ="Pay Mode name must be between 3 and 50")]
        public string Name { get; set; }

        [DisplayName("Observation")]
        [Required(ErrorMessage = "Pay mode observation is required")]
        [StringLength(200,MinimumLength = 3, ErrorMessage ="Pay mode observation must be between 3 and 200")]
        public string Observation { get; set; }
    }
}
