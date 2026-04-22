using GLH_producers.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace GLH_producers.Controllers
{
    public class OrdersController : Controller
    {
        private string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ActionResult Checkout()
        {
            int userId = GetLoggedInUserId();

            if (userId < 1)
            {
                return RedirectToAction("Login", "Account");
            }

            List<BasketItem> basket = GetBasket();

            if (basket.Count == 0)
            {
                TempData["Message"] = "Your basket is empty.";
                return RedirectToAction("Index", "Basket");
            }

            CheckoutViewModel model = new CheckoutViewModel();
            model.BasketItems = basket;
            model.Total = basket.Sum(item => item.Subtotal);

            return View(model);
        }

        [HttpPost]
        public ActionResult Checkout(CheckoutViewModel model)
        {
            int userId = GetLoggedInUserId();

            if (userId < 1)
            {
                return RedirectToAction("Login", "Account");
            }

            List<BasketItem> basket = GetBasket();
            model.BasketItems = basket;
            model.Total = basket.Sum(item => item.Subtotal);

            if (basket.Count == 0)
            {
                ModelState.AddModelError("", "Your basket is empty.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                int pointsEarned = PlaceOrder(userId, model, basket);
                Session["Basket"] = new List<BasketItem>();
                Session["Points"] = GetUserPoints(userId);
                TempData["Message"] = "Order placed successfully. You earned " + pointsEarned + " loyalty points.";

                return RedirectToAction("History");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                model.BasketItems = GetBasket();
                model.Total = model.BasketItems.Sum(item => item.Subtotal);
                return View(model);
            }
        }

        public ActionResult History()
        {
            int userId = GetLoggedInUserId();

            if (userId < 1)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Message = TempData["Message"];
            return View(GetCustomerOrders(userId));
        }

        private int PlaceOrder(int userId, CheckoutViewModel model, List<BasketItem> basket)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    List<BasketItem> checkedItems = CheckBasketStock(connection, transaction, basket);
                    decimal total = checkedItems.Sum(item => item.Subtotal);

                    string orderSql = @"
                        INSERT INTO Orders (UserId, OrderDate, OrderType, Address, RequiredDate, Status, Total)
                        VALUES (@UserId, GETDATE(), @OrderType, @Address, @RequiredDate, 'Pending', @Total);
                        SELECT SCOPE_IDENTITY();";

                    SqlCommand orderCommand = new SqlCommand(orderSql, connection, transaction);
                    orderCommand.Parameters.AddWithValue("@UserId", userId);
                    orderCommand.Parameters.AddWithValue("@OrderType", model.OrderType);
                    orderCommand.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(model.Address) ? (object)DBNull.Value : model.Address);
                    orderCommand.Parameters.AddWithValue("@RequiredDate", model.RequiredDate.HasValue ? (object)model.RequiredDate.Value.Date : DBNull.Value);
                    orderCommand.Parameters.AddWithValue("@Total", total);

                    int orderId = Convert.ToInt32(orderCommand.ExecuteScalar());

                    foreach (BasketItem item in checkedItems)
                    {
                        string itemSql = @"
                            INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price)
                            VALUES (@OrderId, @ProductId, @Quantity, @Price)";

                        SqlCommand itemCommand = new SqlCommand(itemSql, connection, transaction);
                        itemCommand.Parameters.AddWithValue("@OrderId", orderId);
                        itemCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                        itemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                        itemCommand.Parameters.AddWithValue("@Price", item.Price);
                        itemCommand.ExecuteNonQuery();

                        string stockSql = @"
                            UPDATE Products
                            SET Stock = Stock - @Quantity
                            WHERE ProductId = @ProductId AND Stock >= @Quantity";

                        SqlCommand stockCommand = new SqlCommand(stockSql, connection, transaction);
                        stockCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                        stockCommand.Parameters.AddWithValue("@Quantity", item.Quantity);

                        int changedRows = stockCommand.ExecuteNonQuery();

                        if (changedRows != 1)
                        {
                            throw new Exception("Stock changed while placing the order. Please check your basket.");
                        }
                    }

                    int pointsEarned = (int)Math.Floor(total);
                    AddPoints(connection, transaction, userId, pointsEarned);

                    transaction.Commit();
                    UpdateBasketPrices(checkedItems);

                    return pointsEarned;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private List<BasketItem> CheckBasketStock(SqlConnection connection, SqlTransaction transaction, List<BasketItem> basket)
        {
            List<BasketItem> checkedItems = new List<BasketItem>();

            foreach (BasketItem basketItem in basket)
            {
                string sql = @"
                    SELECT ProductId, Name, Price, Stock
                    FROM Products
                    WHERE ProductId = @ProductId AND IsAvailable = 1";

                SqlCommand cmd = new SqlCommand(sql, connection, transaction);
                cmd.Parameters.AddWithValue("@ProductId", basketItem.ProductId);

                SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    reader.Close();
                    throw new Exception(basketItem.Name + " is no longer available.");
                }

                int stock = (int)reader["Stock"];
                decimal price = (decimal)reader["Price"];
                string name = reader["Name"].ToString();
                reader.Close();

                if (basketItem.Quantity < 1)
                {
                    throw new Exception(name + " has an invalid quantity.");
                }

                if (basketItem.Quantity > stock)
                {
                    throw new Exception(name + " does not have enough stock.");
                }

                checkedItems.Add(new BasketItem
                {
                    ProductId = basketItem.ProductId,
                    Name = name,
                    Price = price,
                    Quantity = basketItem.Quantity,
                    Stock = stock
                });
            }

            return checkedItems;
        }

        private void AddPoints(SqlConnection connection, SqlTransaction transaction, int userId, int pointsEarned)
        {
            string sql = @"
                IF OBJECT_ID('UserPoints', 'U') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM UserPoints WHERE UserId = @UserId)
                    BEGIN
                        UPDATE UserPoints
                        SET Points = Points + @Points, LastUpdated = GETDATE()
                        WHERE UserId = @UserId
                    END
                    ELSE
                    BEGIN
                        INSERT INTO UserPoints (UserId, Points, LastUpdated)
                        VALUES (@UserId, @Points, GETDATE())
                    END
                END";

            SqlCommand cmd = new SqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Points", pointsEarned);
            cmd.ExecuteNonQuery();
        }

        private List<CustomerOrderViewModel> GetCustomerOrders(int userId)
        {
            List<CustomerOrderViewModel> orders = new List<CustomerOrderViewModel>();

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = @"
                    SELECT OrderId, OrderDate, OrderType, Address, RequiredDate, Status, Total
                    FROM Orders
                    WHERE UserId = @UserId
                    ORDER BY OrderDate DESC";

                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    orders.Add(new CustomerOrderViewModel
                    {
                        OrderId = (int)reader["OrderId"],
                        OrderDate = (DateTime)reader["OrderDate"],
                        OrderType = reader["OrderType"].ToString(),
                        Address = reader["Address"].ToString(),
                        RequiredDate = reader["RequiredDate"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["RequiredDate"],
                        Status = reader["Status"].ToString(),
                        Total = (decimal)reader["Total"]
                    });
                }

                reader.Close();

                foreach (CustomerOrderViewModel order in orders)
                {
                    order.Items = GetOrderItems(connection, order.OrderId);
                }
            }

            return orders;
        }

        private List<OrderItemViewModel> GetOrderItems(SqlConnection connection, int orderId)
        {
            List<OrderItemViewModel> items = new List<OrderItemViewModel>();

            string sql = @"
                SELECT p.Name, oi.Quantity, oi.Price
                FROM OrderItems oi
                INNER JOIN Products p ON oi.ProductId = p.ProductId
                WHERE oi.OrderId = @OrderId";

            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                items.Add(new OrderItemViewModel
                {
                    ProductName = reader["Name"].ToString(),
                    Quantity = (int)reader["Quantity"],
                    UnitPrice = (decimal)reader["Price"]
                });
            }

            reader.Close();
            return items;
        }

        private List<BasketItem> GetBasket()
        {
            List<BasketItem> basket = Session["Basket"] as List<BasketItem>;

            if (basket == null)
            {
                basket = new List<BasketItem>();
                Session["Basket"] = basket;
            }

            return basket;
        }

        private void UpdateBasketPrices(List<BasketItem> checkedItems)
        {
            Session["Basket"] = checkedItems;
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

        private int GetUserPoints(int userId)
        {
            int points = 0;

            try
            {
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    connection.Open();

                    string sql = "SELECT Points FROM UserPoints WHERE UserId = @UserId";
                    SqlCommand cmd = new SqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        points = Convert.ToInt32(result);
                    }
                }
            }
            catch
            {
                points = 0;
            }

            return points;
        }
    }
}
