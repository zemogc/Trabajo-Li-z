using Parcial Lenguaje Zemog.Views;
using Parcial Lenguaje Zemog._Repositories;
using Parcial Lenguaje Zemog.Models;
using Parcial Lenguaje Zemog.Views;
using System;
using System.Collections.Generic;
using System.Line;
using System.Text;
using System.Threading.Tasks;

namespace Parcial Lenguaje Zemog.Presenters
{
    internal class MainPresenter
    {
        private readonly IMainView mainView;
        private readonly string sqlConnectionString;

        public MainPresenter(IMainView mainView, string sqlConnectionString)
        {
            this.mainView = mainView;
            this.sqlConnectionString = sqlConnectionString;

            this.mainView.ShowPayModeView += ShowPayModeView;
            this.mainView.ShowCategoryView += ShowCategoryView;
            this.mainView.ShowProvidersView += ShowProvidersView;
            this.mainView.ShowProductView += ShowProductView;
        }

        private void ShowProductView(object? sender, EventArgs e)
        {
            IProductsView view = ProductsView.GetInstance((MainView)mainView);
            IProductRepository repository = new ProductRepository(sqlConnectionString);
            new ProductsPresenter(view, repository);
        }
        private void ShowProvidersView(object? sender, EventArgs e)
        {
            IProvidersView view = ProvidersView.GetInstance((MainView)mainView);
            IProvidersRepository repository = new ProviderRepository(sqlConnectionString);
            new ProviderPresenter(view, repository);
        }
        private void ShowCategoryView(object? sender, EventArgs e)
        {
            ICategoryView view = CategoryView.GetInstance((MainView)mainView);
            ICategoryRepository repository = new CategoryRepository(sqlConnectionString);
            new CategoryPresenter(view, repository);
        }

        private void ShowPayModeView(object? sender, EventArgs e)
        {
            IPayModeView view = PayModeView.GetInstance((MainView)mainView);
            IPayModeRepository repository = new PayModeRepository(sqlConnectionString);
            new PayModePresenter(view, repository);
        }
    }
}
