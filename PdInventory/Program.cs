using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // 只有標註 [Required] 的欄位才必填，避免不可為 null 的字串屬性被隱含視為必填
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    // 空白輸入綁定為空字串而非 null
    options.ModelMetadataDetailsProviders.Add(new EmptyStringBindsAsEmptyProvider());
});

// 清單頁搜尋條件的記憶（見 Helpers/SearchMemory.cs）
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var dbDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dbDir);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(dbDir, "pdinventory.db")}"));

var app = builder.Build();

// 首次啟動建立資料庫並匯入 個資清冊.xlsx 匯出的種子資料
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbSeeder.Seed(db, app.Environment.ContentRootPath);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

/// <summary>表單空白輸入一律綁定為空字串（預設會轉成 null）。</summary>
class EmptyStringBindsAsEmptyProvider : IDisplayMetadataProvider
{
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (context.Key.MetadataKind == ModelMetadataKind.Property
            && context.Key.ModelType == typeof(string))
        {
            context.DisplayMetadata.ConvertEmptyStringToNull = false;
        }
    }
}
