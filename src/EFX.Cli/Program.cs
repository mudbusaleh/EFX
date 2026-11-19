using EFX.Sample;

if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
{
    Console.WriteLine("EFX - Independent database development framework for .NET");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  efx demo [database-file]   Run the Zero-Command Schema Sync demo");
    Console.WriteLine("  efx help                   Show this help");
    return 0;
}

if (string.Equals(args[0], "demo", StringComparison.OrdinalIgnoreCase))
    return EfxSample.Run(args.Length > 1 ? args[1] : "efx-demo.db");

Console.Error.WriteLine($"Unknown command: {args[0]}");
Console.Error.WriteLine("Run 'efx help' to see available commands.");
return 1;
