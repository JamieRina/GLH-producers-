using GLH_producers.Models;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;

namespace GLH_producers.Controllers
{
    public class AccountController : Controller
    {
        private string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(Registeration model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Database database = new Database();

            string fullName = model.FullName.Replace("'", "''");
            string email = model.Email.Replace("'", "''");
            string passwordHash = Database.HashPassword(model.Password);

            string checkSql = $"SELECT COUNT(*) FROM Users WHERE Email='{email}'";
            int emailCount = database.Select(checkSql);

            if (emailCount > 0)
            {
                ModelState.AddModelError("Email", "This email already exists.");
                return View(model);
            }

            string sql = $@"
                INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
                VALUES ('{fullName}', '{email}', '{passwordHash}', 'Customer', GETDATE())";

            int userId = database.Insert(sql, true);

            if (userId < 1)
            {
                ModelState.AddModelError("", "Could not create account.");
                return View(model);
            }

            EnsureUserPointsRow(userId);

            HttpCookie httpCookie = new HttpCookie("authenticated", userId.ToString());
            Response.Cookies.Add(httpCookie);

            Session["FullName"] = model.FullName;
            Session["Email"] = model.Email;
            Session["Role"] = "Customer";
            Session["Points"] = 0;

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Database database = new Database();
            string cleanEmail = model.Email.Replace("'", "''");
            int userId = database.AuthenticateUser(cleanEmail, model.Password);

            if (userId < 1)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            string fullName = "";
            string email = "";
            string role = "Customer";

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = "SELECT FullName, Email, Role FROM Users WHERE UserId = @UserId";
                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    fullName = reader["FullName"].ToString();
                    email = reader["Email"].ToString();
                    role = reader["Role"].ToString();
                }

                reader.Close();
            }

            if (role != "Admin" && role != "Producer" && role != "Customer")
            {
                role = "Customer";
            }

            EnsureUserPointsRow(userId);

            HttpCookie httpCookie = new HttpCookie("authenticated", userId.ToString());
            Response.Cookies.Add(httpCookie);

            Session["FullName"] = fullName;
            Session["Email"] = email;
            Session["Role"] = role;
            Session["Points"] = GetUserPoints(userId);

            if (role == "Producer")
            {
                return RedirectToAction("Dashboard", "Producer");
            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Logout()
        {
            if (Request.Cookies["authenticated"] != null)
            {
                var cookie = new HttpCookie("authenticated");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            Session["Email"] = null;
            Session["FullName"] = null;
            Session["Role"] = null;
            Session["Points"] = null;

            return RedirectToAction("Index", "Home");
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

        private void EnsureUserPointsRow(int userId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    connection.Open();

                    string sql = @"
                        IF OBJECT_ID('UserPoints', 'U') IS NOT NULL
                        BEGIN
                            IF NOT EXISTS (SELECT 1 FROM UserPoints WHERE UserId = @UserId)
                            BEGIN
                                INSERT INTO UserPoints (UserId, Points, LastUpdated)
                                VALUES (@UserId, 0, GETDATE())
                            END
                        END";

                    SqlCommand cmd = new SqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // If the points script has not been run yet, the site can still log in.
            }
        }
    }
}
