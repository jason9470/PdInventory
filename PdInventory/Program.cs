using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Helpers;
using PdInventory.Models;

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

// 軌跡、軟刪除與資產授權都需要知道「是誰在操作」
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<IAssetAccess, AssetAccess>();
// 維護資料表提供的下拉選項。畫面用 @inject 直接取，不必每個控制器都補 ViewBag。
builder.Services.AddScoped<LookupOptions>();
// 人員表 ←→ 使用者帳號的對接規則，登入與人員維護兩邊共用
builder.Services.AddScoped<UserProvisioning>();

// 身分來源。Simulated 供權限測試用，正式環境設為 Ldap 改向公司 AD 驗證帳號密碼。
var employeeOptions = builder.Configuration
    .GetSection(EmployeeDirectoryOptions.SectionName)
    .Get<EmployeeDirectoryOptions>() ?? new EmployeeDirectoryOptions();
builder.Services.AddSingleton(employeeOptions);

if (employeeOptions.IsSimulated)
    builder.Services.AddScoped<IEmployeeDirectory, SimulatedEmployeeDirectory>();
else
    builder.Services.AddScoped<IEmployeeDirectory, LdapEmployeeDirectory>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        // 走 HTTPS 時自動加上 Secure；開發環境用 http 仍可運作
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// 授權原則集中在這裡定義，控制器只掛 [Authorize(Policy = ...)]，
// 要調整某個功能開放給誰時只改這一段，不必翻遍控制器。
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Admin, policy =>
        policy.RequireRole(nameof(UserRole.Admin)));

    options.AddPolicy(Policies.ManageAssets, policy =>
        policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Manager)));

    options.AddPolicy(Policies.ViewAssets, policy =>
        policy.RequireAuthenticatedUser());

    // 沒掛任何屬性的動作一律要求登入，避免新增控制器時忘了保護
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var dbDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dbDir);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(dbDir, "pdinventory.db")}"));

var app = builder.Build();

// 編輯畫面的 ViewModel 是手寫的欄位清單，與實體不同步時會安靜地存不進去。
// 在這裡先擲出例外，讓不一致變成啟動就看得到的失敗。
InfoSystemBlocks.AssertViewModelsCoverAllFields();

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
app.UseAuthentication();
app.UseAuthorization();

// AllowAnonymous：靜態檔是端點，會套用全域的 FallbackPolicy（必須登入）。
// 少了這一句，未登入時連 bootstrap.min.css 都被導回登入頁——登入頁自己因此沒有樣式。
app.MapStaticAssets().AllowAnonymous();

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
