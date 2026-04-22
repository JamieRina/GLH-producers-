using GLH_producers.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace GLH_producers.Controllers
{
    public class AdminController : Controller
    {
        private string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ActionResult Roles()
        {
            ActionResult blocked = BlockNonAdmin();

            if (blocked != null)
            {
                return blocked;
            }

            ViewBag.Message = TempData["Message"];
            return View(GetUsers());
        }

        [HttpPost]
        public ActionResult UpdateRole(int userId, string role)
        {
            ActionResult blocked = BlockNonAdmin();

            if (blocked != null)
            {
                return blocked;
            }

            if (!IsValidRole(role))
            {
                TempData["Message"] = "Invalid role selected.";
                return RedirectToAction("Roles");
            }

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = "UPDATE Users SET Role = @Role WHERE UserId = @UserId";
                SqlCommand cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Role", role);
                cmd.ExecuteNonQuery();
            }

            if (role == "Producer")
            {
                EnsureProducerRecord(userId);
            }

            if (userId == GetLoggedInUserId())
            {
                Session["Role"] = role;
            }

            TempData["Message"] = "User role updated.";
            return RedirectToAction("Roles");
        }

        private ActionResult BlockNonAdmin()
        {
            if (Request.Cookies["authenticated"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            return null;
        }

        private bool IsValidRole(string role)
        {
            return role == "Customer" || role == "Producer" || role == "Admin";
        }

        private List<AdminUserRoleViewModel> GetUsers()
        {
            List<AdminUserRoleViewModel> users = new List<AdminUserRoleViewModel>();

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string sql = "SELECT UserId, FullName, Email, Role FROM Users ORDER BY FullName";
                SqlCommand cmd = new SqlCommand(sql, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new AdminUserRoleViewModel
                    {
                        UserId = (int)reader["UserId"],
                        FullName = reader["FullName"].ToString(),
                        Email = reader["Email"].ToString(),
                        Role = reader["Role"].ToString()
                    });
                }

                reader.Close();
            }

            return users;
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

        private void EnsureProducerRecord(int userId)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string checkSql = "SELECT COUNT(*) FROM Producers WHERE UserId = @UserId";
                SqlCommand checkCommand = new SqlCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@UserId", userId);

                int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (count > 0)
                {
                    return;
                }

                string fullName = "New Producer";

                string nameSql = "SELECT FullName FROM Users WHERE UserId = @UserId";
                SqlCommand nameCommand = new SqlCommand(nameSql, connection);
                nameCommand.Parameters.AddWithValue("@UserId", userId);

                object nameResult = nameCommand.ExecuteScalar();

                if (nameResult != null && nameResult != DBNull.Value)
                {
                    fullName = nameResult.ToString();
                }

                string insertSql = @"
                    INSERT INTO Producers (UserId, BusinessName, Description, Methods)
                    VALUES (@UserId, @BusinessName, '', '')";

                SqlCommand insertCommand = new SqlCommand(insertSql, connection);
                insertCommand.Parameters.AddWithValue("@UserId", userId);
                insertCommand.Parameters.AddWithValue("@BusinessName", fullName);
                insertCommand.ExecuteNonQuery();
            }
        }
    }
}
