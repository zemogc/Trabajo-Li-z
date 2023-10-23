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
    internal class ProviderRepository : BaseRepository, IProvidersRepository
    {
        public ProviderRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }
        public void Add(ProvidersModel providersModel)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "INSERT INTO Providers VALUES (@name, @Observation)";
                command.Parameters.Add("@name", SqlDbType.NVarChar).Value = providersModel.Name;
                command.Parameters.Add("@observation", SqlDbType.NVarChar).Value = providersModel.Observation;
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
                command.CommandText = "DELETE FROM Providers WHERE Provider_Id = @id";
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                command.ExecuteNonQuery();
            }
        }

        public void Edit(ProvidersModel providersModel)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = @"UPDATE Providers
                    SET Provider_Name = @name,
                    Provider_Observation = @observation
                    WHERE Provider_Id = @id";
                command.Parameters.Add("@name", SqlDbType.NVarChar).Value = providersModel.Name;
                command.Parameters.Add("@observation", SqlDbType.NVarChar).Value = providersModel.Observation;
                command.Parameters.Add("@id", SqlDbType.Int).Value = providersModel.Id;
                command.ExecuteNonQuery();
            }
        }

        public IEnumerable<ProvidersModel> GetAll()
        {
            var providerList = new List<ProvidersModel>();
            using (var connetion = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connetion.Open();
                command.Connection = connetion;
                command.CommandText = "SELECT * FROM Providers ORDER BY Provider_Id DESC";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var providermodel = new ProvidersModel();
                        providermodel.Id = (int)reader["Provider_Id"];
                        providermodel.Name = reader["Provider_Name"].ToString();
                        providermodel.Observation = reader["Provider_Observation"].ToString();
                        providerList.Add(providermodel);
                    }
                }
            }
            return providerList;
        }

        public IEnumerable<ProvidersModel> GetByValue(string value)
        {
            var providerList = new List<ProvidersModel>();
            int providerId = int.TryParse(value, out _) ? Convert.ToInt32(value) : 0;
            string providerName = value;
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = @"SELECT * FROM Providers
                                        WHERE Provider_Id=@id or Provider_Name LIKE @name+ '%'
                                        ORDER By Provider_Id DESC";
                command.Parameters.Add("@id", SqlDbType.Int).Value = providerId;
                command.Parameters.Add("@name", SqlDbType.NVarChar).Value = providerName;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var providerModel = new ProvidersModel();
                        providerModel.Id = (int)reader["Provider_Id"];
                        providerModel.Name = reader["Provider_Name"].ToString();
                        providerModel.Observation = reader["Provider_Observation"].ToString();
                        providerList.Add(providerModel);
                    }
                }
            }
            return providerList;
        }
    }
}
