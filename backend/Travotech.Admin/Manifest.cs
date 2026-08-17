using OrchardCore.Modules.Manifest;

// A module rather than a theme: placement.json shipped by a theme is only consulted for that
// theme's own rendering, and the admin renders under TheAdmin. Modules contribute placement to
// every context, which is how OrchardCore.Contents and OrchardCore.Seo do it.
//
// placement.json groups the editors into tabs using Orchard's "Zone#Tab;position" syntax; the
// segment after '#' becomes the tab header. Note that the file must contain nothing but
// shape-name keys mapping to arrays - a "//" comment key fails deserialization and takes the
// whole tenant down with a 500.
[assembly: Module(
    Name = "TrafoTech Admin",
    Author = "TrafoTech",
    Website = "https://trafotech.local",
    Version = "1.0.0",
    Description = "Groups the TrafoTech content editors into tabs.",
    Category = "Content Management"
)]
