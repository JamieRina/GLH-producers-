using GLH_producers.Models;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace GLH_producers.Controllers
{
    public class ProductsController : Controller
    {
        private string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ActionResult Index()
        {
            ViewBag.Message = TempData["Message"];
            return View(GetAvailableProducts());
        }

        public ActionResult Details(int id)
        {
            ProductModel product = GetAvailableProduct(id);

            if (product == null)
            {
                TempData["Message"] = "That product is not available.";
                return RedirectToAction("Index");
            }

            return View(product);
        }

        private List<ProductModel> GetAvailableProducts()
        {
            List<ProductModel> products = new List<ProductModel>();

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = @"
                    SELECT ProductId, ProducerId, Name, Category, Description, Price, Stock, BatchCode, IsAvailable
                    FROM Products
                    WHERE IsAvailable = 1 AND Stock > 0
                    ORDER BY Category, Name";

                SqlCommand cmd = new SqlCommand(sql, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    products.Add(ReadProduct(reader));
                }

                reader.Close();
            }

            return products;
        }

        private ProductModel GetAvailableProduct(int productId)
        {
            ProductModel product = null;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = @"
                    SELECT ProductId, ProducerId, Name, Category, Description, Price, Stock, BatchCode, IsAvailable
                    FROM Products
                    WHERE ProductId = @ProductId AND IsAvailable = 1 AND Stock > 0";

                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@ProductId", productId);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    product = ReadProduct(reader);
                }

                reader.Close();
            }

            return product;
        }

        private ProductModel ReadProduct(SqlDataReader reader)
        {
            return new ProductModel
            {
                ProductId = (int)reader["ProductId"],
                ProducerId = (int)reader["ProducerId"],
                Name = reader["Name"].ToString(),
                Category = reader["Category"].ToString(),
                Description = reader["Description"].ToString(),
                Price = (decimal)reader["Price"],
                Stock = (int)reader["Stock"],
                BatchCode = reader["BatchCode"].ToString(),
                IsAvailable = (bool)reader["IsAvailable"]
            };
        }
    }
}
