# EcommerceApp

ASP.NET Core MVC ecommerce storefront with SQL Server, Entity Framework Core, custom cookie authentication, Stripe checkout, admin dashboard, and Cloudinary image uploads.

## Features

- Product catalog with categories, search, filters, deals, and best sellers
- Shopping cart, wishlist, and Stripe card payments
- Orders created **only after payment succeeds** (no abandoned pending orders in normal flow)
- Admin panel: products, categories, banners, users, orders, inventory, analytics
- Email verification and password reset (MailKit)
- Role-based access (`admin` / `user`)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB or Express)
- [Stripe](https://stripe.com) test account (for checkout)
- Optional: [Cloudinary](https://cloudinary.com) account (product/banner images)
- Optional: SMTP credentials (order/verification emails)

## Quick start

### 1. Clone and configure

Update `appsettings.Development.json` (or User Secrets) — **do not commit real secrets**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=EcommerceApp;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_KEY",
    "SecretKey": "sk_test_YOUR_KEY",
    "Currency": "usd"
  },
  "EmailSettings": {
    "Enabled": "false"
  }
}
```

### 2. Database

From the project folder (`EcommerceApp`):

```bash
dotnet ef database update
```

If `dotnet ef` is not installed:

```bash
dotnet tool install --global dotnet-ef
```

### 3. Run

```bash
dotnet run
```

Open the URL shown in the console (usually `https://localhost:7xxx`).

### 4. Create an admin user

1. Register a new account at `/Account/Register` (any valid email — not limited to Gmail).
2. In SQL Server, promote your user:

```sql
UPDATE Users SET UserRole = 'admin' WHERE Email = 'your@email.com';
```

3. Sign in again and open `/Admin/Dashboard`.

## Stripe test checkout

Use [Stripe test cards](https://stripe.com/docs/testing), for example:

- Card: `4242 4242 4242 4242`
- Expiry: any future date
- CVC: any 3 digits

## Project structure

| Area | Location |
|------|----------|
| Models & EF Core | `Models/`, `Data/ApplicationDbContext.cs` |
| Checkout logic | `Services/CheckoutService.cs` |
| Storefront | `Controllers/HomeController.cs`, `Views/Home/` |
| Cart & payment | `Controllers/CartController.cs` |
| Admin | `Controllers/AdminController.cs`, `Views/Admin/` |

## Tech stack

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core + SQL Server
- Stripe.net, MailKit, Cloudinary, BCrypt

## License

Portfolio / educational project.
