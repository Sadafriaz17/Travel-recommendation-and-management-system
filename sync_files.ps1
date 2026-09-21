$projPath = "d:\My websites\TourFYP\TourFYP\TourwebsiteFYP.csproj"
[xml]$proj = Get-Content -Raw $projPath

# The default namespace for old csproj files is usually http://schemas.microsoft.com/developer/msbuild/2003
$nsUri = $proj.Project.NamespaceURI

$itemGroup = $proj.CreateElement('ItemGroup', $nsUri)
$proj.Project.AppendChild($itemGroup)

$compileFiles = @(
    "Areas\AdminArea\Controllers\ManageReviewsController.cs",
    "Controllers\BlogController.cs"
)

$contentFiles = @(
    "Areas\AdminArea\Views\ManageReviews\Index.cshtml",
    "Areas\AdminArea\Views\ManageReviews\Create.cshtml",
    "Areas\AdminArea\Views\ManageReviews\Edit.cshtml",
    "Views\Home\HomepageReviews.cshtml",
    "Views\Blog\Index.cshtml",
    "Views\Blog\HomepageBlogs.cshtml",
    "Views\Blog\Details.cshtml",
    "Views\Product\Index.cshtml",
    "Views\Product\Details.cshtml"
)

foreach ($f in $compileFiles) {
    # Check if already exists
    $exists = $proj.Project.ItemGroup.Compile | Where-Object { $_.Include -eq $f }
    if (-not $exists) {
        $compile = $proj.CreateElement('Compile', $nsUri)
        $compile.SetAttribute('Include', $f)
        $itemGroup.AppendChild($compile)
        Write-Host "Added $f to Compile"
    }
}

foreach ($f in $contentFiles) {
    # Check if already exists
    $exists = $proj.Project.ItemGroup.Content | Where-Object { $_.Include -eq $f }
    if (-not $exists) {
        $content = $proj.CreateElement('Content', $nsUri)
        $content.SetAttribute('Include', $f)
        $itemGroup.AppendChild($content)
        Write-Host "Added $f to Content"
    }
}

$proj.Save($projPath)
Write-Host "Done saving project."
