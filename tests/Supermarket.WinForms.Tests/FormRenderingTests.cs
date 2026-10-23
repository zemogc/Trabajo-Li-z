using System.Drawing.Imaging;
using Supermarket.Core.Domain;
using Supermarket.WinForms.Views;

namespace Supermarket.WinForms.Tests;

/// <summary>
/// Smoke tests that build each screen off-screen, feed it sample data through its view interface
/// and render it to a bitmap. No window is ever shown.
/// </summary>
/// <remarks>
/// Set SUPERMARKET_SNAPSHOT_DIR to keep the rendered images, for example to refresh the README screenshots.
/// </remarks>
public class FormRenderingTests
{
    private static readonly Size WindowSize = new(1180, 740);

    private static readonly Category[] Categories =
    [
        new() { Id = 1, Name = "Lácteos" },
        new() { Id = 2, Name = "Granos" },
    ];

    private static readonly Provider[] Providers = [new() { Id = 1, Name = "Distribuidora del Valle" }];

    private static readonly Product[] Products =
    [
        new() { Id = 1, Name = "Arroz 1 kg", Price = 4800m, Stock = 60, MinimumStock = 25, CategoryId = 2, CategoryName = "Granos", ProviderId = 1, ProviderName = "Distribuidora del Valle" },
        new() { Id = 2, Name = "Queso campesino 500 g", Price = 11500m, Stock = 6, MinimumStock = 8, CategoryId = 1, CategoryName = "Lácteos" },
        new() { Id = 3, Name = "Leche entera 1 L", Price = 4200m, Stock = 48, MinimumStock = 20, CategoryId = 1, CategoryName = "Lácteos" },
    ];

    [Fact]
    public void Sales_screen_renders_a_sale_in_progress() => RunOnStaThread(() =>
    {
        using var form = new SalesForm();
        Prepare(form);

        form.DisplayPaymentMethods([new PaymentMethod { Id = 1, Name = "Efectivo" }, new PaymentMethod { Id = 2, Name = "Tarjeta débito" }]);
        form.DisplayProducts(Products);
        form.DisplayCart(
            [
                new SaleLine { ProductId = 1, ProductName = "Arroz 1 kg", Quantity = 2, UnitPrice = 4800m },
                new SaleLine { ProductId = 3, ProductName = "Leche entera 1 L", Quantity = 3, UnitPrice = 4200m },
            ],
            (2 * 4800m) + (3 * 4200m));
        form.DisplayRecentSales(
        [
            new Sale { Id = 3, CreatedAt = DateTime.UtcNow.AddHours(-1), PaymentMethodName = "Efectivo", Total = 19300m, ItemCount = 3 },
            new Sale { Id = 2, CreatedAt = DateTime.UtcNow.AddHours(-5), PaymentMethodName = "Tarjeta débito", Total = 10800m, ItemCount = 6 },
            new Sale { Id = 1, CreatedAt = DateTime.UtcNow.AddHours(-26), PaymentMethodName = "Efectivo", Total = 13200m, ItemCount = 3 },
        ]);
        form.DisplaySaleLines([new SaleLine { ProductName = "Queso campesino 500 g", Quantity = 1, UnitPrice = 11500m }]);

        Assert.Equal(1, form.SelectedPaymentMethodId);
        Assert.Null(form.SelectedSaleId);
        Render(form, "sales.png");
    });

    [Fact]
    public void Product_screen_renders_with_the_editor_open() => RunOnStaThread(() =>
    {
        using var form = new ProductForm();
        Prepare(form);

        form.DisplayCategories(Categories);
        form.DisplayProviders(Providers);
        form.DisplayEntities(Products);
        form.OpenEditor(Products[0]);

        var edited = form.ReadEditor();
        Assert.Equal("Arroz 1 kg", edited.Name);
        Assert.Equal(2, edited.CategoryId);
        Assert.Equal(1, edited.ProviderId);
        Assert.Equal(25, edited.MinimumStock);
        Render(form, "product-editor.png");
    });

    [Fact]
    public void Dashboard_renders_todays_figures() => RunOnStaThread(() =>
    {
        using var form = new DashboardForm();
        Prepare(form);

        form.DisplayToday(new SalesSummary { SalesCount = 3, Revenue = 43300m, UnitsSold = 12 }, productsToRestock: 1);
        form.DisplayTopProducts(
        [
            new ProductSales { ProductName = "Agua sin gas 600 ml", Units = 6, Revenue = 10800m },
            new ProductSales { ProductName = "Arroz 1 kg", Units = 3, Revenue = 14400m },
        ]);
        form.DisplayPaymentBreakdown(
        [
            new PaymentMethodSales { PaymentMethodName = "Efectivo", SalesCount = 2, Total = 32500m },
            new PaymentMethodSales { PaymentMethodName = "Tarjeta débito", SalesCount = 1, Total = 10800m },
        ]);
        form.DisplayProductsToRestock([Products[1]]);

        Render(form, "dashboard.png");
    });

    [Fact]
    public void Stock_entry_screen_renders_an_entry_in_progress() => RunOnStaThread(() =>
    {
        using var form = new StockEntryForm();
        Prepare(form);

        form.DisplayProviders(Providers);
        form.DisplayProducts(Products);
        form.DisplayDraft(
            [
                new StockEntryLine { ProductId = 1, ProductName = "Arroz 1 kg", Quantity = 24, UnitCost = 3500m },
                new StockEntryLine { ProductId = 2, ProductName = "Queso campesino 500 g", Quantity = 10, UnitCost = 8200m },
            ],
            (24 * 3500m) + (10 * 8200m));
        form.DisplayRecentEntries(
        [
            new StockEntry { Id = 1, CreatedAt = DateTime.UtcNow.AddDays(-2), ProviderName = "Distribuidora del Valle", ItemCount = 48, TotalCost = 168000m },
        ]);

        Assert.Null(form.SelectedProviderId);
        Assert.Equal(1, form.Quantity);
        Render(form, "stock-entries.png");
    });

    [Fact]
    public void Reports_screen_renders_a_period() => RunOnStaThread(() =>
    {
        using var form = new ReportsForm();
        Prepare(form);

        form.SetPeriod(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 23));
        form.DisplaySummary(new SalesSummary { SalesCount = 128, Revenue = 4_350_900m, UnitsSold = 512 });
        form.DisplaySales(
        [
            new Sale { Id = 130, CreatedAt = DateTime.UtcNow, PaymentMethodName = "Efectivo", ItemCount = 3, Total = 19300m },
            new Sale { Id = 129, CreatedAt = DateTime.UtcNow.AddHours(-2), PaymentMethodName = "Tarjeta débito", ItemCount = 6, Total = 10800m, CancelledAt = DateTime.UtcNow },
        ]);
        form.DisplayPaymentBreakdown([new PaymentMethodSales { PaymentMethodName = "Efectivo", SalesCount = 90, Total = 3_100_000m }]);
        form.DisplayTopProducts([new ProductSales { ProductName = "Arroz 1 kg", Units = 140, Revenue = 672_000m }]);

        Assert.Equal(new DateOnly(2026, 10, 1), form.FirstDay);
        Render(form, "reports.png");
    });

    private static void Prepare(Form form)
    {
        // Hosted like MainForm does it: as a child window. A child of no visible parent is
        // never on screen, yet counts as visible, which is what DrawToBitmap needs.
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Size = WindowSize;
        form.Visible = true;
        form.PerformLayout();
    }

    private static void Render(Form form, string fileName)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));

        // A blank render would mean the layout failed; count a few distinct colors.
        var colors = new HashSet<int>();
        for (var x = 0; x < bitmap.Width; x += 37)
        {
            for (var y = 0; y < bitmap.Height; y += 37)
            {
                colors.Add(bitmap.GetPixel(x, y).ToArgb());
            }
        }

        Assert.True(colors.Count > 3, "The form rendered as a blank image.");

        if (Environment.GetEnvironmentVariable("SUPERMARKET_SNAPSHOT_DIR") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            bitmap.Save(Path.Combine(directory, fileName), ImageFormat.Png);
        }
    }

    /// <summary>WinForms controls must be created on a single-threaded apartment thread.</summary>
    private static void RunOnStaThread(Action test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                test();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
