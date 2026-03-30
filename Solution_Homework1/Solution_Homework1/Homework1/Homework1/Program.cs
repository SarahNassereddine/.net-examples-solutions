using Microsoft.AspNetCore.Routing;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/Students", () => StudentList.All);

//If we do not use a route parameter in the handler, then Swagger will not appear it as required field !! But in the route, you should pass it 
app.MapGet("Student/{id}/{Age}", Handlers.GetStudent);

app.MapPost("Student/{id}", Handlers.AddStudent)
    .AddEndpointFilter(ValidationHelper.ValidateId)
    .AddEndpointFilter<GradeValidationFilter>();


//Show the student same path and method --> Error during loading swagger.
//If we do not use a route parameter in the handler, then Swagger will not appear it as required field !! But in the route, you should pass it 
app.MapPost("Student/{id}/{Age}", Handlers.AddStudent)
    .AddEndpointFilter(ValidationHelper.ValidateId)
    .AddEndpointFilter<GradeValidationFilter>();



app.MapPut("Student/{id}",  Handlers.ModifyStudent);
app.MapGet("Student/getAverage",()=> "The average of the Students is:  " + StudentList.GetAverage());//Explicit call of the method
app.MapGet("/Student/info", Handlers.GetInfo);
app.Run();
record StudentList()
{
         
    public static readonly Dictionary<int, Student> All = new();
    public static float GetAverage()
    {
        float sum = 0;
        float avg;
            foreach (var s in All)
            {
                sum += s.Value.Grade;
            }
            avg = sum / All.Count;
            return avg;
        
    }
}

public class Student
{
     public string? Name { get; set; }
     public  int Age { get; set; }
     public string? Nationality { get; set; }
     public float Grade { get; set; }
}

class Handlers
{
    public static IResult GetStudent(int id) //Here the id is automatically converted to integer
    {
        if (!StudentList.All.ContainsKey(id))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                {id.ToString(), new[] { "There is no student with this id." } }
            });
        }
        var obj = StudentList.All[id];
        return Results.Ok(obj);

    }
    public static IResult AddStudent(Student student, int id)
    {
        if (StudentList.All.ContainsKey(id))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { id.ToString() , new[] { "The student id you entered already exists !"} }
            });
        }
        StudentList.All.Add(id, student);
        return TypedResults.Created($"/Student/{id}", student);

    }
    public static IResult ModifyStudent(int id, Student student)
    {

        if (!StudentList.All.ContainsKey(id))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                {id.ToString() , new[] {"There is no Student with the id you entered ! -:)  "} }
            });

        }
        StudentList.All[id] = student;
        return Results.Ok(student);
    }
    public static IResult GetInfo()
    {
        //String s = "";
        //foreach (var item in StudentList.All)
        //{
        //    s = s + "ID: " + item.Key;
        //    s = s + ", Name: " + item.Value.Name;
        //    s = $"{s}, Age: {item.Value.Age}";
        //    s = s + "   --- ";

        //}

        //return Results.Ok(s);

        List<object> list = new List<object>();
        foreach (var item in StudentList.All)
        {
            var stinfo = new
            {
                id = item.Key,
                name = item.Value.Name,
                age = item.Value.Age,
            };

            list.Add(stinfo);
        }

        return Results.Ok<object>(list);

    }

}   

class ValidationHelper
{
    internal static async ValueTask<object?> ValidateId(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var id = context.GetArgument<int>(1);// order of the arguments in the called handler or the lambda expression
        if(id < 0 || id > 1000)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                {id.ToString(), new[] { "The id is not valid. :(" } }
            });
        }
        return await next(context);
    }   
}
//Default access is internal
internal class GradeValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
       EndpointFilterInvocationContext context,
       EndpointFilterDelegate next)
    {

        var s = context.GetArgument<Student>(0);
        if (s.Grade < 0 || s.Grade > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                {s.Grade.ToString(), new[] { " Invalid Grade - It should be between 0 and 100 :( ." } }
            });
        }
        return await next(context);
    }
}