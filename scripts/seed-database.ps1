# Database Seeding Script for Development
# This script seeds the database with sample data

param(
    [switch]$Reset,
    [string]$ConnectionString = "Host=localhost;Database=surveybot_dev;Username=postgres;Password=postgres"
)

Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Database Seeding Script" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Change to project directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "Project Root: $projectRoot" -ForegroundColor Yellow
Write-Host "Connection String: $ConnectionString" -ForegroundColor Yellow
Write-Host ""

# Check if we should reset the database
if ($Reset) {
    Write-Host "WARNING: Reset flag detected!" -ForegroundColor Red
    Write-Host "This will DROP the database and recreate it with seed data." -ForegroundColor Red
    $confirm = Read-Host "Are you sure you want to continue? (yes/no)"

    if ($confirm -ne "yes") {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Create a temporary C# program to seed the database
$tempProgram = @"
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurveyBot.Infrastructure.Data;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = args.Length > 0 ? args[0] : "Host=localhost;Database=surveybot_dev;Username=postgres;Password=postgres";
        var shouldReset = args.Length > 1 && args[1] == "reset";

        Console.WriteLine("Setting up services...");

        var services = new ServiceCollection();

        services.AddDbContext<SurveyBotDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

        if (shouldReset)
        {
            Console.WriteLine("Dropping database...");
            await context.Database.EnsureDeletedAsync();
            Console.WriteLine("Database dropped.");
        }

        Console.WriteLine("Applying migrations...");
        await context.Database.MigrateAsync();
        Console.WriteLine("Migrations applied.");

        Console.WriteLine("Seeding database...");
        var seeder = new DataSeeder(context, logger);
        await seeder.SeedAsync();

        // Verify seeding
        var userCount = await context.Users.CountAsync();
        var surveyCount = await context.Surveys.CountAsync();
        var questionCount = await context.Questions.CountAsync();
        var responseCount = await context.Responses.CountAsync();
        var answerCount = await context.Answers.CountAsync();

        Console.WriteLine("");
        Console.WriteLine("=================================");
        Console.WriteLine("Database Seeding Complete!");
        Console.WriteLine("=================================");
        Console.WriteLine($"Users: {userCount}");
        Console.WriteLine($"Surveys: {surveyCount}");
        Console.WriteLine($"Questions: {questionCount}");
        Console.WriteLine($"Responses: {responseCount}");
        Console.WriteLine($"Answers: {answerCount}");
        Console.WriteLine("=================================");
    }
}
"@

# Create temporary directory
$tempDir = Join-Path $env:TEMP "SurveyBotSeeder"
if (Test-Path $tempDir) {
    Remove-Item -Path $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

# Create temporary project
$tempCsproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$projectRoot\src\SurveyBot.Infrastructure\SurveyBot.Infrastructure.csproj" />
  </ItemGroup>
</Project>
"@

# Write files
Set-Content -Path (Join-Path $tempDir "Program.cs") -Value $tempProgram
Set-Content -Path (Join-Path $tempDir "TempSeeder.csproj") -Value $tempCsproj

Write-Host "Building temporary seeder project..." -ForegroundColor Yellow
Set-Location $tempDir
$buildOutput = dotnet build --verbosity quiet 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $buildOutput
    exit 1
}

Write-Host "Running seeder..." -ForegroundColor Yellow
Write-Host ""

if ($Reset) {
    dotnet run -- $ConnectionString "reset"
} else {
    dotnet run -- $ConnectionString
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Seeding completed successfully!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Seeding failed!" -ForegroundColor Red
}

# Cleanup
Set-Location $projectRoot
Remove-Item -Path $tempDir -Recurse -Force

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
