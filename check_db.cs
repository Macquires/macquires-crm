using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;

var builder = Host.CreateApplicationBuilder(args);
// Assuming RegisterBackEndServices or similar is called in Program.cs
// I'll just try to get the context from the built host if I can.
// But I don't have the full setup here.

// Alternative: Use a simple console app that connects to the DB.
// I'll just use a shell command to query the DB if possible.
