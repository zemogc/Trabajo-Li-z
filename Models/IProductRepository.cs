﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcial Lenguaje Zemog.Models
{
    internal interface IProductRepository
    {
        void Add(ProductModel productModel);
        void Edit(ProductModel productModel);
        void Delete(int id);
        IEnumerable<ProductModel> GetAll();
        IEnumerable<ProductModel> GetByValue(string values);
    }
}
