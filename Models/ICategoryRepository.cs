using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcial Lenguaje Zemog.Models
{
    internal interface ICategoryRepository
    {
        void Add(CategoryModel categoriesModel);
        void Edit(CategoryModel categoriesModel);
        void Delete(int id);
        IEnumerable<CategoryModel> GetAll();
        IEnumerable<CategoryModel> GetByValue(string values);
    }
}
