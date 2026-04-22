# GLH Producers

GLH Producers is a student ASP.NET MVC web application for a local food producer system.

It lets users:
- browse products
- add items to a basket
- place orders
- manage products if they are a producer

This project was built as part of my full-stack web development practice using C#, ASP.NET MVC, SQL Server, HTML, CSS, Bootstrap, and a small amount of JavaScript.

## About the Project

This project is based on a practice exam brief for a local food hub system.

The website includes:
- public product browsing
- customer checkout
- producer product management
- simple order viewing
- loyalty points
- a hidden admin page for changing user roles

I am still learning software and web development, so I have tried to keep the project simple, readable, and easy to follow.

The main aim of this project was to practise:
- ASP.NET MVC
- SQL Server
- authentication
- sessions
- role-based features
- building a full web application structure

## Technologies Used

- C#
- ASP.NET MVC
- SQL Server LocalDB
- ADO.NET / SqlConnection
- HTML5
- CSS3
- Bootstrap
- JavaScript

This project does **not** use Entity Framework.

## Features

### Public Features
- Homepage with a simple welcome section
- Product browsing page
- Product prices and stock shown clearly

### Basket and Orders
- Session-based basket
- Add items to basket
- Update basket quantity
- Remove items from basket
- Checkout for logged-in users
- Collection or delivery option
- Delivery address validation
- Required date validation
- Orders saved to SQL Server
- Order items saved to SQL Server
- Stock reduced after checkout
- Customer order history

### Loyalty System
- Simple points system for users

### Producer Features
- Producer dashboard
- Add products
- Edit products
- Remove products
- View own products
- View orders related to their own products

### Admin Features
- Hidden admin page at `/Admin/Roles`
- Admin can change a user role to:
  - Customer
  - Producer
  - Admin

The admin page is not shown in the normal site navigation.

## Project Structure

```text
GLH Producers/
│
├── App_Start/
├── Content/
├── Controllers/
├── Models/
├── Properties/
├── Scripts/
├── Views/
├── App_Data/
├── DatabaseUpdates.sql
├── GLH Producers.csproj
├── Global.asax
├── Global.asax.cs
├── Web.config
├── Web.Debug.config
├── Web.Release.config
├── packages.config
└── favicon.ico
Database Notes

The project uses SQL Server LocalDB with the database file stored in App_Data.

Main tables used in the project:

Users
Producers
Products
Orders
OrderItems
UserPoints

If the UserPoints table is missing, run DatabaseUpdates.sql.

This script adds the loyalty points table and creates starting points rows for existing users.

How to Run the Project

Clone the repository:

git clone https://github.com/JamieRina/GLH-producers-.git
Open the solution in Visual Studio
Restore NuGet packages if needed
Check the database connection string in Web.config
Run DatabaseUpdates.sql if needed
Start the project using IIS Express or Visual Studio
User Roles

The project supports 3 roles:

Customer
Producer
Admin
Role behaviour
New users register as Customer by default
Producers can manage their own products
Admins can go to /Admin/Roles after logging in to manage user roles
What I Practised in This Project

This project helped me practise:

building an ASP.NET MVC application
connecting C# code to a SQL Server database
using sessions for basket data
creating role-based pages
writing controller actions and Razor views
validating user input
keeping a student project organised and readable
Future Improvements
Improve styling and responsiveness
Add better error handling
Add more detailed order status updates
Improve product search and filtering
Add more validation for producer details
Add tests as I learn more about MVC testing
Author

Jamie Rina
Student Developer
GitHub: @JamieRina

Final Note

This project is part of my learning process.

It is not meant to be perfect, but it shows my progress with:

C#
ASP.NET MVC
SQL
full-stack web development

A couple of small fixes I made:
- made the wording more natural and easier to scan
- grouped features into sections
- cleaned up the project structure formatting
- made the run steps simpler
- fixed some repeated wording

I can also turn this into a more professional portfolio-style README if you want.
