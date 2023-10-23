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
    internal class CategoryRepository : BaseRepository, ICategoryRepository
    {
        public CategoryRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }
        public void Add(CategoryModel categoriesModel)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "INSERT INTO Categories VALUES (@name, @Observation)";
                command.Parameters.Add("@name", SqlDbType.NVarChar).Value = categoriesModel.Name;
                command.Parameters.Add("@observation", SqlDbType.NVarChar).Value = categoriesModel.Observation;
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
                command.CommandText = "DELETE FROM Categories WHERE Categorie_Id = @id";
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                command.ExecuteNonQuery();
            }
        }

        public void Edit(CategoryModel categoriesModel)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = @"UPDATE Categories
                    SET Categorie_Name = @name,
                    Categorie_Observation = @observation
                    WHERE Categorie_Id = @id";
                command.Parameters.Add("@name", SqlDbType.NVarChar).Value = categoriesModel.Name;
                command.Parameters.Add("@observation", SqlDbType.NVarChar).Value = categoriesModel.Observation;
                command.Parameters.Add("@id", SqlDbType.Int).Value = categoriesModel.Id;
                command.ExecuteNonQuery();
            }
        }

        public IEnumerable<CategoryModel> GetAll()
        {
            var categoryList = new List<CategoryModel>();
            using (var connetion = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connetion.Open();
                command.Connection = connetion;
                command.CommandText = "SELECT * FROM Categories ORDER BY Categorie_Id DESC";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var categoriemodel = new CategoryModel();
                        categoriemodel.Id = (int)reader["Categorie_Id"];
                        categoriemodel.Name = reader["Categorie_Name"].ToString();
                        categoriemodel.Observation = reader["Categorie_Observation"].ToString();
                        categoryList.Add(categoriemodel);
                    }
                }
            }
            return categoryList;
        }

        public IEnumerable<CategoryModel> GetByValue(string value)
        {
            var categoryList = new List<CategoryModel>();
            int categorieId = int.TryParse(value, out _) ? Convert.ToInt32(value) : 0;
            string categorieName = value;
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = @"SELECT * FROM Categories
                                        WHERE Categorie_Id=@id or Categorie_Name LIKE @name+ '%'
                                        ORDER By Categorie_Id DESC";
                command.Parameters.Add("@id", SqlDbType.Int).Value = categorieId;
                command.Parameters.Add("@name", SqlDbType.NVarChar).Value = categorieName;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var categorieModel = new CategoryModel();
                        categorieModel.Id = (int)reader["Categorie_Id"];
                        categorieModel.Name = reader["Categorie_Name"].ToString();
                        categorieModel.Observation = reader["Categorie_Observation"].ToString();
                        categoryList.Add(categorieModel);
                    }
                }
            }
            return categoryList;
        }
    }
}
