using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.AI;
using Midnight.EC.Plant.WEB.Services.Background;
using Midnight.EC.Plant.WEB.Services.Configuration;
using Midnight.EC.Plant.WEB.Services.ContentParser;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Services.PageComposer;
using Midnight.EC.Plant.WEB.Services.Plant;
using Midnight.EC.Plant.WEB.Services.PlantAnalysis;
using Midnight.EC.Plant.WEB.Services.PlantDiary;
using Midnight.EC.Plant.WEB.Services.PlantEffect;
using Midnight.EC.Plant.WEB.Services.PlantKnowledge;
using Midnight.EC.Plant.WEB.Services.PlantCare;
using Midnight.EC.Plant.WEB.Services.PlantList;
using Midnight.EC.Plant.WEB.Services.PlantProfile;
using Midnight.EC.Plant.WEB.Services.PlantReminder;
using Midnight.EC.Plant.WEB.Services.PlantSource;
using Midnight.EC.Plant.WEB.Services.PlantTimeline;

namespace Midnight.EC.Plant.WEB.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlantServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<DBList>(configuration.GetSection("ConnectionStrings"));

        services.AddHttpClient("ContentFetcher", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(45);
        });

        services.AddHttpClient("OpenAI", (sp, client) =>
        {
            var aiOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
            client.BaseAddress = new Uri(aiOptions.OpenAI.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(180);
        });

        services.AddScoped<PlantRespo>();
        services.AddScoped<PlantSpeciesRespo>();
        services.AddScoped<PlantKnowledgeRespo>();
        services.AddScoped<PlantDiaryRespo>();
        services.AddScoped<PlantImageRespo>();
        services.AddScoped<PlantEffectImageRespo>();
        services.AddScoped<PlantAnalysisRespo>();
        services.AddScoped<PlantAnalysisJobRespo>();
        services.AddScoped<PlantSourceRespo>();
        services.AddScoped<PlantCareRecordRespo>();
        services.AddScoped<PlantFertilizerProductRespo>();
        services.AddScoped<PlantProfileRespo>();
        services.AddScoped<PlantReminderRespo>();
        services.AddScoped<PhotoWallLayoutRespo>();
        services.AddScoped<PageComposerLayoutRespo>();

        services.AddScoped<PlantService>();
        services.AddScoped<PhotoWallLayoutService>();
        services.AddScoped<PageComposerService>();
        services.AddScoped<PageComposerHomeBuilder>();
        services.AddScoped<PlantKnowledgeService>();
        services.AddScoped<PlantDiaryService>();
        services.AddScoped<PlantImageService>();
        services.AddScoped<PlantEffectImageService>();
        services.AddScoped<IOpenAIImageService, OpenAIImageService>();
        services.AddScoped<IChatCompletionEnvelope, OpenAIChatCompletionEnvelope>();
        services.AddScoped<OpenAISpeciesFinderService>();
        services.AddScoped<CareKnowledgeSynthesisService>();
        services.AddScoped<EnvironmentFitService>();
        services.AddScoped<OpenAIPlantAgentService>();
        services.AddScoped<PlantSourceService>();
        services.AddScoped<PlantCareService>();
        services.AddScoped<PlantListService>();
        services.AddScoped<PlantProfileService>();
        services.AddScoped<PlantReminderService>();
        services.AddScoped<PlantTimelineService>();
        services.AddScoped<IImageStorageService, LocalImageStorageService>();
        services.AddScoped<CareGuideLayoutService>();
        services.AddScoped<PlantAnalysisService>();

        services.AddScoped<IContentFetcher, ContentFetcher>();
        services.AddScoped<IPlantContentParser, YouTubeParser>();
        services.AddScoped<IPlantContentParser, WebArticleParser>();
        services.AddScoped<IPlantContentParser, GenericHtmlParser>();
        services.AddScoped<IPlantContentParserFactory, PlantContentParserFactory>();

        services.AddHostedService<PlantAnalysisBackgroundService>();

        return services;
    }
}
