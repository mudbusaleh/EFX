using EFX.Core;
using EFX.Sqlite;

var engine = new EfxEngine();
var provider = new SqliteProvider();
var models = engine.DiscoverModels(typeof(Program).Assembly);
Console.WriteLine("EFX Sample");
foreach (var model in models) Console.WriteLine($"Model: {model.Name}");
var plan = engine.Plan(typeof(Program).Assembly, provider, "Data Source=efx-demo.db");
Console.WriteLine($"Changes: {plan.Changes.Count}");
