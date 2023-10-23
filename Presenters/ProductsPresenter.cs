using Supermarker_mvp.Models;
using Supermarker_mvp.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Supermarker_mvp.Presenters
{
    internal class ProductsPresenter
    {
        private IProductsView view;
        private IProductRepository repository;
        private BindingSource productBindingSource;
        private IEnumerable<ProductModel> productsList;

        public ProductsPresenter(IProductsView view, IProductRepository repository)
        {
            this.productBindingSource = new BindingSource();
            this.view = view;
            this.repository = repository;

            this.view.SearchEvent += Searchproduct;
            this.view.AddNewEvent += AddNewproduct;
            this.view.EditEvent += LoadSelectproductToEdit;
            this.view.DeleteEvent += DeleteSelectedproduct;
            this.view.SaveEvent += Saveproduct;
            this.view.CancelEvent += CancelAction;

            this.view.SetProductListBildingSource(productBindingSource);
            LoadAllProductList();
            this.view.Show();


        }

        private void CancelAction(object? sender, EventArgs e)
        {
            CleanViewFields();
        }
        private void CleanViewFields()
        {
            view.ProductId = "0";
            view.ProductName = "";
            view.ProductPrice = "";
            view.ProductStock = "0";
            view.ProductCategorieId = "0";
        }
        private void Saveproduct(object? sender, EventArgs e)
        {
            var product = new ProductModel();
            product.Id = Convert.ToInt32(view.ProductId);
            product.Name = view.ProductName;
            product.Price= Convert.ToInt32(view.ProductPrice);
            product.Stock = Convert.ToInt32(view.ProductStock);
            product.Categorie_id = Convert.ToInt32(view.ProductCategorieId);
            try
            {
                new Common.ModelDataValidation().Validate(product);
                if (view.IsEdit)
                {
                    repository.Edit(product);
                    view.Message = "PayMode Edited Successfuly";
                }
                else
                {
                    repository.Add(product);
                    view.Message = "PayMode added Successfuly";
                }
                view.IsSuccessful = true;

                CleanViewFields();
                LoadAllProductList();

            }
            catch (Exception ex)
            {
                view.IsSuccessful = false;
                view.Message = "An Error Ocurred, Could not delete pay mode";
            }
        }

        private void DeleteSelectedproduct(object? sender, EventArgs e)
        {
            try
            {
                var product = (ProductModel)productBindingSource.Current;
                repository.Delete(product.Id);
                view.IsSuccessful = true;
                view.Message = "Pay Mode deleted succesfuly";
                LoadAllProductList();
            }
            catch (Exception ex)
            {
                view.IsSuccessful = false;
                view.Message = "A error ocurred, could not delete paymode";
            }
        }

        private void LoadSelectproductToEdit(object? sender, EventArgs e)
        {
            var product = (ProductModel)productBindingSource.Current;

            view.ProductId = product.Id.ToString();
            view.ProductName = product.Name;
            view.ProductPrice = product.Price.ToString();
            view.ProductStock = product.Stock.ToString();
            view.ProductCategorieId = product.Categorie_id.ToString();

            view.IsEdit = true;
        }

        private void AddNewproduct(object? sender, EventArgs e)
        {
            view.IsEdit = false;
        }

        private void Searchproduct(object? sender, EventArgs e)
        {
            bool emptyValue = string.IsNullOrWhiteSpace(this.view.SearchValue);
            if (emptyValue == false)
            {
                productsList = repository.GetByValue(this.view.SearchValue);
            }
            else
            {
                productsList = repository.GetAll();
            }
            productBindingSource.DataSource = productsList;
        }

        private void LoadAllProductList()
        {
            productsList = repository.GetAll();
            productBindingSource.DataSource = productsList;
        }
    }
}
