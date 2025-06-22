var builder = WebApplication.CreateBuilder(args);

// CORS para permitir peticiones desde el frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5500") // o la URL desde donde sirves el HTML
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware
app.UseHttpsRedirection();  

// Usa la política CORS
app.UseCors("PermitirFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();

