using Midnight.EC.Plant.WEB.Services.PlantEffect;

namespace Midnight.EC.Plant.WEB.Tests;

public class ResolveLeftImageTests
{
    [Fact]
    public void Cover_only_returns_cover()
    {
        Assert.Equal("/uploads/plants/a/photos/c.jpg", PlantEffectImageService.ResolveLeftImage("/uploads/plants/a/photos/c.jpg", null));
    }

    [Fact]
    public void Swapped_url_wins()
    {
        Assert.Equal(
            "/uploads/plants/a/effects/e.png",
            PlantEffectImageService.ResolveLeftImage("/uploads/plants/a/photos/c.jpg", " /uploads/plants/a/effects/e.png "));
    }

    [Fact]
    public void Whitespace_swap_falls_back_to_cover()
    {
        Assert.Equal("/cover.jpg", PlantEffectImageService.ResolveLeftImage("/cover.jpg", "   "));
    }

    [Fact]
    public void Both_empty_returns_null()
    {
        Assert.Null(PlantEffectImageService.ResolveLeftImage(null, null));
        Assert.Null(PlantEffectImageService.ResolveLeftImage("  ", ""));
    }
}
