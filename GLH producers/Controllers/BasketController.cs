using GLH_producers.Models;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace GLH_producers.Controllers
{
    public class BasketController : Controller
    {
        private string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ActionResult Index()
        {
            ViewBag.Message = TempData["Message"];

            BasketViewModel model = new BasketViewModel();
            model.Items = GetBasket();

            return View(model);
        }

        [HttpPost]
        public ActionResult Add(int id, int quantity = 1)
        {
            if (quantity < 1)
            {
                quantity = 1;
            }

            ProductModel product = GetProductForBasket(id);

            if (product == null)
            {
                TempData["Message"] = "That product is not available.";
                return RedirectToAction("Index", "Products");
            }

            List<BasketItem> basket = GetBasket();
            BasketItem item = basket.FirstOrDefault(x => x.ProductId == id);

            if (item == null)
            {
                item = new BasketItem
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 0,
                    Stock = product.Stock
                };

                basket.Add(item);
            }

            int newQuantity = item.Quantity + quantity;

            if (newQuantity > product.Stock)
            {
                newQuantity = product.Stock;
                TempData["Message"] = "Basket quantity was limited to the current stock.";
            }
            else
            {
                TempData["Message"] = "Product added to basket.";
            }

            item.Name = product.Name;
            item.Price = product.Price;
            item.Stock = product.Stock;
            item.Quantity = newQuantity;

            SaveBasket(basket);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Update(int id, int quantity)
        {
            List<BasketItem> basket = GetBasket();
            BasketItem item = basket.FirstOrDefault(x => x.ProductId == id);

            if (item == null)
            {
                return RedirectToAction("Index");
            }

            ProductModel product = GetProductForBasket(id);

            if (product == null)
            {
                basket.Remove(item);
                SaveBasket(basket);
                TempData["Message"] = "That product was removed because it is no longer available.";
                return RedirectToAction("Index");
            }

            if (quantity < 1)
            {
                quantity = 1;
            }

            if (quantity > product.Stock)
            {
                quantity = product.Stock;
                TempData["Message"] = "Quantity was limited to the current stock.";
            }
            else
            {
                TempData["Message"] = "Basket updated.";
            }

            item.Name = product.Name;
            item.Price = product.Price;
            item.Stock = product.Stock;
            item.Quantity = quantity;

            SaveBasket(basket);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Remove(int id)
        {
            List<BasketItem> basket = GetBasket();
            BasketItem item = basket.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                basket.Remove(item);
                SaveBasket(basket);
                TempData["Message"] = "Item removed from basket.";
            }

            return RedirectToAction("Index");
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

        private void SaveBasket(List<BasketItem> basket)
        {
            Session["Basket"] = basket;
        }

        private ProductModel GetProductForBasket(int productId)
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
                    product = new ProductModel
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

                reader.Close();
            }

            return product;
        }
    }
}
