using Microsoft.EntityFrameworkCore;
using PgrStudentManagement.Adapters.API.Controllers;
using PgrStudentManagement.Adapters.API.Mappers;
using PgrStudentManagement.Adapters.Persistence.Relational;
using PgrStudentManagement.Adapters.Persistence.Relational.Gateway;
using PgrStudentManagement.Adapters.Persistence.Relational.Mappers;
using PgrStudentManagement.Adapters.Web.Controllers;
using PgrStudentManagement.Adapters.Web.Mappers;
using PgrStudentManagement.Business.Students.Gateway;
using PgrStudentManagement.Business.Students.ViewStudentEnrolments;

var builder = WebApplication.CreateBuilder(args);

// Register persistence
builder.Services.AddDbContext<StudentManagementDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("StudentManagementDatabase")
        ?? "Data Source=student-management.db"));

// Register mappers
builder.Services.AddSingleton<EnrolmentPersistenceMapper>();
builder.Services.AddSingleton<StudentEnrolmentsWebMapper>();
builder.Services.AddSingleton<StudentEnrolmentsApiMapper>();

// Register gateways (output port)
builder.Services.AddScoped<IEnrolmentGateway, EfEnrolmentGateway>();

// Register use cases (input port)
builder.Services.AddScoped<IViewStudentEnrolmentsUseCase, ViewStudentEnrolmentsUseCase>();

// Register MVC and API controllers from adapter assemblies
builder.Services
    .AddControllersWithViews()
    .AddApplicationPart(typeof(StudentsController).Assembly)
    .AddApplicationPart(typeof(StudentsApiController).Assembly);

var app = builder.Build();

// Ensure database exists and is seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StudentManagementDbContext>();
    await context.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
