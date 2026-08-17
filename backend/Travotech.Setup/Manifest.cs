using OrchardCore.DisplayManagement.Manifest;

// Declared as a theme rather than a module: Orchard's setup-recipe harvester only scans
// theme extensions, so a recipe carried by a module never reaches the setup screen.
[assembly: Theme(
    Name = "TrafoTech Setup",
    Author = "TrafoTech",
    Website = "https://trafotech.local",
    Version = "1.0.0",
    Description = "Carries the TrafoTech headless setup recipe: content types, roles and seed content."
)]
