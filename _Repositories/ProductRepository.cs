using Microsoft.Data.SqlClient;
using Supermarker_mvp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Supermarker_mvp._Repositories
{
    internal class ProductRepository : BaseRepository, IProductRepository
    {
        public ProductRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }
        public void Add(ProductModel productModel)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "INSERT INTO Products VALUES (@name, @price, @stock, @categorie_id)";
                command.Parameters.Add("@name", SqlDbType.NVarChar).Value = productModel.Name;
                command.Parameters.Add("@price", SqlDbType.NVarChar).Value = productModel.Price;
                command.Parameters.Add("@stock", SqlDbType.NVarChar).Value = productModel.Stock;
                command.Parameters.Add("@categorie_id", SqlDbType.NVarChar).Value = productModel.Categorie_id;
                command.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "DELETE FROM Products WHERE Product_Id = @id";
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                command.ExecuteNonQuery();
            }
        }

        public void Edit(ProductModel productModel)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = @"UPDATE Products
                    SET Product_Name = @name,
                    Product_Price = @price,
                    Product_Stock = @stock,
                    Product_Categorie_Id = @categorie
                    WHERE Product_Id = @id";
                command.Parameters.Add("@name", SqlDbType.NVarChar).Value = productModel.Name;
                command.Parameters.Add("@price", SqlDbType.NVarChar).Value = productModel.Price;
                command.Parameters.Add("@stock", SqlDbType.NVarChar).Value = productModel.Stock;
                command.Parameters.Add("@categorie", SqlDbType.NVarChar).Value = productModel.Categorie_id;
                command.Parameters.Add("@id", SqlDbType.Int).Value = productModel.Id;
                command.ExecuteNonQuery();
            }
        }

        public IEnumerable<ProductModel> GetAll()
        {
            var productList = new List<ProductModel>();
            using (var connetion = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connetion.Open();
                command.Connection = connetion;
                command.CommandText = "SELECT * FROM Products ORDER BY Product_Id DESC";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var productmodel = new ProductModel();
                        productmodel.Id = (int)reader["Product_Id"];
                        productmodel.Name = reader["Product_Name"].ToString();
                        productmodel.Price= (int)reader["Product_Price"];
                        productmodel.Stock = (int)reader["Product_Stock"];
                        productmodel.Categorie_id = (int)reader["Product_Categorie_Id"];
                        productList.Add(productmodel);
                    }
                }
            }
            return productList;
        }

        public IEnumerable<ProductModel> GetByValue(string value)
        {
            var productLIst = new List<ProductModel>();
            int productId = int.TryParse(value, out _) ? Convert.ToInt32(value) : 0;
            string productname = value;
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = @"SELECT * FROM Products
                                        WHERE Product_Id=@id or Product_Name LIKE @name+ '%'
                                        ORDER By Product_Id DESC";
                command.Parameters.Add("@id", SqlDbType.Int).Value = productId;
                command.Parameters.Add("@name", SqlDbType.NVarChar).Value = productname;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var productmodel = new ProductModel();
                        productmodel.Id = (int)reader["Provider_Id"];
                        productmodel.Name = reader["Product_Name"].ToString();
                        productmodel.Price = (int)reader["Product_Price"];
                        productmodel.Stock = (int)reader["Product_Stock"];
                        productmodel.Categorie_id = (int)reader["Product_Categorie_Id"];
                        productLIst.Add(productmodel);
                    }
                }
            }
            return productLIst;
        }
    }
}
