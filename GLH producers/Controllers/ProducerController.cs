using GLH_producers.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.Mvc;

namespace GLH_producers.Controllers
{
    public class ProducerController : Controller
    {
        private string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ActionResult Dashboard()
        {
            if (!CheckProducerAccess())
            {
                return RedirectToAction("Login", "Account");
            }

            int producerId = EnsureProducerRecord(GetLoggedInUserId());

            ProducerDashboardViewModel model = new ProducerDashboardViewModel();
            model.Producer = GetProducer(producerId);
            model.Products = GetProductsForProducer(producerId);
            model.Orders = GetOrdersForProducer(producerId);

            return View("ProducerHome", model);
        }

        public ActionResult MyProducts()
        {
            if (!CheckProducerAccess())
            {
                return RedirectToAction("Login", "Account");
            }

            int producerId = EnsureProducerRecord(GetLoggedInUserId());
            ViewBag.Message = TempData["Message"];

            return View("ProductList", GetProductsForProducer(producerId));
        }

        public ActionResult CreateProduct()
        {
            if (!CheckProducerAccess())
            {
                return RedirectToAction("Login", "Account");
            }

            ProductModel model = new ProductModel();
            model.IsAvailable = true;

            return View("ProductForm", model);
        }

        [HttpPost]
        public ActionResult CreateProduct(ProductModel model)
        {
            if (!CheckProducerAccess())
            {
                return RedirectToAction("Login", "Account");
            }

            int producerId = EnsureProducerRecord(GetLoggedInUserId());
            model.ProducerId = producerId;
            CheckProductRules(model);

            if (!ModelState.IsValid)
            {
                return View("ProductForm", model);
            }

            string sql = $@"
                INSERT INTO Products (ProducerId, Name, Category, Description, Price, Stock, BatchCode, IsAvailable)
                VALUES ({producerId}, N'{Clean(model.Name)}', N'{Clean(model.Category)}', N'{Clean(model.Description)}',
                        {model.Price.ToString(CultureInfo.InvariantCulture)}, {model.Stock}, N'{Clean(model.BatchCode)}',
                        {(model.IsAvailable ? 1 : 0)})";

            Database database = new Database();
            int newId = database.Insert(sql, true);

            if (newId < 1)
            {
                ModelState.AddModelError("", "Product could not be saved.");
                return View("ProductForm", model);
            }

            TempData["Message"] = "Product added.";
            return RedirectToAction("MyProducts");
        }

        public ActionResult EditProduct(int id)
        {
            if (!CheckProducerAccess())
            {
                return RedirectToAction("Login", "Account");
            }

            int producerId = EnsureProducerRecord(GetLoggedInUserId());
            ProductModel product = GetProductForProducer(id, producerId);

            if (product == null)
            {
                TempData["Message"] = "Product was not found.";
                return RedirectToAction("MyProducts");
            }

            return View("EditProductForm", product);
        }

        [HttpPost]
        public ActionResult EditProduct(ProductModel model)
        {
            if (!CheckProducerAccess())
            {
                return RedirectToAction("Login", "Account");
            }

            int producerId = EnsureProducerRecord(GetLoggedInUserId());
            model.ProducerId = producerId;
            CheckProductRules(model);

            if (!ModelState.IsValid)
            {
                return View("EditProductForm", model);
            }

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = @"
                    UPDATE Products
                    SET Name = @Name,
                        Category = @Category,
                        Description = @Description,
                        Price = @Price,
                        Stock = @Stock,
                        BatchCode = @BatchCode,
                        IsAvailable = @IsAvailable
                    WHERE ProductId = @ProductId AND ProducerId = @ProducerId";

                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                cmd.Parameters.AddWithValue("@ProducerId", producerId);
                cmd.Parameters.AddWithValue("@Name", model.Name);
                cmd.Parameters.AddWithValue("@Category", model.Category);
                cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(model.Description) ? (object)DBNull.Value : model.Description);
                cmd.Parameters.AddWithValue("@Price", model.Price);
                cmd.Parameters.AddWithValue("@Stock", model.Stock);
                cmd.Parameters.AddWithValue("@BatchCode", string.IsNullOrWhiteSpace(model.BatchCode) ? (object)DBNull.Value : model.BatchCode);
                cmd.Parameters.AddWithValue("@IsAvailable", model.IsAvailable);

                int rowsChanged = cmd.ExecuteNonQuery();

                if (rowsChanged != 1)
                {
                    ModelState.AddModelError("", "Product could not be updated.");
                    return View("EditProductForm", model);
                }
            }

            TempData["Message"] = "Product updated.";
            return RedirectToAction("MyProducts");
        }

        [HttpPost]
        public ActionResult RemoveProduct(int id)
        {
            if (!CheckProducerAccess())
            {
                return RedirectToAction("Login", "Account");
            }

            int producerId = EnsureProducerRecord(GetLoggedInUserId());

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                // Soft remove keeps old order history safe.
                string sql = @"
                    UPDATE Products
                    SET IsAvailable = 0, Stock = 0
                    WHERE ProductId = @ProductId AND ProducerId = @ProducerId";

                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@ProductId", id);
                cmd.Parameters.AddWithValue("@ProducerId", producerId);
                cmd.ExecuteNonQuery();
            }

            TempData["Message"] = "Product removed.";
            return RedirectToAction("MyProducts");
        }

        public ActionResult Orders()
        {
            if (!CheckProducerAccess())
            {
                return RedirectToAction("Login", "Account");
            }

            int producerId = EnsureProducerRecord(GetLoggedInUserId());
            return View("ProducerOrdersList", GetOrdersForProducer(producerId));
        }

        private bool CheckProducerAccess()
        {
            return Request.Cookies["authenticated"] != null &&
                   GetLoggedInUserId() > 0 &&
                   Session["Role"] != null &&
                   Session["Role"].ToString() == "Producer";
        }

        private int GetLoggedInUserId()
        {
            if (Request.Cookies["authenticated"] == null)
            {
                return 0;
            }

            int userId;
            int.TryParse(Request.Cookies["authenticated"].Value, out userId);

            return userId;
        }

        private int EnsureProducerRecord(int userId)
        {
            if (userId < 1)
            {
                return 0;
            }

            int producerId = GetProducerIdForUser(userId);

            if (producerId > 0)
            {
                return producerId;
            }

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string fullName = Session["FullName"] == null ? "New Producer" : Session["FullName"].ToString();

                string sql = @"
                    INSERT INTO Producers (UserId, BusinessName, Description, Methods)
                    VALUES (@UserId, @BusinessName, '', '');
                    SELECT SCOPE_IDENTITY();";

                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@BusinessName", fullName);

                producerId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            return producerId;
        }

        private int GetProducerIdForUser(int userId)
        {
            int producerId = 0;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = "SELECT ProducerId FROM Producers WHERE UserId = @UserId";
                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                object result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    producerId = Convert.ToInt32(result);
                }
            }

            return producerId;
        }

        private ProducerModel GetProducer(int producerId)
        {
            ProducerModel producer = null;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = @"
                    SELECT ProducerId, UserId, BusinessName, Description, Methods
                    FROM Producers
                    WHERE ProducerId = @ProducerId";

                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@ProducerId", producerId);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    producer = new ProducerModel
                    {
                        ProducerId = (int)reader["ProducerId"],
                        UserId = (int)reader["UserId"],
                        BusinessName = reader["BusinessName"].ToString(),
                        Description = reader["Description"].ToString(),
                        Methods = reader["Methods"].ToString()
                    };
                }

                reader.Close();
            }

            return producer;
        }

        private List<ProductModel> GetProductsForProducer(int producerId)
        {
            List<ProductModel> products = new List<ProductModel>();

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = @"
                    SELECT ProductId, ProducerId, Name, Category, Description, Price, Stock, BatchCode, IsAvailable
                    FROM Products
                    WHERE ProducerId = @ProducerId
                    ORDER BY Name";

                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@ProducerId", producerId);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    products.Add(ReadProduct(reader));
                }

                reader.Close();
            }

            return products;
        }

        private ProductModel GetProductForProducer(int productId, int producerId)
        {
            ProductModel product = null;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = @"
                    SELECT ProductId, ProducerId, Name, Category, Description, Price, Stock, BatchCode, IsAvailable
                    FROM Products
                    WHERE ProductId = @ProductId AND ProducerId = @ProducerId";

                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@ProductId", productId);
                cmd.Parameters.AddWithValue("@ProducerId", producerId);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    product = ReadProduct(reader);
                }

                reader.Close();
            }

            return product;
        }

        private List<ProducerOrderRowViewModel> GetOrdersForProducer(int producerId)
        {
            List<ProducerOrderRowViewModel> orders = new List<ProducerOrderRowViewModel>();

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = @"
                    SELECT o.OrderId, o.OrderDate, u.FullName, oi.OrderItemId, p.ProductId,
                           p.Name AS ProductName, oi.Quantity, oi.Price, o.Status,
                           o.OrderType, o.Address, o.RequiredDate, o.Total
                    FROM Orders o
                    INNER JOIN OrderItems oi ON o.OrderId = oi.OrderId
                    INNER JOIN Products p ON oi.ProductId = p.ProductId
                    INNER JOIN Users u ON o.UserId = u.UserId
                    WHERE p.ProducerId = @ProducerId
                    ORDER BY o.OrderDate DESC";

                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@ProducerId", producerId);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    orders.Add(new ProducerOrderRowViewModel
                    {
                        OrderId = (int)reader["OrderId"],
                        OrderDate = (DateTime)reader["OrderDate"],
                        CustomerName = reader["FullName"].ToString(),
                        OrderItemId = (int)reader["OrderItemId"],
                        ProductId = (int)reader["ProductId"],
                        ProductName = reader["ProductName"].ToString(),
                        Quantity = (int)reader["Quantity"],
                        UnitPrice = (decimal)reader["Price"],
                        LineTotal = (decimal)reader["Price"] * (int)reader["Quantity"],
                        Status = reader["Status"].ToString(),
                        OrderType = reader["OrderType"].ToString(),
                        Address = reader["Address"].ToString(),
                        RequiredDate = reader["RequiredDate"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["RequiredDate"],
                        OrderTotal = (decimal)reader["Total"]
                    });
                }

                reader.Close();
            }

            return orders;
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

        private void CheckProductRules(ProductModel model)
        {
            if (model.Price < 0)
            {
                ModelState.AddModelError("Price", "Price cannot be negative.");
            }

            if (model.Stock < 0)
            {
                ModelState.AddModelError("Stock", "Stock cannot be negative.");
            }
        }

        private string Clean(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            return text.Trim().Replace("'", "''");
        }
    }
}
