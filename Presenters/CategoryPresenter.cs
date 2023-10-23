using Parcial Lenguaje Zemog.Models;
using Parcial Lenguaje Zemog.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcial Lenguaje Zemog.Presenters
{
    internal class CategoryPresenter
    {
        private ICategoryView view;
        private ICategoryRepository repository;
        private BindingSource categoryBindingSource;
        private IEnumerable<CategoryModel> categoryList;

        public CategoryPresenter(ICategoryView view, ICategoryRepository repository)
        {
            this.categoryBindingSource= new BindingSource();
            this.view = view;
            this.repository = repository;

            this.view.SearchEvent += SearchCategory;
            this.view.AddNewEvent += AddNewCategory;
            this.view.EditEvent += LoadSelectCategoryToEdit;
            this.view.DeleteEvent += DeleteSelectedCategory;
            this.view.SaveEvent += SaveCategory;
            this.view.CancelEvent += CancelAction;

            this.view.SetCategorieListBildingSource(categoryBindingSource);
            LoadAllCategorieList();
            this.view.Show();
        }
        private void LoadAllCategorieList()
        {
            categoryList= repository.GetAll();
            categoryBindingSource.DataSource = categoryList;
        }

        private void CancelAction(object? sender, EventArgs e)
        {
            CleanViewFields();
        }

        private void CleanViewFields()
        {
            view.CategorieId = "0";
            view.CategorieName = "";
            view.CategorieObservation = "";
        }

        private void SaveCategory(object? sender, EventArgs e)
        {
            var categorie = new CategoryModel();
            categorie.Id = Convert.ToInt32(view.CategorieId);
            categorie.Name = view.CategorieName;
            categorie.Observation = view.CategorieObservation;

            try
            {
                new Common.ModelDataValidation().Validate(categorie);
                if (view.IsEdit)
                {
                    repository.Edit(categorie);
                    view.Message = "Categorie Edited Success";
                }
                else
                {
                    repository.Add(categorie);
                    view.Message = "categorie added Success";
                }
                view.IsSuccessful = true;

                CleanViewFields();
                LoadAllCategorieList();

            }
            catch (Exception ex)
            {
                view.IsSuccessful = false;
                view.Message = "a Error Ocurred, Could not delete categorie";
            }
        }

        private void DeleteSelectedCategory(object? sender, EventArgs e)
        {
            try
            {
                var categorie = (CategoryModel)categoryBindingSource.Current;
                repository.Delete(categorie.Id);
                view.IsSuccessful = true;
                view.Message = "Categorie deleted succes";
                LoadAllCategorieList();
            }
            catch (Exception ex)
            {
                view.IsSuccessful = false;
                view.Message = "A error ocurred, could not delete categorie";
            }
        }

        private void LoadSelectCategoryToEdit(object? sender, EventArgs e)
        {
            var categorie = (CategoryModel)categoryBindingSource.Current;

            view.CategorieId = categorie.Id.ToString();
            view.CategorieName = categorie.Name;
            view.CategorieObservation = categorie.Observation;

            view.IsEdit = true;
        }

        private void AddNewCategory(object? sender, EventArgs e)
        {
            view.IsEdit = false;
        }

        private void SearchCategory(object? sender, EventArgs e)
        {
            bool emptyValue = string.IsNullOrWhiteSpace(this.view.SearchValue);
            if (emptyValue == false)
            {
                categoryList = repository.GetByValue(this.view.SearchValue);
            }
            else
            {
                categoryList = repository.GetAll();
            }
            categoryBindingSource.DataSource = categoryList;
        }
    }
}
