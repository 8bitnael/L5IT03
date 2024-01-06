using System.Data;
using System.Data.SqlClient;
using Dapper;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Configurazione della connessione al database
var connectionString = "Host=surus.db.elephantsql.com;Port=5432;Username=rcdcrzoc;Password=CQ9R3n-DnGcPvwcKRhXgyxz1cDbBkWJN;Database=rcdcrzoc";
builder.Services.AddSingleton<IDbConnection>(new NpgsqlConnection(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Inizializza la lista degli Employee
var employees = new List<Employee>();

// Endpoint per ottenere tutti gli Employee dal database
app.MapGet("/api/employees", async (IDbConnection dbConnection) =>
{
    var sql = "SELECT * FROM Employees";
    var result = await dbConnection.QueryAsync<Employee>(sql);
    return result;
});


app.MapGet("/api/employees/{id}", async (IDbConnection dbConnection, int id) =>
{
    var sql = @"
    SELECT e.*, d.department_name
    FROM Employees e
    INNER JOIN Department d ON e.DepartmentId = d.department_id
    WHERE e.Id = @Id";
    
    var result = await dbConnection.QueryFirstOrDefaultAsync<EmployeeWithDepartment>(sql, new { Id = id });
    Console.WriteLine($"Query eseguita: {sql}");
    return result != null ? Results.Ok(result) : Results.NotFound();
});


app.MapPost("/api/employees", async (IDbConnection dbConnection, Employee employee) =>
{
    var sql = @"
        INSERT INTO Employees (Name, Surname, Position, DepartmentId)
        VALUES (@Name, @Surname, @Position, @DepartmentId)
        RETURNING *"; // Utilizzo RETURNING * per ottenere i dati inseriti, incluso l'ID assegnato automaticamente

    var insertedEmployee = await dbConnection.QueryFirstOrDefaultAsync<Employee>(sql, employee);

    if (insertedEmployee != null)
    {
        return Results.Created($"/api/employees/{insertedEmployee.Id}", insertedEmployee);
    }
    else
    {
        // Gestire il caso in cui l'inserimento non abbia restituito alcun dato (errore)
        return Results.BadRequest("Errore durante l'inserimento del dipendente.");
    }
});

// Endpoint per modificare un Employee esistente nel database
app.MapPut("/api/employees/{id}", async (IDbConnection dbConnection, int id, Employee employee) =>
{
    var sql = "UPDATE Employees SET Name = @Name, Position = @Position WHERE Id = @Id";
    var affectedRows = await dbConnection.ExecuteAsync(sql, new { Id = id, employee.Name, employee.Position });
    return affectedRows > 0 ? Results.NoContent() : Results.NotFound();
});

// Endpoint per eliminare un Employee dal database
app.MapDelete("/api/employees/{id}", async (IDbConnection dbConnection, int id) =>
{
    var sql = "DELETE FROM Employees WHERE Id = @Id";
    var affectedRows = await dbConnection.ExecuteAsync(sql, new { Id = id });
    return affectedRows > 0 ? Results.NoContent() : Results.NotFound();
});


app.Run();
 
// Classe Employee
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Position { get; set; }
    public int DepartmentId { get; set; }
}

// Classe Department
public class Department
{
    public int Id { get; set; }
    public string department_name { get; set; }

    public string department_location{ get; set; }
}

public class EmployeeWithDepartment : Employee
{
    public string department_name { get; set; }

}