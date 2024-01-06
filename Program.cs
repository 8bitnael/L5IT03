/**
* L5IT03 C# Programming
* Daniele Castrovinci
**/
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//database connection on ElephantSQL with connectionString
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

//  Employee List
var employees = new List<Employee>();

// Endpoint * Employee from database
app.MapGet("/api/employees", async (IDbConnection dbConnection) =>
{
    var sql = "SELECT * FROM Employees";
    var result = await dbConnection.QueryAsync<Employee>(sql);
    return result;
});

//get Employee by id
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

//ADD NEW EMPLOYEE
app.MapPost("/api/employees", async (IDbConnection dbConnection, Employee employee) =>
{
    var sql = @"
        INSERT INTO Employees (Name, Surname, Position, DepartmentId)
        VALUES (@Name, @Surname, @Position, @DepartmentId)
        RETURNING *";  

    var insertedEmployee = await dbConnection.QueryFirstOrDefaultAsync<Employee>(sql, employee);

    if (insertedEmployee != null)
    {
        return Results.Created($"/api/employees/{insertedEmployee.Id}", insertedEmployee);
    }
    else
    {
        // ERROR 
        return Results.BadRequest("Errore durante l'inserimento del dipendente.");
    }
});

// Endpoint edit Employee in database
app.MapPut("/api/employees/{id}", async (IDbConnection dbConnection, int id, Employee employee) =>
{
    var sql = "UPDATE Employees SET Name = @Name, Position = @Position WHERE Id = @Id";
    var affectedRows = await dbConnection.ExecuteAsync(sql, new { Id = id, employee.Name, employee.Position });
    return affectedRows > 0 ? Results.NoContent() : Results.NotFound();
});

// Endpoint delete Employee from database
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

// Classe Employee - Department
public class EmployeeWithDepartment : Employee
{
    public string department_name { get; set; }

}
