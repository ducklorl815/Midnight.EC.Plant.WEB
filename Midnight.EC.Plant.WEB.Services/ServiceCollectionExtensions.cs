using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.AI;
using Midnight.EC.Plant.WEB.Services.Background;
using Midnight.EC.Plant.WEB.Services.Configuration;
using Midnight.EC.Plant.WEB.Services.ContentParser;
using Midnight.EC.Plant.WEB.Services.External;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Services.Plant;
using Midnight.EC.Plant.WEB.Services.PlantAnalysis;
using Midnight.EC.Plant.WEB.Services.PlantDiary;
using Midnight.EC.Plant.WEB.Services.PlantKnowledge;
using Midnight.EC.Plant.WEB.Services.PlantCare;
using Midnight.EC.Plant.WEB.Services.PlantProfile;
using Midnight.EC.Plant.WEB.Services.PlantReminder;
using Midnight.EC.Plant.WEB.Services.PlantSource;
using Midnight.EC.Plant.WEB.Services.PlantTimeline;

namespace Midnight.EC.Plant.WEB.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlantServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExternalPlantApiOptions>(configuration.GetSection(ExternalPlantApiOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddDbContext<PlantDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpClient("ExternalPlantApi", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient("ContentFetcher", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(45);
        });

        services.AddHttpClient("OpenAI", (sp, client) =>
        {
            var aiOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
            client.BaseAddress = new Uri(aiOptions.OpenAI.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddScoped<IPlantRepository, PlantRepository>();
        services.AddScoped<IPlantSpeciesRepository, PlantSpeciesRepository>();
        services.AddScoped<IPlantKnowledgeRepository, PlantKnowledgeRepository>();
        services.AddScoped<IPlantDiaryRepository, PlantDiaryRepository>();
        services.AddScoped<IPlantImageRepository, PlantImageRepository>();
        services.AddScoped<IPlantAnalysisRepository, PlantAnalysisRepository>();
        services.AddScoped<IPlantAnalysisJobRepository, PlantAnalysisJobRepository>();
        services.AddScoped<IPlantSourceRepository, PlantSourceRepository>();
        services.AddScoped<IPlantCareRecordRepository, PlantCareRecordRepository>();
        services.AddScoped<IPlantProfileRepository, PlantProfileRepository>();
        services.AddScoped<IPlantReminderRepository, PlantReminderRepository>();

        services.AddScoped<IPlantService, PlantService>();
        services.AddScoped<IPlantKnowledgeService, PlantKnowledgeService>();
        services.AddScoped<IPlantDiaryService, PlantDiaryService>();
        services.AddScoped<IPlantImageService, PlantImageService>();
        services.AddScoped<IPlantSourceService, PlantSourceService>();
        services.AddScoped<IPlantCareService, PlantCareService>();
        services.AddScoped<IPlantProfileService, PlantProfileService>();
        services.AddScoped<IPlantReminderService, PlantReminderService>();
        services.AddScoped<IPlantTimelineService, PlantTimelineService>();
        services.AddScoped<IExternalPlantApiService, ExternalPlantApiService>();
        services.AddScoped<IImageStorageService, LocalImageStorageService>();
        services.AddScoped<IAIAgentService, OpenAIPlantAgentService>();
        services.AddScoped<ICareKnowledgeSynthesisService, CareKnowledgeSynthesisService>();
        services.AddScoped<PlantAnalysisService>();
        services.AddScoped<IPlantAnalysisService>(sp => sp.GetRequiredService<PlantAnalysisService>());

        services.AddScoped<IContentFetcher, ContentFetcher>();
        services.AddScoped<IPlantContentParser, YouTubeParser>();
        services.AddScoped<IPlantContentParser, WebArticleParser>();
        services.AddScoped<IPlantContentParser, GenericHtmlParser>();
        services.AddScoped<IPlantContentParserFactory, PlantContentParserFactory>();

        services.AddHostedService<PlantAnalysisBackgroundService>();

        return services;
    }
}
