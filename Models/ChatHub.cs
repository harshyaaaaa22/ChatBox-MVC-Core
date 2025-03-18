using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> UserConnections = new ConcurrentDictionary<string, string>();

    public async Task RegisterUser(string username)
    {
        var connectionId = Context.ConnectionId;
        UserConnections[username] = connectionId;
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var username = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        if (username != null)
        {
            UserConnections.TryRemove(username, out _);
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Group chat methods
    public async Task SendMessage(string group, string user, string message)
    {
        await Clients.Group(group).SendAsync("ReceiveMessage", user, message);
    }

    public async Task SendFile(string group, string user, string fileName, string fileUrl)
    {
        await Clients.Group(group).SendAsync("ReceiveFile", user, fileName, fileUrl);
    }

    public async Task JoinGroup(string group)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public async Task LeaveGroup(string group)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }

    public async Task NotifyTypingGroup(string group, string user)
    {
        await Clients.Group(group).SendAsync("UserTyping", user);
    }

    // Private chat methods
    public async Task SendPrivateMessage(string sender, string recipient, string message)
    {
        var recipientConnectionId = GetConnectionIdByUsername(recipient);

        if (!string.IsNullOrEmpty(recipientConnectionId))
        {
            await Clients.Client(recipientConnectionId).SendAsync("ReceivePrivateMessage", sender, recipient, message);
        }
        await Clients.Caller.SendAsync("ReceivePrivateMessage", sender, recipient, message);
    }

    public async Task SendPrivateFile(string sender, string recipient, string fileName, string fileUrl)
    {
        var recipientConnectionId = GetConnectionIdByUsername(recipient);

        if (!string.IsNullOrEmpty(recipientConnectionId))
        {
            await Clients.Client(recipientConnectionId).SendAsync("ReceivePrivateFile", sender, recipient, fileName, fileUrl);
        }
        await Clients.Caller.SendAsync("ReceivePrivateFile", sender, recipient, fileName, fileUrl);
    }

    public async Task NotifyTypingPrivate(string sender, string recipient)
    {
        var recipientConnectionId = GetConnectionIdByUsername(recipient);

        if (!string.IsNullOrEmpty(recipientConnectionId))
        {
            await Clients.Client(recipientConnectionId).SendAsync("UserTypingPrivate", sender);
        }
    }

    private string GetConnectionIdByUsername(string username)
    {
        UserConnections.TryGetValue(username, out var connectionId);
        return connectionId;
    }

    public async Task DeleteMessage(string groupName, string messageId)
    {
        await Clients.Group(groupName).SendAsync("DeleteMessage", messageId);
    }

    public async Task DeletePrivateMessage(string sender, string recipient, string messageId)
    {
        await Clients.User(recipient).SendAsync("DeleteMessage", messageId);
        await Clients.User(sender).SendAsync("DeleteMessage", messageId);
    }
}

public class ProductFilterViewModel
{
    public List<Product> Products { get; set; }
    public string SearchTerm { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
}



public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }

    public ICollection<Product> Products { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Salary { get; set; }
    public int CategoryId { get; set; }

    public string Image1Url { get; set; }
    public string Image2Url { get; set; }

    public Category Category { get; set; }
}


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
}

services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

public class ProductController : Controller
{
    private readonly AppDbContext _context;
    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(string searchTerm, decimal? minSalary, decimal? maxSalary)
    {
        var products = _context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            products = products.Where(p => p.Name.Contains(searchTerm));
        }

        if (minSalary.HasValue)
        {
            products = products.Where(p => p.Salary >= minSalary.Value);
        }

        if (maxSalary.HasValue)
        {
            products = products.Where(p => p.Salary <= maxSalary.Value);
        }

        var model = new ProductFilterViewModel
        {
            Products = products.ToList(),
            SearchTerm = searchTerm,
            MinSalary = minSalary,
            MaxSalary = maxSalary
        };

        return View(model);
    }
}

public class ProductController : Controller
{
    private readonly AppDbContext _context;
    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(string searchTerm, decimal? minSalary, decimal? maxSalary)
    {
        var products = _context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            products = products.Where(p => p.Name.Contains(searchTerm));
        }

        if (minSalary.HasValue)
        {
            products = products.Where(p => p.Salary >= minSalary.Value);
        }

        if (maxSalary.HasValue)
        {
            products = products.Where(p => p.Salary <= maxSalary.Value);
        }

        var model = new ProductFilterViewModel
        {
            Products = products.ToList(),
            SearchTerm = searchTerm,
            MinSalary = minSalary,
            MaxSalary = maxSalary
        };

        return View(model);
    }
}



@model ProductFilterViewModel

<form method="get">
    <input type="text" name="searchTerm" placeholder="Search Product" value="@Model.SearchTerm" />
    <input type="number" step="0.01" name="minSalary" placeholder="Min Salary" value="@Model.MinSalary" />
    <input type="number" step="0.01" name="maxSalary" placeholder="Max Salary" value="@Model.MaxSalary" />
    <button type="submit">Filter</button>
</form>

<table class="table">
    <thead>
        <tr>
            <th>Product</th>
            <th>Salary</th>
            <th>Category</th>
            <th>Images</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model.Products)
        {
            <tr>
                <td>@item.Name</td>
                <td>@item.Salary</td>
                <td>@item.Category?.Name</td>
                <td>
                    <img src="@item.Image1Url" width="100" />
                    <img src="@item.Image2Url" width="100" />
                </td>
            </tr>
        }
    </tbody>
</table>



Add-Migration InitialCreate
Update-Database


public class ProductController : Controller
{
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    // Show list with filter
    public IActionResult Index(string searchTerm, decimal? minSalary, decimal? maxSalary)
    {
        var products = _context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
            products = products.Where(p => p.Name.Contains(searchTerm));

        if (minSalary.HasValue)
            products = products.Where(p => p.Salary >= minSalary.Value);

        if (maxSalary.HasValue)
            products = products.Where(p => p.Salary <= maxSalary.Value);

        var model = new ProductFilterViewModel
        {
            Products = products.ToList(),
            SearchTerm = searchTerm,
            MinSalary = minSalary,
            MaxSalary = maxSalary
        };

        return View(model);
    }

    // Create
    public IActionResult Create()
    {
        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
        return View();
    }

    [HttpPost]
    public IActionResult Create(Product product)
    {
        if (ModelState.IsValid)
        {
            _context.Products.Add(product);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        return View(product);
    }

    // Edit
    public IActionResult Edit(int id)
    {
        var product = _context.Products.Find(id);
        if (product == null) return NotFound();

        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        return View(product);
    }

    [HttpPost]
    public IActionResult Edit(Product product)
    {
        if (ModelState.IsValid)
        {
            _context.Products.Update(product);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        return View(product);
    }

    // Delete
    public IActionResult Delete(int id)
    {
        var product = _context.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound();

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteConfirmed(int id)
    {
        var product = _context.Products.Find(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }
}


@model Product
@{
    ViewBag.Title = "Create Product";
}
<h2>Create Product</h2>

<form asp-action="Create">
    <div>
        <label>Name</label>
        <input asp-for="Name" class="form-control" />
    </div>
    <div>
        <label>Salary</label>
        <input asp-for="Salary" class="form-control" />
    </div>
    <div>
        <label>Category</label>
        <select asp-for="CategoryId" asp-items="ViewBag.Categories" class="form-control"></select>
    </div>
    <div>
        <label>Image 1 URL</label>
        <input asp-for="Image1Url" class="form-control" />
    </div>
    <div>
        <label>Image 2 URL</label>
        <input asp-for="Image2Url" class="form-control" />
    </div>
    <button type="submit" class="btn btn-success">Create</button>
</form>


@model Product
@{
    ViewBag.Title = "Edit Product";
}
<h2>Edit Product</h2>

<form asp-action="Edit">
    <input type="hidden" asp-for="Id" />
    <!-- Same fields as Create -->
    <div>
        <label>Name</label>
        <input asp-for="Name" class="form-control" />
    </div>
    <div>
        <label>Salary</label>
        <input asp-for="Salary" class="form-control" />
    </div>
    <div>
        <label>Category</label>
        <select asp-for="CategoryId" asp-items="ViewBag.Categories" class="form-control"></select>
    </div>
    <div>
        <label>Image 1 URL</label>
        <input asp-for="Image1Url" class="form-control" />
    </div>
    <div>
        <label>Image 2 URL</label>
        <input asp-for="Image2Url" class="form-control" />
    </div>
    <button type="submit" class="btn btn-primary">Update</button>
</form>



<a asp-action="Create" class="btn btn-success">Add New Product</a>

@foreach (var item in Model.Products)
{
    <tr>
        <!-- product fields -->
        <td>
            <a asp-action="Edit" asp-route-id="@item.Id" class="btn btn-sm btn-primary">Edit</a>
            <a asp-action="Delete" asp-route-id="@item.Id" class="btn btn-sm btn-danger">Delete</a>
        </td>
    </tr>
                }
