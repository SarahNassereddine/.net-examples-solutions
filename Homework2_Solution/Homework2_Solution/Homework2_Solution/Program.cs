using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System.ComponentModel.DataAnnotations;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//Exercice 1
app.MapGet("/api/random-a", Handler.GetRandom_a);//By Delegate
app.MapGet("/api/random-b", Handler.GetRandom_b);//By Delegate
app.MapGet("/api/random-b-V1", (int count)=>Handler.GetRandom_b_V1(count)); // By Calling the method - You should use lambda expression

//Exercice 2
app.MapGet("/api/square-a", Handler.GetSquare_a);
app.MapGet("/api/square-b", Handler.GetSquare_b);

//Exercice 3
app.MapPost("/api/square-a", Handler.GetSquareFromBody_a);
app.MapPost("/api/square-b", Handler.GetSquareFromBody_b);

//Erercice 4
app.MapPost("/api/person-a", ([FromBody] Person person) => Handler.AddPerson_a(person)); //In Minimal APIs (lambda expression) , It is recommended to put [FromBody] in order to let swagger detect it, otherwise we should modify some configurations in Swagger.
app.MapPost("/api/person-a-V1", ([FromBody] Person person, ILogger<Program> logger) => Handler.AddPerson_a_V1(person, logger)); //Same comment as before
app.MapPost("/api/person-b", ([FromBody] Person person) => Handler.AddPerson_b(person)) ////Same comment as before
    .AddEndpointFilter<ValidatePerson>();

//Erercice 5
app.MapPost("/api/persons", Handler.AddPeople)
	.AddEndpointFilter<ValidatePeople>();

//Erercice 6
app.MapGet("/api/person/{id}", Handler.GetPerson)
	.AddEndpointFilter<ValidateId>();

//Exercice 7
app.MapGet("/api/user-a", Handler.GetUser_a);
app.MapGet("/api/user-b", Handler.GetUser_b);

//Exercice 8
app.MapPost("/api/user", Handler.AddUser)
	.WithParameterValidation(); //This will let the application make validations using DataAnnotations and IValidatableObject, otherwise no validation even we decorate the properties with Data Annotations

//Exercice 9
app.MapGet("/api/users", Handler.GetUsers)
	.AddEndpointFilter<ValidateAge>();

//Exercice 10
app.MapGet("/api/user/{username}", Handler.GetUserByUsername)
	.AddEndpointFilter<ValidateUsername>();

//Exercice 11
app.MapPost("/api/user/{username}", Handler.UpdateEmail);

//Exercice 12
app.MapPost("/api/product", Handler.AddProduct)
	.WithParameterValidation();//This will let the application make validations using DataAnnotations and IValidatableObject

//Exercice 13
app.MapGet("/api/products", Handler.GetProductsByPrice)
	.AddEndpointFilter<ValidatePrice>();

//Exercice 14
app.MapGet("/api/product/{title}", Handler.GetProduct);

//Exercice 15
app.MapPost("/api/product/{title}/{description}", Handler.AddProductTryParse);

//Exercice 16
app.MapGet("/api/person/{id}/{name}", Handler.GetPersonInfo)
	.AddEndpointFilter<ValidateIdViaPerson>();
//Exercice 17
//See User Class

//Exercice 18
//Answer : The question requires values from query string and route parameters, so with swagger it is clear that swagger is not able to detect them implicitly in BindAsync..
//For that we should use the url to send query strings and modify the URL if we want to pass route parameter.
//Try https://localhost:7253/api/person-a?id=1&name=kamal after adding a user with id=1 and name=kamal
app.MapGet("/api/person-a", Handler.GetPersonBindAsync);

//Exercice 18
app.MapGet("/api/person-a/{id}", Handler.GetPersonBindAsync);

//Exercice 18
app.MapGet("/api/person-a/{id}/{name}", Handler.GetPersonBindAsync);

app.Run();

internal class Person
{
	public int Id { get; set; }
	public string Name { get; set; }
	public DateTime? DateOfBirth { get; set; }
	public static async ValueTask<Person?> BindAsync(HttpContext context)
	{
		var query = context.Request.Query; // To Get all query string parameters
		var person = new Person();
		
        if (context.Request.RouteValues.ContainsKey("id"))
        {
            // Access the value of the "id" route parameter
          if (int.TryParse(context.Request.RouteValues["id"].ToString(), out var outId))
			{
				person.Id = outId;
            }
		  else
			{
				person.Id = -1; //To indicate that the value is not parsable
            }
		
        }
		else if (query.TryGetValue("id", out var outId)) 
        {
            if (int.TryParse(outId, out var nId))
            {
                person.Id = nId;
            }

            else
            {
                person.Id = -1; //To indicate that the value is not parsable
            }
        }
		

		///////////
        if (context.Request.RouteValues.ContainsKey("name"))
        {
			// Access the value of the "name" route parameter
			person.Name = context.Request.RouteValues["name"].ToString();

        }
        else if (query.TryGetValue("name", out var outName))
        {  
                person.Name = outName; 
        }



        return person;
	}
}
internal class User : IValidatableObject
{
	[MaxLength(20)]
	public string Username { get; set; }
	[EmailAddress]
	public string Email { get; set; }
	[Range(1, 100)]
	public int Age { get; set; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if(Age % 2 != 0)
		{
			yield return new ValidationResult("Age must be multiple of 2");
		}
		if(!(Email.Contains("gmail.com") || Email.Contains("ul.edu.lb")))
		{
			yield return new ValidationResult("Email should contain either gmail.com or ul.edu.lb");
		}

	}
}
internal class Product
{
	[Required]
	public string Title { get; set; }
	[Required]
	public string Description { get; set; }
	[Required]
	public double Price { get; set; }
	public static bool TryParse(string s, out Product result) // This will be called when we try to parse a non complex type to a complex type
	{
		//IMPORTANT NOTE : s will contain all query string parameterS .
		//In our example, there is only one. But is there are many, we should split them using string[] keyValuePairs = s.Split('&');
	
		if (float.TryParse(s, out float price))
		{
			result = new Product()
			{
				Price = price
			};
			return true;
		}
		result = default;
		return false; //This will let the RoutingMidlleWare to retun 404
	}
}

internal class Handler
{
	public static List<Person> people = new List<Person>
	{
	 new Person { Id = 1, Name = "Alice", DateOfBirth = new DateTime(1990, 5, 15) },
	 new Person { Id = 2, Name = "Bob", DateOfBirth = new DateTime(1985, 10, 25) },
	 new Person { Id = 3, Name = "Charlie", DateOfBirth = new DateTime(1988, 3, 8) }
	};

	public static List<User> users = new List<User>
	{
	 new User { Username = "user1", Email = "user1@gmail.com", Age = 30 },
	 new User { Username = "user2", Email = "user2@ul.edu.lb", Age = 25 },
	 new User { Username = "user3", Email = "user3@gmail.com", Age = 35 }
	};

	public static List<Product> products = new List<Product>
	{
	 new Product { Title = "Product 1", Description = "Description of Product 1",
	Price = 99.99 },
	 new Product { Title = "Product 2", Description = "Description of Product 2",
	Price = 149.99 },
	 new Product { Title = "Product 3", Description = "Description of Product 3",
	Price = 199.99 }
	};
    public static IResult GetRandom_a()
    {
        Random rand = new Random();
        return Results.Ok(rand.Next(1, 100));
    }//NO need for [FromQuery] if you do not receive count from many sources
    public static IResult GetRandom_b([FromQuery] int count)
	{
		if (count <=0)
		{
			return Results.BadRequest("Bad value of count");
		}
		int[] arr = new int[count];
		Random rand = new Random();
		for(int i = 0; i < count; i++)
			{
			arr[i] = rand.Next(1, 100);
		}
		return Results.Ok(arr);
    }//NO need for [FromQuery] if you do not receive count from many sources
    public static IResult GetRandom_b_V1(int count) //NO need for [FromQuery] if you do not receive count from many sources
    {
        if (count <= 0)
        {
            return Results.BadRequest("Bad value of count");
        }
        int[] arr = new int[count];
        Random rand = new Random();
        for (int i = 0; i < count; i++)
        {
            arr[i] = rand.Next(1, 100);
        }
        return Results.Ok(arr);
    }
    public static IResult GetSquare_a(int number)
    {
        return Results.Ok(number * number);
    }
	public static IResult GetSquare_b([FromQuery(Name = "number")] int[] numbers) //Array of integers will take all values passed as query strings named number
	{
        //return Results.Ok(numbers.Select(x => x * x)); => It works as well

        for (int i = 0; i < numbers.Length; i++)
        {
            numbers[i] *= numbers[i];
        }
		return Results.Ok(numbers);
    }

    public static IResult GetSquareFromBody_a([FromBody] int number) //Here We should receive the number from Body - Check Swagger
    {        
        return Results.Ok(number*number);
    }

    public static IResult GetSquareFromBody_b([FromBody] int[] numbers) //Here We should receive the array from Body as Json format - Check Swagger
	{
        //return Results.Ok(numbers.Select(x => x * x)); => It works as well

        for (int i = 0; i < numbers.Length; i++)
        {
            numbers[i] *= numbers[i];
        }
        return Results.Ok(numbers);
    }

	public static IResult AddPerson_a(Person person)
	{
		Console.WriteLine("Added Person : "+ person.ToString());
		people.Add(person);
		return Results.Created($"/api/person/{person.Id}", person); //This link should be valid
	}

    public static IResult AddPerson_a_V1(Person person, ILogger logger)
    {
        logger.LogInformation($"Adding Person {person.Name}");
        people.Add(person);
        return Results.Created($"/api/person/{person.Id}", person);
    }

    public static IResult AddPerson_b(Person person)
    {
        Console.WriteLine("Added Person after validation using filter : " + person.ToString());
        people.Add(person);
        return Results.Created($"/api/person/{person.Id}", person);
    }
	public static IResult AddPeople(Person[] persons)
	{
		people.AddRange(persons);
		return Results.Ok("Added People To List");
	}
	public static IResult GetPerson(int id)
	{
		var person = people.FirstOrDefault(x => x.Id == id);
		if (person is null)
			return Results.ValidationProblem(new Dictionary<string, string[]>
			{
				{"Id", new [] {"Id doesn't exist"} }
			});
		return Results.Ok(person);
	}
	public static IResult GetUser_a()
	{
		//Hardcoded contents for user
		User user = new User { 
			Age = 1, 
			Email ="K.B@gmail.com",
			Username="Test",
		};	
		return Results.Ok(user);
	}
    public static IResult GetUser_b([FromQuery] int? age, [FromQuery] string? username) // ? indicates that the parameters ARE OPTIONALS
    {
        if (age is null && username is null)
        {
            return Results.Ok(users); // return all users
        }
        //return Results.Ok(users.Where(x => x.Age == age || x.Username == username).ToList()); //It works well
        var matchingUsers = new List<User>();
        foreach (var user in users)
        {
            if (user.Age == age || user.Username == username)
            {
                matchingUsers.Add(user);
            }
        }

        return Results.Ok(matchingUsers);
    }

    public static IResult AddUser(User user) //Here, there is no need to add [FromBody] as it will be bound automatically (complex type) in the definition of the handler without passing by lambda expression
    {
		users.Add(user);
		return Results.Created($"/api/user-b?age={user.Age}&username={user.Username}", user);
	}
	public static IResult GetUsers([FromQuery] int age) //No need for [FromQuery] as we do not pass age as route paramters
    {
        //return Results.Ok(users.Where(x => x.Age > age).ToList()); // It works well
        var olderUsers = new List<User>();
        foreach (var user in users)
        {
            if (user.Age > age)
            {
                olderUsers.Add(user);
            }
        }

        return Results.Ok(olderUsers);

    }
	public static IResult GetUserByUsername(string username)
	{
        //return Results.Ok(users.FirstOrDefault(x => x.Username == username)); //It works fine
        User user = null;
        foreach (var u in users)
        {
            if (u.Username == username)
            {
                user = u;
                break;
            }
        }
		//if (user == null) return Results.NotFound(); //This validation could be done in the filter
        return Results.Ok(user);

    }
    public static IResult UpdateEmail(string username, [FromBody] string newEmail)
	{

        if (!(newEmail.Contains("gmail.com") || newEmail.Contains("ul.edu.lb")))
		{
			return Results.Content("The email should contain @gmail.com or @ul.edu.lb");
		}
        
		//var u = users.FirstOrDefault(x => x.Username == username); //It works well
        //if (u is null) return Results.Problem(username + "was not found");
        //u.Email = newEmail;
        //return Results.Ok(u);

        User u = null;
        foreach (var user in users)
        {
            if (user.Username == username)
            {
                u = user;
                break;
            }
        }
		if (u is null) return Results.Problem(username + "was not found");
		u.Email = newEmail;
        return Results.Ok(u);
	}
	public static IResult AddProduct([FromBody] Product product) //Here we should add [FromBody] as in other question we will add to the class AsyncBind method.
                                                      //BindAsync is  more priority in binding than body (default) if we did not specify [FromBody]

    {
        products.Add(product);
		return Results.Created($"/api/product/{product.Title}", product); // This endpoint should exist.
	}
	public static IResult GetProductsByPrice(float minPrice, float maxPrice)
	{
        // return Results.Ok(products.Where(x => x.Price >= minPrice && x.Price <= maxPrice).ToList()); // It works well
        var matchingProducts = new List<Product>();
        foreach (var product in products)
        {
            if (product.Price >= minPrice && product.Price <= maxPrice)
            {
                matchingProducts.Add(product);
            }
        }
		if (matchingProducts.Count == 0) return Results.Content("No products in this range of prices");
		return Results.Ok(matchingProducts);
    }
    public static IResult GetProduct([FromRoute]string title,[FromQuery] string description) //No need for [FromRoute] and [FromQuery] as there is no conflict in priorities
    {
		
		// IMPORTANT - Normally we can put small validations in the handler, but not recommended - For that I will let the validation here, otherwise, we use filters.
		if(title =="" ||  description =="")
		{
			return Results.BadRequest("Title and description should not be empty.");
		}
		// return Results.Ok(products.Where(x => x.Title == title || x.Description == description).ToList()); // It works well
        var matchingProducts = new List<Product>();
        foreach (var product in products)
        {
            if (product.Title == title || product.Description == description)
            {
                matchingProducts.Add(product);
            }
        }

        return Results.Ok(matchingProducts);
    }

    
    public static IResult AddProductTryParse(string title, string description, Product? product) // Important - If you write Prodcut? prodcut (optional) , then if we do not pass any query string, then TryParse will not be called.
    // Product is required or Passed : As Product has a TryParse method, then it will be called !!
    // price (non complex type, the only string query parameters) passed as query string should be parsed to Product (complex type) type using TryParse as the name of the parameter is product (not available in route parameters)
    {
		Product newProduct;

        if (product is null) { 
		newProduct = new Product
		{
			Title = title,
			Description = description,
            Price = 1
        };
            products.Add(newProduct);
            return Results.Created($"/api/product/{title}", newProduct);//This link should be available
        }
		else
		{
			product.Title = title;
			product.Description = description;
            //Price is already set in the TryParse
            products.Add(product);
            return Results.Created($"/api/product/{title}", product);//This link should be available
        }
			
	}
	public static IResult GetPersonInfo([AsParameters] Person person)
	{
        // return Results.Ok(people.FirstOrDefault(x => x.Id == person.Id && x.Name == person.Name)); // It works well

        Person match = null;
        foreach (var p in people)
        {
            if (p.Id == person.Id && p.Name == person.Name)
            {
                match = p;
                break;
            }
        }
		if (match is null) return Results.Content("No person was found with those info");
        return Results.Ok(match);

    }
    public static IResult GetPersonBindAsync(Person person)
	{
        var results = people.Where(x => x.Id == person.Id || x.Name == person.Name).ToList();

        if (results.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
    {
        {"Person Not found: ", new [] {"Id and Name do not exist"} }
    });
        }

        return Results.Ok(results);


    }
}


public class ValidatePerson : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var person = context.GetArgument<Person>(0); //Get the first argument from the handler signature AddPerson_b(person) - Binding then Validation then execution of the handler body
        if (person.Id < 1 || person.Id > 1000 || !Char.IsLetter(person.Name[0]))
		{
			return Results.ValidationProblem(new Dictionary<string, string[]>
			{
				{"Id", new [] {"Id should be between 1 and 1000"} },
				{"Name", new [] {"Name should start with a letter" } }
			});
		}
		return await next(context);
	}
}

public class ValidateId : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
        var id = context.GetArgument<int>(0);
        //Person person = Handler.people.FirstOrDefault(x => x.Id == id); //It works well
        Person person = null;
        foreach (var p in Handler.people)
        {
            if (p.Id == id)
            {
                person = p;
                break;
            }
        }
        if (person is null)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                {"Id", new [] {"Id doesn't exist"} }
            });

		if(id < 1 || id > 1000)
		{
			return Results.ValidationProblem(new Dictionary<string, string[]>
			{
				{"Id", new [] {"Id should be between 1 and 1000"} }
			});
		}

		return await next(context);
	}
}
public class ValidateIdViaPerson : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var person = context.GetArgument<Person>(0);
        if (person.Id < 1 || person.Id > 1000)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                {"Id", new [] {"Id should be between 1 and 1000"} }
            });
        }

        return await next(context);
    }
}

public class ValidateAge : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var age = context.GetArgument<int>(0);
		if(age < 1 || age > 100)
		{
			return Results.ValidationProblem(new Dictionary<string, string[]>
			{
				{"Age", new [] {"Age should be between 1 and 100"} }
			});
		}
		return await next(context);
	}
}

public class ValidateUsername : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var username = context.GetArgument<string>(0);
		
        User user = null;
        foreach (var u in Handler.users)
        {
            if (u.Username == username)
            {
                user = u;
                break;
            }
        }
        if (user == null) return Results.NotFound("The username you provide does not exist."); // We could add that to ValidationProblem as well

        if (username.Length > 20)
		{
			return Results.ValidationProblem(new Dictionary<string, string[]>
			{
				{"Username", new [] {"Username should be less than 20 characters"} }
			});
		}
		return await next(context);
	}
}
public class ValidatePrice : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var min = context.GetArgument<float>(0);
		var max = context.GetArgument<float>(1);
		if(min < 0 || max < 0 || min > max)
		{
			return Results.ValidationProblem(new Dictionary<string, string[]>
			{
				{"price", new [] {"min should be > 0, max should be > 0, min should be smaller than max"} }
			});
		}
		return await next(context);
	}
}

public class ValidatePeople : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var people = context.GetArgument<Person[]>(0);
		foreach(var person in people)
		{
			if(person.Id < 1 || person.Id > 1000 || !Char.IsLetter(person.Name[0]))
			{
				return Results.ValidationProblem(new Dictionary<string, string[]>
				{
					{"Id", new [] {"Id should be between 1 and 1000"} },
					{"Name", new [] {"Name should start with a letter" } }
				});
			}
		}
		return await next(context);
	}
}